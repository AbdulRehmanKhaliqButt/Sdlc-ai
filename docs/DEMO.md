# End-to-end demo

1. Create a project and paste a grooming transcript.
2. Generate structured requirements, review open questions, and approve.
3. Add project-memory entries such as architecture decisions, coding conventions or domain constraints.
4. Generate acceptance-criteria-linked QA tests and approve the QA plan.
5. Generate a Playwright E2E proposal and review acceptance-criteria traceability.
6. Generate and approve the development plan.
7. Analyze a target GitHub repository to retrieve relevant code and technology signals.
8. With `AI_PROVIDER=openai`, create a delivery run. The developer agent receives only the approved plan, bounded repository context and recalled project memory.
9. Review the proposed full-file changes, validation commands and risks. No repository write has happened yet.
10. Approve the delivery run. SDLC AI creates an isolated branch, writes the approved files and opens a pull request.
11. GitHub Actions builds .NET, tests the Python AI service, builds Next.js and executes the Playwright gate.
12. Review the pull request, CI results, audit events and telemetry as release evidence.

## Safe demo mode

The deterministic provider keeps the workflow reproducible without paid credentials and deliberately returns **zero code changes**. This proves that the delivery contract cannot mutate a repository accidentally in local/CI mode.

## Production demo configuration

- Set `AI_PROVIDER=openai`, `AI_MODEL` and `OPENAI_API_KEY`.
- Set `GITHUB_TOKEN` to a fine-grained token limited to the repository used for the demo.
- Use a disposable demo repository until the branch/write flow has been reviewed for your environment.

External side effects fail closed when write credentials are absent.
