# ADR-0002: Authentication, sessions and invite-only registration

- **Status:** Accepted
- **Date:** 2026-11-17 (revised the same day: absolute session lifetime, revoke-all on token reuse,
  serialized bootstrap claim, rate limiting)

## Context

JobTrail is a small invite-only community that stores personal job-search data. Each user's data has to
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
can never register with the same code.

**First admin.** The first admin registers with a one-time bootstrap code from configuration.
- The code stops working once an admin exists.
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
- Within those 30 seconds, for example when two tabs refresh at the same moment, the old token is just
  rejected.
- The swap itself is a conditional `UPDATE`, so when two refreshes of the same token race, exactly one
  wins.
- Logout revokes only that session's family; other devices stay signed in.

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
- Refresh has its own budget, 30 per minute.
- Rejections are 429 ProblemDetails with `Retry-After`.
- Limits are configured under `Identity:RateLimits`.
- Fixed windows were chosen because they report an exact retry time. They allow up to twice the limit
  across a window boundary, which is acceptable at these limits.

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
- **Behind a proxy, rate limits see the proxy's IP.** Until forwarded headers are configured, every user
  shares one budget. Phase 8 must set up `ForwardedHeaders`, trusting only the hosting proxy
  (Fly.io's `Fly-Client-IP` or the Netlify proxy), never arbitrary `X-Forwarded-For` values.
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
