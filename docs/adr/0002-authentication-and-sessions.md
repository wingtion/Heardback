# ADR-0002: Authentication, sessions and invite-only registration

- **Status:** Accepted
- **Date:** 2026-11-17 (revised the same day: absolute session lifetime, revoke-all on token reuse,
  serialized bootstrap claim, rate limiting; then fixes from the independent Phase 1 review;
  2026-11-18: sign-in ends the browser's previous session; reload keeps the session on brief outages;
  bootstrap code has the invite code shape; 2026-11-26: client IP behind Netlify's signed proxy)

## Context

Heardback is a small invite-only community that stores personal job-search data. Each user's data has to
stay separate, sign-in has to survive page reloads, and a stolen browser token should do as little
damage as possible. One person builds and runs it, so the design has to stay simple to operate.

## Decision

**Users and passwords.** The Identity module has its own small `users` table. Passwords are hashed with
ASP.NET Core's built-in `PasswordHasher` (versioned PBKDF2, rehashed on sign-in when the algorithm is
upgraded). Passwords must be 12–128 characters, with no composition rules (NIST SP 800-63B). Full
ASP.NET Core Identity was rejected: it adds roughly seven tables and features we do not need.

**Registration.** Registration needs a single-use invite code. An admin creates invite codes, and each
one expires (7 days by default). The register endpoint checks the code before the email, so it cannot
be used to find out who has an account. A code is claimed with one conditional `UPDATE`, so two people
can never register with the same code. A code counts as used once `used_at` is set; that column is never
cleared, so deleting the account that used a code does not make the code valid again.

**First admin.** The first admin registers with a one-time bootstrap code from configuration.
- The code stops working once an admin exists.
- It has the same shape as a generated invite code (12 characters, Crockford base32), so the first
  admin registers through the normal form. Startup fails on any other shape (decided 2026-11-18, after a
  16-character code was silently cut to 12 by the form).
- Bootstrap registrations take a Postgres advisory transaction lock and re-check for an admin inside
  the registration transaction. Simultaneous attempts therefore run one at a time, and only the first
  succeeds. Without the lock, five simultaneous attempts created five admins.

**Access tokens.** Access tokens are HS256 JWTs that last 15 minutes. They carry only `sub` (the user
id) and `role`, never PII. Expiry is checked against the injected `TimeProvider`, with zero clock skew.
The signing key comes from user-secrets or an environment variable and must be at least 32 bytes.

**Refresh tokens.** Refresh tokens are opaque 256-bit random values. Only their SHA-256 hash is stored.
- Every refresh swaps the current token for a new one in the same *family*: the chain of tokens that
  started with one login.
- **Sliding lifetime:** a new token is valid for 14 days from the refresh.
- **Absolute lifetime:** each session also has a hard end 60 days after sign-in. The end is fixed at
  login and carried through every rotation. No token, and no cookie, outlives it, so after 60 days the
  user must sign in again however active they are.
- **Reuse detection:** presenting a token that was already swapped out, more than 30 seconds later,
  means it was probably copied. We cannot tell which device is the attacker's, so **every session that
  user has** is revoked. Other users are unaffected.
- Within those 30 seconds, for example when two tabs refresh at the same moment, the old token is rejected
  as *superseded*.
- The swap itself is a conditional `UPDATE`, so when two refreshes of the same token race, exactly one
  wins. The loser is also *superseded*.
- A superseded refresh returns 409 `auth.refresh_superseded` and leaves the cookie alone, because the
  browser already holds the winner's token. The Angular client then retries the refresh once. Every other
  failure returns 401 and clears the cookie.
- The client signs the user out only on a 401 from refresh. A 429, a server error or a network failure
  leaves the session in place. That includes the refresh on page reload: the app stays on its
  "Restoring your session" screen and retries after 1, 2 and 4 seconds (or after `Retry-After`, if it
  is 10 seconds or less). Only then does it start signed out, with a "Couldn't reach Heardback" notice.
- Logout revokes only that session's family; other devices stay signed in.
- A successful sign-in or registration also revokes the session of the refresh cookie the browser still
  sent, whoever it belonged to. The new cookie replaces it, so that session could never be used by this
  browser again and would otherwise stay alive (and stealable) until it expired. A failed sign-in
  changes nothing.
- On sign-in, the user's tokens from sessions past their absolute end are deleted. They can never be used
  again, and replaying them is just "expired".

**Transport.** The refresh token travels only in an `httpOnly`, `Secure`, `SameSite=Strict` cookie
scoped to `/api/auth`, on every response that sets or clears it. The access token lives in memory in
the Angular app, never in `localStorage`, so an XSS bug cannot steal a long-lived credential.

**CSRF.** There is no CSRF token, because nothing a cross-site request can trigger needs one:
- All data endpoints authenticate with the `Authorization: Bearer` header, which a browser never
  attaches on its own. A forged cross-site request therefore arrives unauthenticated.
- The only cookie is the refresh cookie. With `SameSite=Strict`, browsers do not send it on cross-site
  requests at all, and its `Path` limits it to `/api/auth`.
- Even a same-site forged refresh would only rotate the token and return the new access token in a
  response the attacker cannot read (same-origin, no CORS).

**Same-origin.** The frontend and API share one origin. In development the Angular dev server proxies
`/api`; in production Netlify will forward `/api/*` to the Fly.io API (Phase 8). This is what makes
`SameSite=Strict` work, and it removes the need for CORS.

**Rate limiting.** The anonymous auth endpoints use ASP.NET Core's built-in rate limiter, with fixed
windows partitioned by client IP.
- Login and register share one budget, 10 per minute by default, so alternating between them does not
  help an attacker.
- Refresh and logout share their own budget, 30 per minute.
- Rejections are 429 ProblemDetails with `Retry-After`.
- Limits are configured under `Identity:RateLimits`.
- Fixed windows were chosen because they report an exact retry time. They allow up to twice the limit
  across a window boundary, which is acceptable at these limits.

**Client IP.** Per-IP limits only work if the API knows the real client IP. Behind a proxy, the
connection address is the proxy's, so every user would share one budget.
- `ClientIp:Source` is required and has no default, so a deployment that has not decided refuses to
  start.
- `Direct` means the connection address is the client's. Development and tests use it.
- `ForwardedHeaders` honours `X-Forwarded-For` only from configured `TrustedProxies` or
  `TrustedNetworks`. A spoofed header from anyone else is ignored.
- `NetlifySignedProxy` is the production setting. Netlify forwards `/api/*` to Fly.io with a signed
  proxy rewrite, which adds an `x-nf-sign` token (HS256 JWS with `iss: netlify` and `exp`, signed with
  a shared secret). Netlify's egress addresses are not published or stable, so a trusted proxy list
  cannot work; the signature is what proves a request came through Netlify.
  - Every request except `/health` must carry a valid token: HS256 only, issuer `netlify`, an expiry
    that has not passed (30 s clock skew), signed with `ClientIp:Netlify:SignatureSecret` (at least
    32 characters, environment variable). Anything else gets a 403 ProblemDetails. So someone calling
    the Fly.io host directly reaches nothing but the health check, and cannot choose their own IP.
  - The client IP is then read from `ClientIp:Netlify:ClientIpHeader` (default
    `X-Nf-Client-Connection-Ip`). A signed request without a valid IP there is refused too, rather
    than sharing one budget with everyone. `X-Forwarded-For` is never read in this mode.
  - The refusal reason is logged; the IP and the token are not.

**Authorization.** A fallback policy requires an authenticated user everywhere. Anonymous endpoints
(health, register, login, refresh, logout) are marked explicitly. The `Admin` policy requires the
`admin` role. The current user's id is read only from the validated token, through `ICurrentUser`.

**Data isolation.** Module DbContexts derive from `ModuleDbContext`, which applies a named query filter
to every `IUserOwned` entity. It also stamps the owner on insert and refuses updates or deletes of
another user's rows. Architecture tests require every entity with a `UserId` to be either filtered or
explicitly exempted with a stated reason (`RefreshToken` is exempt because it is looked up before the
caller is known).

## Consequences

- An access token stays valid for up to 15 minutes after logout or revocation. That is accepted for
  this scale. A denylist could be added later if needed.
- **Approved trade-off (author, 2026-11-17): a lost refresh response signs the user out everywhere.**
  If a refresh succeeds on the server but the response never reaches the browser (network drop, tab
  closed mid-request), the browser keeps the old token. Its next refresh, more than 30 seconds later,
  looks like theft and revokes all of the user's sessions. This is inherent to reuse detection and rare.
  The user simply signs in again.
- **Client IP behind Netlify (decided in Phase 8).** Fly.io sees Netlify's address, so Fly's own
  client-IP header is useless here; the signed proxy check above replaces it.
  - Netlify documents the `x-nf-sign` token but not the header that carries the client IP to a proxied
    site, nor whether a client-supplied copy of it is overwritten. Both are checked once after the first
    deploy, before anyone is invited, with the sign-in rate limit: two networks get separate budgets,
    and a forged header does not reset the budget (README, "Deploying"). The header name is a setting so
    it can follow what Netlify actually sends.
  - **Approved trade-off (author, 2026-11-26): a seen token can be replayed until it expires.** A
    token is valid until its `exp`, so whoever holds one could replay it with an IP of their choosing
    until then, getting around per-IP limits (not per-user ones). Netlify offers no way to bind a
    token to one request. Tokens only travel from Netlify to Fly.io over TLS, so only those two could
    see one.
- IPv6 clients are limited per full address. If address rotation becomes a problem, partition by /64.
- Limits are per instance (in memory). That is fine while the API runs as one instance.
- Production deployment must keep frontend and API on one origin (proxy or shared site). Splitting
  them would need `SameSite=None`, CORS with credentials, and CSRF tokens.
- Account lockout after repeated failures is not implemented; the per-IP limits cover brute force at
  this scale.
- If the only admin's account is ever deleted (Phase 7) while the bootstrap code is still configured,
  the code would work again. Remove the bootstrap setting once the admin exists.
- Background jobs (Phase 4) run with no current user, so they will need an explicit way to act for a
  specific user when they write user-owned data.
