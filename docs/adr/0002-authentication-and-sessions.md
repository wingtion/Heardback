# ADR-0002: Authentication, sessions and invite-only registration

- **Status:** Accepted
- **Date:** 2026-11-17

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
can never register with the same code. The first admin registers with a one-time bootstrap code from
configuration, which stops working once an admin exists.

**Access tokens.** Access tokens are HS256 JWTs that last 15 minutes. They carry only `sub` (the user
id) and `role`, never PII. Expiry is checked against the injected `TimeProvider`, with zero clock skew.
The signing key comes from user-secrets or an environment variable and must be at least 32 bytes.

**Refresh tokens.** Refresh tokens are opaque 256-bit random values. Only their SHA-256 hash is stored.
- Every refresh swaps the current token for a new one in the same *family*: the chain of tokens that
  started with one login. The new token's 14-day lifetime starts at the refresh.
- Replaying a token that was already swapped out, more than 30 seconds later, is treated as theft and
  revokes the whole family.
- Within those 30 seconds, for example when two tabs refresh at the same moment, the old token is just
  rejected.
- The swap itself is a conditional `UPDATE`, so when two refreshes of the same token race, exactly one
  wins.
- Logout revokes only that session's family; other devices stay signed in.

**Transport.** The refresh token travels only in an `httpOnly`, `Secure`, `SameSite=Strict` cookie
scoped to `/api/auth`. The access token lives in memory in the Angular app, never in `localStorage`, so
an XSS bug cannot steal a long-lived credential.

**Same-origin.** The frontend and API share one origin. In development the Angular dev server proxies
`/api`; in production Netlify will forward `/api/*` to the Fly.io API (Phase 8). This is what makes
`SameSite=Strict` work, and it removes the need for CORS.

**Authorization.** A fallback policy requires an authenticated user everywhere. Anonymous endpoints
(health, register, login, refresh, logout) are marked explicitly. The `Admin` policy requires the
`admin` role. The current user's id is read only from the validated token, through `ICurrentUser`.

**Data isolation.** Module DbContexts derive from `ModuleDbContext`, which applies a named query filter
to every `IUserOwned` entity. It also stamps the owner on insert and refuses updates or deletes of
another user's rows. Architecture tests require every entity with a `UserId` to be either filtered or
explicitly exempted with a stated reason (`RefreshToken` is exempt because it is looked up before the
caller is known).

## Consequences

- An access token stays valid for up to 15 minutes after logout. That is accepted for this scale. A
  denylist could be added later if needed.
- Because each refresh extends the lifetime, an active user stays signed in indefinitely. An absolute
  session cap can be added later by keeping each family's start time.
- Production deployment must keep frontend and API on one origin (proxy or shared site). Splitting
  them would need `SameSite=None`, CORS with credentials, and CSRF protection.
- Rate limiting and lockout for the auth endpoints are planned for Phase 7.
- Background jobs (Phase 4) run with no current user, so they will need an explicit way to act for a
  specific user when they write user-owned data.
