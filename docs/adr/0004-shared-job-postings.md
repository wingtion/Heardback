# ADR-0004: Shared job postings with per-application copies

- **Status:** Accepted
- **Date:** 2026-11-18 (revised the same day after the independent Phase 2 review: per-user duplicate keys,
  checks before catalogue writes, cleaned stored links)

## Context

Heardback's community signals ("many members applied to this posting and nobody heard back") and the
later posting analysis need to know when two users applied to the same job. Users enter a job in different
ways: the same link copied with tracking parameters, the same LinkedIn job under several URLs, or the
posting text pasted without a link. At the same time, one user must never be able to change what another
user sees, and users should not see text that someone else pasted.

## Decision

**One shared catalogue.** The Postings module owns `postings.job_postings`. A row holds the first submitted
link, company, role and text, a normalized link, a content hash, and when and how often it was submitted.
It holds **no user id**. Other modules reach it only through `IJobPostings` in the module's contracts.

**Matching, in order:**
1. **Normalized link.** Lower-case host without `www.`, no scheme, no fragment, no trailing slash, the
   default port dropped, tracking parameters (`utm_*`, `gclid`, `fbclid`, `trk`, `ref`, ...) removed and the
   rest sorted. LinkedIn job links (`/jobs/view/<slug>-<id>`, `?currentJobId=<id>`) become
   `linkedin.com/jobs/view/<id>`. Path case is kept, since some sites use case-sensitive ids.
2. **Content hash.** SHA-256 of company, role and text after Unicode normalization (NFC), whitespace
   collapsing and lower-casing. It is only computed when there is posting text, because company and role
   alone are too common to say two submissions are the same job.
3. Without a link and without text, every submission is a new posting.

When the link matches one posting and the text matches another, the link wins.

**Concurrency.** Both the normalized link and the content hash have unique indexes. When two users submit
the same job at once, one insert fails on the index, and that request looks the posting up again and uses
the winner's row. An integration test submits the same posting from eight concurrent requests and expects
one row.

**Postings are never edited.** After a posting is created, only `last_seen_at` and `times_added` change.

**Each application keeps its own copy** (decided by the author on 2026-11-18). The Applications module
stores the company, role, link and posting text the user entered, and that is what the user sees and
edits. The shared posting is only used to recognise the same job across users, for later analysis and for
community data. When a user edits the link or text, the application is linked again to whichever posting
now matches (a new one if none does). If that posting is one the user already tracks in another
application, the edit is refused ("Already tracked").

**"Already tracked" is decided per user** (revised after the independent review). Each application also
stores its own link key and content hash, computed with the same rules (`PostingKeys` in the Postings
contracts), with unique indexes on (user, link key), (user, content hash) and (user, posting). Before the
shared catalogue is touched, a submission is checked against the user's own keys. The posting id is checked
after resolving as well, because a submission can match a posting through another user's link or text.
This way the result never depends on what other users happened to submit first: a posting matched by text
does not record the second link, so a catalogue-only check missed it. The domain checks (dates, version)
also run before the catalogue is touched, so a refused request leaves no shared posting behind.

**No fetching.** The server never downloads posting pages (PLAN.md: no scraping). This also rules out
server-side request forgery through user-supplied links.

## Consequences

- The same job is recognised across users without anyone seeing another user's text or edits.
- Posting text is stored more than once (once per application, once in the posting). That is negligible at
  this scale.
- The first submitter's text stays in the shared posting and feeds later analysis, which is why the form
  warns against personal notes in the posting text.
- A posting first added without text never gains text later. Phase 4 (analysis) decides whether an empty
  posting may take the text of a later submission.
- Matching only by link misses the same job posted on two sites without text. The content hash covers the
  case where the text is pasted.
- `times_added` counts submissions, including re-links after an edit, so it slightly overcounts. A
  submission refused only by the posting-id check (it matched the user's own posting through someone else's
  link or text), or one that loses a race on the unique indexes, is counted too.
- The shared posting stores the first link without credentials, tracking parameters or fragment.
