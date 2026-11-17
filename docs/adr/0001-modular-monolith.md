# ADR-0001: Modular monolith with contract-only module boundaries

- **Status:** Accepted
- **Date:** 2026-11-17

## Context

JobTrail serves a small invite-only community (a handful to a few dozen users) and is built and run
by one person. Microservices would add deployment, networking and consistency costs with no benefit
at this scale. A plain layered monolith, though, tends to grow hidden coupling between features
(shared tables, reaching into another feature's classes), which makes the code hard to reason about
and to change.

## Decision

Build one deployable ASP.NET Core API made of modules (`JobTrail.Modules.*`), one project each, with
boundaries enforced by the compiler and by tests:

1. **Public surface = contracts.** A module's public types live only in its
   `JobTrail.Modules.<Name>.Contracts` namespace. The single exception is the module's registration
   class `JobTrail.Modules.<Name>.<Name>Module`, which the host calls to wire the module up.
   Everything else is `internal`.
2. **Module-to-module references are for contracts only.** A module project may reference another
   module project, but may only use types from that module's `Contracts` namespace.
3. **Data is private.** Each module owns its own `DbContext` and Postgres schema. No module reads or
   writes another module's tables.
4. **Enforcement.** Architecture tests in `JobTrail.UnitTests/Architecture` check that:
   - every module project in `src/` is inspected;
   - module assemblies export only contracts plus the registration class;
   - no module's metadata references another module's non-contract types. This reads the compiled
     assembly's type references, so it also catches leaks through `InternalsVisibleTo`.

   Contracts stay a namespace inside the module project rather than a separate `*.Contracts`
   project. If a module ever needs a contracts-only dependency (for example, to break a reference
   cycle), that module's contracts move to their own project.

## Consequences

- One process to deploy, debug and monitor. Cross-module calls are in-process method calls.
- Boundaries are checked on every build in CI, not just by review.
- Two modules cannot reference each other (project references cannot be cyclic). If that need
  arises, it is a design signal: move the shared concept, invert the dependency with an interface
  owned by one side, or split the contracts into their own project.
- Modules could later be extracted into services along the same seams, though that is not a goal.
