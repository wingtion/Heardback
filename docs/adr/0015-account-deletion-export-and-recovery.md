# ADR-0015: Account deletion, data export and account recovery

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

Phase 7 (PLAN.md) adds the privacy baseline of PLAN.md §7: a member can delete their account and everything
personal with it, and can download their own data. It also adds a way back in after a forgotten password or
email. Heardback is a small invite-only group (about 15 people the author knows) with no email infrastructure,
so recovery goes through people, not mail (decided 2026-11-18, PLAN.md Phase 7).

Data is spread over five modules, each with its own schema: Identity (user, invite codes, sessions),
Applications (applications, timeline, posting analyses), Postings (shared postings, no user id, ADR-0004),
Matching (CV, AI consent, tailoring results) and the LLM infrastructure (analysis jobs, daily usage).

Decided by the author on 2026-11-20: shared postings that only the deleted member used are deleted with the
account, and member reset codes are valid for 24 hours and work once.

## Decision

### Account deletion

- **Endpoint:** `DELETE /api/account` with the current password in the body. A stolen access token alone cannot
  delete an account, and a slip of the mouse cannot either. A wrong password returns 400 with a field error and
  counts against the per-IP credentials rate limit (ADR-0002), so it cannot be used to guess passwords.
- **Each module deletes its own data.** A new Shared contract, `IUserDataEraser`, is implemented by every module
  that keeps user data. The Identity module runs them for the signed-in user and then deletes the user itself.
  No module touches another module's tables. Order: Applications (which also cleans up postings, below),
  Matching, LLM, and finally Identity (sessions, invite codes, user). All erasers are idempotent, so if a step
  fails the member can simply try again and the user row, deleted last, keeps the account usable until
  everything else is gone.
- **What goes:** applications with their timeline and notes, posting analyses no other application uses,
  CV text and skills, AI consent, tailoring results, queued or finished analysis jobs for the user, daily LLM
  usage rows, all refresh tokens, invite codes the member created that are still unused, and the user row.
  Invite codes the member used or created that were already used are deleted too: they only record who invited
  whom, and keeping them would keep the deleted member's id.
- **Shared postings (author's decision):** a posting that, after the member's applications are gone, no
  application refers to is deleted. A posting another member also tracks stays, because it is part of their
  application. The same rule now applies when a single application is deleted, so a posting text never outlives
  the last application that uses it (Phase 2 kept it; this changes that).
- **Race with a member adding the same posting.** Without care, member B could link a new application to a
  posting in the moment member A's deletion decides that nobody uses it, leaving B's application pointing to a
  deleted posting. Both sides therefore take a Postgres transaction-level advisory lock keyed on the posting id:
  deletion takes it before checking "nobody uses it" and holds it until the posting is gone; linking takes it,
  checks the posting still exists (re-resolving it if not) and holds it until the application is saved. Advisory
  locks are a database feature, not another module's table, so the module boundary holds. An integration test
  runs both at once.
- **Last admin:** the only remaining admin cannot delete their account (409), so the group never loses its
  last admin through the app.
- **Aggregates:** community signals and personal insights are computed on each read (ADR-0011, ADR-0013), so a
  deleted member drops out of every number immediately. Nothing derived is stored.
- **Proof:** an integration test creates a member with data in every module, deletes the account, then scans
  every table in every module schema that has a `user_id` (or similar owner) column and asserts that no row
  carries the member's id; a second member's data is untouched. New tables are picked up automatically.
- **Logs** record that an account was deleted, by user id only.

### Data export

- **Endpoint:** `GET /api/account/export` returns one JSON file (`Content-Disposition: attachment`) with
  everything the member can see about themselves: profile (email, display name, joined), applications with
  timeline and notes, their copy of the posting text, posting analyses, CV text and skills, AI consent, and
  tailoring results. Other members' data and community numbers are not included.
- Each module contributes its part through a Shared contract `IUserDataExporter`; the Identity module builds
  the file. It is rate limited per user (a few per hour), because it reads everything at once.
- The file format is versioned (`formatVersion`) so a later import or change stays readable.

### Account recovery (no email)

- **Admin-issued reset code for members.** An admin opens the member list (`GET /api/admin/members`: display
  name, email, joined, admin or not; admins only) and issues a reset code for a member
  (`POST /api/admin/members/{id}/reset-code`). The code uses the invite-code alphabet and length (60 bits), is
  stored only as a SHA-256 hash, is shown once, expires after **24 hours**, works **once**, and issuing a new one
  replaces the old. The admin passes it on in person or by message.
- **Using the code:** `POST /api/auth/reset-password` (anonymous) with the code and a new password that follows
  the normal password policy. It is limited per IP with the credentials policy. On success the password changes,
  the code is used up and **every session of that member is revoked**. A wrong or expired code gives one generic
  error that does not say which.
- **Forgotten email:** the member asks an admin, who can see it in the member list.
- **Admins:** an admin cannot issue a code for another admin (403), so one admin cannot take over another. Admins
  recover through the owner command.
- **Owner command on the server:** `Heardback.Api reset-code <email>` prints a reset code for any user
  (admins included) and `Heardback.Api list-admins` prints the admins' names and emails. They run with server
  access only (for example `fly ssh console`) and never start the web server. The command prints a code rather
  than taking a new password, so no password ends up in shell history or process lists.
- Issuing and using a code is logged with user ids only, never the code.

## Consequences

- An admin can take over a member's account by issuing a code and using it. This is inherent in admin-run
  recovery for a group without email; it is limited to non-admin accounts, logged, and the member notices because
  their sessions end.
- Deleting a single application now also deletes its posting when nobody else tracks it. Community history for
  that posting (first seen, times added) is lost with it, which only matters for postings nobody tracks anymore.
- Linking an application to a posting takes a short lock; at this scale that costs nothing measurable.
- Every new module table with user data needs an eraser and an exporter; the deletion test fails if an eraser is
  missing, and the export has a test per module part.
