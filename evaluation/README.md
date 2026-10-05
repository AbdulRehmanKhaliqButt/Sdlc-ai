# SDLC AI Evaluation Suite

This folder contains controlled repositories/tasks for demonstrating and measuring SDLC AI.

## Goal

Run the same requirements through the product repeatedly and record whether SDLC AI can:

1. extract correct requirements;
2. preserve project-specific rules;
3. generate useful QA coverage;
4. propose code in the correct files;
5. pass isolated build/test validation;
6. repair failures when validation fails;
7. produce a reviewable pull request without bypassing safety gates.

## Scenarios

### 01 — User deactivation (.NET)
Target: `scenarios/01-user-deactivation`

Grooming transcript:

> Administrators can deactivate a user. A deactivated user cannot log in. Existing sessions must be invalidated. Every deactivation records the target user, administrator and UTC timestamp. Deactivation must be idempotent.

Project Brain:

- Domain services must not reference infrastructure.
- All asynchronous public APIs accept CancellationToken.
- Deactivation is idempotent.
- Authentication decisions belong in UserAccessService.

Expected implementation areas:
- User entity/state
- UserAccessService
- Session invalidation abstraction
- Audit abstraction
- Unit tests

### 02 — Pagination (.NET)
Target: `scenarios/02-pagination`

Transcript:

> Add paginated product search. Page numbers start at 1. Page size defaults to 20 and must be between 1 and 100. Return total count, page number, page size and items. Empty pages are valid.

Expected:
- validation of page/pageSize;
- deterministic ordering;
- metadata in result;
- tests for defaults, limits and an empty page.

### 03 — Monorepo validation (Node)
Target: `scenarios/03-monorepo`

Transcript:

> Add an endpoint-like function that returns a health object containing status=ok and the service name. Add tests and keep the package dependency-free.

Expected validation command:
`npm --prefix evaluation/scenarios/03-monorepo test`

## Security demonstrations

These are already enforced by the sandbox and covered by runner tests:

- `dotnet test && rm -rf /` → blocked because command chaining is forbidden.
- `../../etc/passwd` → blocked because file writes may not escape the repository.
- arbitrary `npm run` scripts → blocked unless the script is build/test/lint/typecheck.

## Suggested live-demo order

1. Start with Scenario 01.
2. Show requirements and QA generation.
3. Add the Project Brain rules.
4. Generate the implementation plan.
5. Analyze this repository with a query limited to `evaluation/scenarios/01-user-deactivation`.
6. Generate + validate the code proposal.
7. Expand sandbox evidence.
8. If a repair occurs, highlight the failing command/output and the repaired attempt.
9. Approve only after validation passes.
10. Open the generated PR and review normal CI.

## Recording results

Use `evaluation/results-template.csv` after every run. Keep failures; they are more valuable than cherry-picked successes.
