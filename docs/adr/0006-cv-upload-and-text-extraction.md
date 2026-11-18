# ADR-0006: CV upload, text extraction and personal details

- **Status:** Accepted (includes an approved trade-off)
- **Date:** 2026-11-18

## Context

Phase 3 matches a user's CV against their job postings without an LLM (PLAN.md). Users upload a CV PDF.
PLAN.md requires that only the extracted text is kept, with personal details removed; the original file is
never stored.

Reading a PDF means running a parser over a file from the user. PDFs can be crafted to cost the server a lot:
a few kilobytes of compressed data can unpack into gigabytes (a "decompression bomb"), and the parser has
no built-in cap for that. .NET cannot cap memory for a single request either.

## Decision

**Server-side extraction with PdfPig** (`PdfPig` 0.1.16, Apache-2.0, pure .NET; approved by the author). The
upload is read into memory, its text extracted, and the bytes are dropped when the request ends. One CV per
user; a new upload replaces the old one.

**Limits, all in `MatchingSettings` and checked in this order:**

| Limit | Default | Why |
|---|---|---|
| Request size | 5 MB + 64 KB | The server stops reading a larger body (413) |
| File size | 5 MB | Clear field error for slightly larger files |
| PDF header | `%PDF-` in the first kilobyte | Rejects Word files and images before parsing |
| Pages | 10 | A CV is a few pages |
| Decompressed data | 32 MB per document | Stops decompression bombs (see below) |
| Text | 50,000 characters | Rejected rather than cut, so no skills are lost silently |
| Uploads | 5 per user per hour | Every attempt counts, rejected files too |

**Decompression budget.** PdfPig lets us supply the decoding filters. Before PdfPig unpacks a Flate stream
(what nearly every PDF exporter uses), our filter inflates it once into a small scratch buffer that is thrown
away, only counting bytes, and stops at the budget. If the stream would not fit what is left of the 32 MB
document budget, the whole PDF is rejected before PdfPig allocates anything. LZW and run-length streams can
expand just as badly, are not produced by today's exporters, and are refused outright. Image filters are left
alone because reading text never decodes images. Unit tests show both checks fail without the budget.

**Personal details are removed before anything is stored:** email addresses, phone numbers and other 10 to
15 digit runs (such as Turkish national ID numbers), links, and lines that look like a postal address. Each
becomes a visible marker (`[email]`, `[phone]`, `[link]`, `[address]`) so the user can see what was taken
out. Names are kept: PLAN.md does not list them, and guessing names would remove skills too.

**Logging** records page and character counts and exception types only; never text or exception messages,
which can quote the document.

## Consequences

- **Approved trade-off (2026-11-18).** The budget covers decompression, the known way a small PDF becomes
  expensive, but not every possible parser cost: within the limits, a crafted PDF can still keep one request
  busy for a while, and a PdfPig bug could cost more than expected. The alternatives were reading the PDF in
  the browser (pdf.js; a large frontend dependency) or in a separate worker process with its own memory and
  time limits (more to build and deploy). The author chose server-side extraction with limits: only invited
  users can upload, and the hourly limit caps how often anyone can try.
- Scanned CVs (images without a text layer) are rejected with a clear message; there is no OCR.
- Address detection is a heuristic (Turkish and English address words plus a number). A rare address line
  may stay, and a rare non-address line with such a word and a number may be replaced.
- The stored text is the user's own data: they can read it back exactly as stored and delete it at any time.
