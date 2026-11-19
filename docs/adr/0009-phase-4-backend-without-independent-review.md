# ADR-0009: Phase 4 backend opened without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-19

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 4 is
split into two pull requests (ADR-0008): this backend PR (LLM client, prompts and validation, the job queue
and worker, posting analysis, quotas, CV tailoring with consent) and a later UI PR. Before asking, the
author was told that this PR adds background work, quotas, jobs that run as the requesting user and CV text
leaving the server, and that a review was recommended; an estimate (about 15-30 minutes for the review and
1-2 hours of fixes) and a shorter, focused review were offered as well.

What was checked instead:
- 618 .NET tests (unit and integration against real PostgreSQL), passing on every one of the 12 commits from
  a clean clone. They cover provider fallback and the rule that CV text never reaches a provider that may
  use it, strict answer validation and its one retry, the job queue under concurrency (the parallel-workers
  test fails without the row lock), lease loss and crash recovery, per-user quotas under concurrent
  requests, refunds, the app-wide cap, user isolation of analyses and suggestions (including a job that
  runs for Alice never reading Bob's CV), consent withdrawal and every deletion path.
- Real-provider checks with the author's keys (not part of CI): both providers accept the generated
  schemas and ignore an injected instruction; a Gemini-only setup refuses CV text. The tailoring test on
  the real API found that the model suggested skills the CV lacks, which led to medium reasoning and the
  grounding filter (ADR-0008).

## Decision

The author decided on 2026-11-19 to open the Phase 4 backend PR **without** an independent review.

## Consequences

- No reviewer who had not seen the work looked at the job queue, the quotas, the background user or the
  handling of CV text. Gaps there may be found later than they otherwise would.
- The approved trade-off in ADR-0008 (posting text may fall back to Gemini's unpaid tier) is unchanged.
- The rule in CLAUDE.md is unchanged: the Phase 4 UI PR and the end of the phase still get the question,
  and later phases end with an independent review unless the author approves a skip again.
