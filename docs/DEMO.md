# End-to-end demo

1. Create a project and paste a grooming transcript.
2. Generate structured requirements, review open questions, and approve.
3. Add project-memory entries such as architecture decisions, coding conventions or domain constraints.
4. Generate acceptance-criteria-linked QA tests and approve the QA plan.
5. Generate a Playwright E2E proposal and review acceptance-criteria traceability.
6. Generate and approve the development plan.
7. Analyze a target GitHub repository to retrieve relevant code and technology signals.
8. With `AI_PROVIDER=openai`, generate a delivery run. The developer agent receives only the approved plan, bounded repository context and recalled Project Brain context.
9. SDLC AI sends the proposal to the isolated sandbox runner. The runner clones the default branch, applies the proposed files and runs the allowlisted validation commands.
10. If a command fails, the exact stdout/stderr, exit code and repository context are sent to the repair agent. The repaired proposal is tested again on a fresh clone. This repeats up to `SANDBOX_MAX_REPAIR_ATTEMPTS`.
11. Review the final proposal, all validation attempts, command logs, timings, repair count and risks in the browser. No GitHub write has happened yet.
12. Only a `ValidatedPendingApproval` delivery can be approved. Approval creates an isolated branch, writes the validated files and opens a pull request.
13. GitHub Actions independently rebuilds the platform and executes Playwright E2E coverage.
14. Review the pull request, normal CI results, audit events and sandbox evidence before merge.

## Sandbox boundaries

The runner intentionally has no Docker socket. In Docker Compose it runs with a read-only root filesystem, a temporary writable workspace, dropped Linux capabilities, a PID limit and CPU/memory limits.

Validation commands are executed directly rather than through a shell. Shell operators, redirection, absolute paths and parent-directory traversal are rejected. The allowlist covers common .NET, npm, Playwright and pytest validation commands, including safe npm `--prefix` usage for monorepos.

The GitHub token is used only by the clone operation when a private repository needs authentication. It is not placed in the validation-command environment.

## Safe demo mode

The deterministic provider keeps the workflow reproducible without paid credentials and deliberately returns **zero code changes**, so sandbox execution and repository mutation do not occur.

## Production demo configuration

- Set `AI_PROVIDER=openai`, `AI_MODEL` and `OPENAI_API_KEY`.
- Set `GITHUB_TOKEN` to a fine-grained token limited to the repository used for the demo.
- Optionally set `SANDBOX_MAX_REPAIR_ATTEMPTS` (default 2) and `SANDBOX_COMMAND_TIMEOUT_SECONDS` (default 180).
- Use a disposable demo repository until the complete branch/write flow has been reviewed for your environment.

External side effects fail closed when write credentials or passing validation evidence are absent.
