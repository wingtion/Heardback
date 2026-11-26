# ADR-0018: Demo accounts ("Try the demo")

- **Status:** Accepted
- **Date:** 2026-11-26

## Context

Heardback is live as a portfolio project (Phase 8, PLAN.md). Employers rarely get past a sign-in wall, and the
app is invite only, so the live site needs a way in without an invite or a password. PLAN.md Phase 8 asks for a
one-click demo: a temporary per-visitor sandbox with sample data, removed after about a day, without admin
access, with a small AI quota, excluded from community statistics, and rate limited.

Decided by the author on 2026-11-26: at most 20 demo visitors at once, 3 new demos per IP per hour, 3 posting
analyses and 2 CV suggestions per demo per day. Demo visitors must never see anything derived from members'
applications, not even aggregated community numbers; a few permanent made-up "neighbor" accounts give the demo
its community signals instead.

## Decision

### Account kinds

- `identity.users.kind` is `Member`, `DemoVisitor` or `DemoNeighbor`. Existing users became `Member`.
- **Demo visitors** have no password, a made-up address under `demo.invalid` (a reserved top-level domain that
  can never receive mail), and `expires_at` = creation + `Identity:Demo:Lifetime` (1 day). Registration refuses
  `demo.invalid` addresses, so a real account can never collide with one.
- **Demo neighbors** (4, `DemoNeighbors.Count`) are permanent, have no password, and exist only so their
  applications can feed the demo's community view.
- A demo visitor's access token carries `account: demo`, read as `ICurrentUser.IsDemo`, so modules can apply
  demo rules without a database lookup.

### Starting a demo

- `POST /api/auth/demo`, anonymous, returns the same response and refresh cookie as sign-in. Off unless
  `Identity:Demo:Enabled` is true (404 otherwise); production turns it on in `fly.toml`.
- **Limits:** a per-IP fixed window (`VisitorsPerIp`, 3 per `IpWindow`, 1 hour), and a cap of
  `MaxActiveVisitors` (20) unexpired visitors. The cap check and the insert run under a Postgres advisory lock,
  so concurrent requests cannot exceed it. A full demo answers 429 with `Retry-After` set to when the oldest
  visitor ends.
- **Sample data:** a new Shared contract, `IDemoDataSeeder`, is implemented by each module with something to
  show (Applications: the 8 sample applications plus 8 more spread over eight weeks; Matching: a made-up CV with
  no contact details). Identity runs them in a fresh service scope whose `BackgroundUser` is the new account,
  so rows are stamped and checked by the normal ownership rules. `ClaimsCurrentUser` now prefers a scope's
  explicitly set `BackgroundUser` over the request's token: the sign-up request itself is anonymous. If seeding
  fails, the account is removed at once rather than left half filled.
- The session's absolute lifetime is capped at the account's `expires_at`, so a refresh can never outlive the
  demo.

### What a demo visitor cannot do

- Admin pages and invite codes (not an admin), sign in with a password (login only matches members), change a
  password (403 `account.demo_no_password`), receive a reset code (members only, including the owner command),
  or appear in the Members list. Deleting the account needs a password, so a visitor simply signs out; the
  data goes when the demo ends.
- AI: `Applications:DemoPostingAnalysesPerDay` (3) and `Matching:DemoTailoringPerDay` (2) instead of the member
  limits. They still share the global daily LLM budget.

### Community statistics

`CommunityAudience` decides whose applications a viewer's community numbers may count (ADR-0013 still applies:
minimum three others, viewer excluded, counts only):

- **Members** count only members. Demo visitors and neighbors are excluded, so demo traffic never shapes a
  real signal.
- **Demo visitors** count only the neighbors. They never see anything derived from members, and other
  visitors do not affect what they see either. The neighbors' applications are arranged to show each state:
  a posting where almost nobody heard back, one where nobody did, a responsive company, and a posting with too
  few members to show numbers.

All sample companies and links are made up (`.example` domains), so demo postings cannot match a real posting
by link. A member who pastes one of those links would share the posting row (ADR-0004), but still never sees
demo accounts in the numbers.

### Removal

`DemoMaintenanceService` (a hosted service, only when the demo is on) creates missing neighbors at startup,
then removes expired visitors at startup and every `CleanupInterval` (1 hour). Removal goes through
`AccountRemover`, the same erasers-then-identity path as deleting one's own account (ADR-0015), in a scope
running as that visitor.

## Consequences

- The Fly.io machine sleeps when idle, so a demo can outlive its `expires_at` until the machine next wakes. In
  that time the visitor cannot refresh their session, the cap no longer counts them, and nobody else sees their
  data; the rows are removed on the next startup.
- Per-IP limits live in memory, so they reset when the machine restarts or sleeps. The 20-visitor cap is in the
  database and always holds, which is what protects the free database tier.
- Neighbors are permanent rows. Changing the neighbor data later needs a migration-like step (delete the
  neighbors and let the next startup recreate them).
- Anyone can create a demo, so its sample data is the only thing ever shown to anonymous visitors.
