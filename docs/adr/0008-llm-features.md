# ADR-0008: LLM features: providers, background jobs, quotas and privacy

- **Status:** Accepted (includes an approved trade-off)
- **Date:** 2026-11-19

## Context

Phase 4 adds two LLM-backed features (PLAN.md):

1. **Posting analysis:** turn pasted posting text into structured fields (company, title, seniority, skills,
   work mode).
2. **CV tailoring suggestions:** advice for one application, based on the user's CV and that posting.

PLAN.md fixes the frame: Groq first, Gemini as fallback, free tiers, JSON output validated against a schema,
a per-user daily quota, posting analysis cached by content hash, personal details removed before CV text
leaves the server, and a Postgres-backed job table processed in-process. The app must keep working when the
LLM is disabled or rate-limited.

Two facts about the providers shaped the decisions below (checked 2026-11-19):

- **Gemini unpaid tier:** Google uses submitted content and responses to improve its products, human
  reviewers may read it, and the terms say not to submit personal information. The paid tier does not use
  prompts for product improvement.
- **Groq:** does not keep inference data by default; it may log inputs and outputs for up to 30 days for
  abuse and reliability checks, unless Zero Data Retention is turned on in the console. Its services
  agreement does not allow training on customer inputs and outputs without permission.

## Decision

### Providers

- `ILlmClient` in `Heardback.Infrastructure.Llm`, with one implementation per provider that calls the REST
  API through `HttpClient`. **No SDK or new package** (approved by the author).
- Every request names its **data class**: `Public` (posting text) or `Personal` (anything with CV text).
- Order: Groq, then Gemini on rate limits (429), server errors (5xx) or timeouts. Client errors (4xx other
  than 429) do not fall back; they are our bug.
- **`Personal` requests never go to a provider whose data may be used for training.** Each provider has a
  `AllowsPersonalData` setting; Gemini defaults to `false` (unpaid tier). With the default settings, CV
  tailoring only uses Groq; if Groq is unavailable the job waits and retries.
- Models, timeouts and API keys come from options validated on start. A provider without an API key is
  skipped; **with no key at all the LLM is disabled**: nothing is queued, the API reports the feature as unavailable, and every non-LLM feature
  works as before.
- Every call logs provider, model, prompt id and version, latency, token counts and outcome. Never prompt
  or response text.

### Prompts and output validation

- Each feature keeps its prompts in one class with an id and a version (for example `posting-analysis/v1`).
  The version is stored with every result so old results can be recognised after a prompt change.
- The response is requested as JSON with a JSON schema attached (Groq strict mode, Gemini response
  schema). The schema is **generated from the answer record** (`JsonSchemaExporter`), so the two cannot
  drift apart. The answer is parsed strictly into that record (unknown, missing, duplicate or null-for-
  non-null fields, wrong types, numbers for enums, undefined enum names, code fences and trailing text are
  refused; enum names are matched without case), then checked with the prompt's own rules: list sizes,
  string lengths, no blank strings.
- Invalid output is retried **once** with the same input; a second invalid answer fails the job without
  further retries (PLAN.md).
- Model output is untrusted: posting text can contain instructions aimed at the model ("prompt
  injection"). Output is only ever stored and shown as plain text, never executed or used as HTML, and the
  validation rules cap its size. Deterministic matching (Phase 3) does not use LLM output.

### Background jobs

- Table `llm.analysis_jobs`: kind, subject key, status (`Pending`, `Running`, `Succeeded`, `Failed`),
  attempts, next run at, lease until, error code, created by (user id, for quota refunds), timestamps.
- **The payload holds ids and hashes only, never text.** The handler loads the current text when the job
  runs, so a deleted CV or application simply ends the job as `Succeeded` with nothing to do, and no user
  text sits in the job table.
- A `BackgroundService` claims one due job at a time with `FOR UPDATE SKIP LOCKED` and gives it a lease
  (5 minutes) and a fresh lease token. Only the holder of that token can record the outcome, so a worker
  that lost its lease cannot overwrite newer state. A job whose lease expired (the process died mid-run) is
  claimed again as its next attempt; once the attempts are used up it is marked `Failed`
  (`lease_expired`), so a job that crashes the process cannot loop forever. Several app instances can run
  the worker safely; an integration test fails without the row lock.
- Transient failures (provider down, rate limit, timeout, a handler exception) are retried with
  exponential backoff (30 s, doubling, at most 1 hour, ±20% jitter), never sooner than `Retry-After`, up
  to 6 attempts; then the job is `Failed`. A handler can also defer a job to a known time (a quota reset)
  without using an attempt. On shutdown, a running job is put back without using an attempt.
- The worker stays idle while the LLM is disabled.
- A partial unique index allows only one unfinished job per kind and subject key, so double clicks and
  races do not queue the same work twice.
- Handlers are registered by the module that owns the data (Postings for posting analysis, Matching for
  tailoring). The worker only knows the handler interface.

### Posting analysis

- **Queued only when the user asks** (`POST /api/applications/{id}/posting-analysis`; decided by the author
  on 2026-11-19, so test postings never use the LLM). Adding a posting calls no LLM.
- The input is the **application's own copy** (company, role, posting text), not the shared posting's
  first-submitted text: an application found by link can carry different text, and nobody may see an
  analysis of someone else's text (ADR-0004). Results are stored per content hash of that copy in
  `applications.posting_analyses`, so identical text is analysed once and shows as ready for every user who
  has it, without asking again. The job reads the text by hash with the ownership filter lifted for that one
  read; every row with the hash holds the same text.
- Before sending, the same personal-details filter as for CVs runs over the text (now in
  `Heardback.Shared.Privacy`, ADR-0006), and the text is wrapped as untrusted data.
- `GET /api/applications/{id}/posting-analysis` reports `Unavailable`, `NoPostingText`, `NotRequested`,
  `Queued`, `Running`, `Ready` or `Failed`. Editing the text changes the hash, so the old analysis no longer
  applies. A failed analysis can be requested again.
- Tested once on real providers with an English, a Turkish and a vague posting: both gave usable fields
  and ignored an injected instruction; Groq was slightly more precise on titles.

### CV tailoring suggestions

- Requested with a button, per application. Needs the user's **opt-in** ("send my CV text to the AI
  provider"), stored per user and revocable. Without it the endpoint refuses with a clear reason.
- Input is the stored CV text (personal details already removed on upload, ADR-0006) and the posting text.
- The result is stored with fingerprints of the CV text and posting text it was made from. When either
  changes, the result is shown as out of date; reading a result never calls the LLM.

### Quotas

All numbers are options, so they change without a code change:

| Limit | Default | When reached |
|---|---|---|
| Tailoring requests per user | 10 per day | 429 ProblemDetails with the reset time |
| New posting analyses a user can trigger | 30 per day | The job waits until the next day; adding the posting still works |
| All LLM calls, app-wide | Below the providers' free daily limits | Jobs wait until the next day; the UI says AI is busy today |

- Days reset at **midnight UTC**; the UI shows the reset in local time.
- Counters live in `llm.usage` (user, day, kind). The check and the increment are one `INSERT ... ON
  CONFLICT DO UPDATE ... WHERE count < limit`, so concurrent requests cannot pass the limit.
- A use is counted when the job is queued and **given back when the job finally fails**.
- The admin has the same limits as everyone.

## Consequences

- **Approved trade-off (2026-11-19).** Posting text goes to Gemini's unpaid tier when Groq is unavailable,
  where Google may use it and people may read it. Posting text is normally a public job ad, the Add form
  already warns against personal notes, and personal details are filtered out first, but a user could still
  paste something personal that the filter does not recognise. Alternatives: (a) accept it; (b) no
  Gemini fallback while only the unpaid tier is available: when Groq is down, posting analysis waits and
  retries; (c) use a paid Gemini key. The author chose (a): posting analysis keeps working when Groq is
  down, and the fallback can be closed later by moving to a paid key without a code change.
- **Author action before production:** turn on Zero Data Retention in the Groq console, and enter the real
  free-tier limits from the Groq console (Settings, Limits) into the app-wide cap.
- The app-wide cap protects the shared free tier from one user or a bug, at the cost of AI features pausing
  for everyone until the next day when it is reached.
- The Gemini terms only allow paid services for API clients offered to users in the EEA, Switzerland and the
  UK. The current users are in Turkey; if that changes, use a paid key.
- The job table is LLM-specific for now (`llm` schema). If non-LLM background work appears later, it can
  move to its own project.
- **For Phase 7 (account deletion):** a posting analysis outlives the applications that led to it. It holds
  only fields read from the text (company, title, skills, ...), no user id, but account deletion must also
  remove analyses whose content hash no application uses any more.
