# ADR-0003: Request validation with a DataAnnotations endpoint filter

- **Status:** Accepted
- **Date:** 2026-11-17

## Context

Every request DTO must be validated, and invalid input must return a 400 with field-level errors
(CLAUDE.md). .NET 10 ships built-in minimal-API validation (`AddValidation()`), but it is
source-generated per project: it only finds request types used by endpoints in the project where the
generator runs. Our endpoints and DTOs live in module class libraries. Covering them needs the
experimental `[ValidatableType]` attribute (diagnostic ASP0029) on every DTO, and every module must
remember to call `AddValidation()` itself.

## Decision

Use a small endpoint filter, `RequestValidation.WithRequestValidation()` in `Heardback.Shared.Http`:

- The host maps every module's endpoints under one root route group that has the filter, so no module
  has to opt in.
- At startup, the filter finds handler parameters whose type has DataAnnotations attributes, then runs
  `Validator.TryValidateObject` on them for each request.
- Failures return a `ValidationProblem` (400) keyed by camelCase field name, matching the JSON payload.
- Positional records put their attributes on the property (`[property: Required]`).

## Consequences

- One validation mechanism for every module, built only on stable APIs.
- Uses reflection rather than generated code. That is negligible at this scale, and the parameter
  check runs once per endpoint at startup.
- Nested objects are not validated recursively. Add that if a request ever needs it.
- Revisit when the built-in validation supports types from other projects without experimental APIs.
