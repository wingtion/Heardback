# ADR-0011: Personal insights and follow-ups

- **Status:** Accepted
- **Date:** 2026-11-19

## Context

Phase 5 (PLAN.md) gives each user numbers from their own applications: applications per week, response rate
per channel, time to first response and a status funnel, plus an in-app list of applications that got no
response. The numbers must mean something the user can trust, so their definitions are fixed here. The
Insights module may not read the Applications module's tables (ADR-0001).

Decided by the author on 2026-11-19: no chart library (the UI draws simple SVG charts), reminders in the app
only (no email), and no AI-drafted follow-up message in this phase. The design of the dashboard is decided
after the backend.

## Decision

**Data access.** The Applications module exposes `IApplicationHistory` in its contracts: the signed-in user's
applications with company, role, channel, applied date, last activity, the "No response" days and their status
changes in timeline order. Nothing else (no posting text, notes or links). The ownership filter keeps it to
the caller's own applications. Everything is computed on each read, like the match (Phase 3), so it is never
stale and nothing new is stored.

**Definitions.**
- **Response:** the first status change after the submission: Interview, Offer or Rejected (also Applied
  again, which the free status transitions allow, counts as no response). A note on its own is not a
  response. Rejected counts: the question is "did I hear back?" (approved by the author), and the ghost-listing
  signal in Phase 6 builds on the same definition.
- **Response rate:** responded applications / all applications, per channel and overall, as a whole percent.
  Channels without applications are left out.
- **Time to first response:** days from the applied date to the first response, over responded applications
  only; the average (one decimal) and the median. Empty when nothing was answered.
- **Applications per week:** weeks start on Monday and count by applied date; the last 12 weeks including the
  current one, with empty weeks as zero.
- **Funnel:** how many applications reached at least each stage: Applied (all), Interview (Interview or
  Offer at any point), Offer. It counts the highest stage ever reached, not today's status, so an interview
  that ended in a rejection still counts as an interview. Rejections are given as a separate total.
- **Follow-ups:** the applications that show "No response · N days" (Applied, last event at least the
  threshold old, ADR of Phase 2), oldest first, paged. Marking one as followed up is an ordinary note ("Add
  update"): it moves the last activity, so the application leaves the list until the threshold passes again.
  No new state is stored.

## Consequences

- Numbers depend on the user keeping their timeline up to date; an answer that was never recorded counts as no
  response.
- With few applications the percentages swing a lot; the UI should show the counts behind each rate.
- Recomputing on every read loads the user's applications and status changes. At this scale (hundreds of
  applications per user) that is cheap; if it ever is not, the numbers can be cached per user.
