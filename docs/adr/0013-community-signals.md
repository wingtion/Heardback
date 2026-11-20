# ADR-0013: Community signals and the ghost-listing hint

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

Phase 6 (PLAN.md) shows members what the community has seen for a posting and a company: how many others
applied, how many heard back and how long that took, plus an explainable "likely inactive" hint (PLAN.md §6).
This is the first feature that reads other users' data, so the rules for what may leave the database are fixed
here. Heardback is a small invite-only group (about 15 people who may know each other), which makes
re-identification easier than in a large anonymous crowd.

Decided by the author on 2026-11-20: the viewer is never part of the group, at least 3 other members are
needed, numbers are shown exactly, the company page is only for companies the viewer tracks, and company names
are matched after normalization with legal suffixes removed.

## Decision

**One cross-user read path.** Every other query keeps the per-user ownership filter. The Applications module
exposes a new contract, `ICommunityOutcomes`, which is the only place that switches the filter off. It runs the
grouping in the database and returns counts only: no user id, application id, notes, posting text, link or
date of an individual application leaves it. The Insights module turns those counts into the signal; it never
sees rows. Nothing new is stored and everything is computed on each read (as in ADR-0011), so a deleted
application drops out of every aggregate immediately.

**Groups.**
- **Posting:** applications linked to the same shared posting (ADR-0004).
- **Company:** applications with the same company key. The key is the company name after Unicode
  normalization (NFC), lower-casing with the invariant culture, collapsing whitespace and punctuation, and
  removing a trailing legal suffix (Inc, LLC, Ltd, Limited, GmbH, AG, BV, SA, SAS, Corp, Co, A.Ş., Ltd. Şti.,
  ...). It is stored on each application (new column, indexed, filled for existing rows in the migration) so
  grouping runs in the database. Two different companies with the same name are merged; that is accepted.

**Who counts.**
- The **viewer's own applications are always left out** of the group they look at. They already know their own
  outcome, and leaving it out means the viewer never learns "the others" by subtracting themselves.
- Only **settled** applications count: applied at least the "No response" threshold ago (the same setting as
  the label, 14 days by default). A fresh application has not had time to get an answer, and counting only the
  answered fresh ones would make response rates look better than they are.
- **Minimum group size:** at least **3 distinct other members** with a settled application in the group.
  Below that, the API says "not enough data" and returns no numbers at all, not even the count. A company
  group counts members, not applications, so one person's ten applications are still one member.

**What is shown (exact numbers, decided by the author).** Per group: members, settled applications, how many
got a response (definition from ADR-0011: Interview, Offer or Rejected as the first status change), the
response rate as a whole percent, and the average and median days to the first response (empty when nobody
heard back).

**Ghost-listing hint (0 to 5 points).** Each point comes with one plain sentence that says why, and the UI shows
the sentences, never only the score. A point can only use a group that passed the minimum size.

| Points | Rule | Group |
| --- | --- | --- |
| 1 | The posting was first seen here at least 30 days ago | posting |
| 2 | No member heard back on this posting | posting |
| 1 | Fewer than 1 in 4 members heard back on this posting (instead of the 2 above) | posting |
| 1 | Fewer than 1 in 4 members heard back from this company | company |
| 1 | The company took 21 days or more to answer on average | company |
| 1 | The company has had this role (same normalized title) as 3 or more separate postings | company |

The total is capped at 5. Level: 0–1 Low, 2–3 Medium, 4–5 High. When neither group passes the minimum size the
hint is not shown at all (design.md §12: render only when data exists). It is worded as a hint ("likely
inactive"), not a verdict. The numbers (30 days, 1 in 4, 21 days, 3 postings, 3 members) are settings with
these defaults and are checked on start.

`times_added` from ADR-0004 is **not** used: it counts re-links after edits, and below the minimum size it
would reveal that someone else tracks the posting.

**Access.**
- `GET /api/applications/{id}/community`: the posting and company signal for one of the viewer's own
  applications (404 for anyone else's, like every other application route).
- A light badge list for the viewer's applications (level only, Medium and High), so the list and board can
  show badges without one request per row.
- `GET /api/companies/{key}`: the company page, only when the viewer has at least one application with that
  company key (404 otherwise). It shows the company aggregates and the viewer's own applications there. It
  never lists other members' postings, roles or dates at that company. There is no company search or list.
- Admins get no extra access to community data.

## Consequences

- **Accepted gap (approved by the author on 2026-11-20): exact numbers can be differenced over time.** If a
  posting showed "3 members, 0 heard back" yesterday and "4 members, 1 heard back" today, the viewer learns that
  the newest member heard back. With bands ("3–5 members", "most did not hear back") this would mostly
  disappear; exact numbers were chosen because they are clearer and more useful in a small group. The minimum
  size still prevents the far stronger case of one or two people being shown on their own.
- A result like "3 members, nobody heard back" tells the viewer that each of those three got no answer. That is
  the signal itself and cannot be avoided; it never says who they are.
- In a group of about 15 people, posting-level numbers will rarely pass the minimum; company-level numbers more
  often. The hint will often be absent, which is the honest outcome.
- The hint depends on members keeping their timelines current; an answer nobody recorded counts as no answer.
- Phase 7 (account deletion) needs nothing extra: deleting the applications removes them from every aggregate.
  Phase 8 demo sandboxes must be excluded inside `ICommunityOutcomes`, the single cross-user path.
- Integration tests (Testcontainers) prove: groups below the minimum return no numbers; the viewer's own
  applications are excluded; other users' applications are excluded from every non-community query as before;
  responses contain no ids or text from other users; a deleted application leaves the aggregates; the company
  page refuses companies the viewer does not track.
