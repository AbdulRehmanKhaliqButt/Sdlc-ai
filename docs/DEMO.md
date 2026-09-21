# End-to-end demo

1. Create a project and paste a grooming transcript.
2. Generate structured requirements, review open questions, and approve.
3. Generate acceptance-criteria-linked QA tests and approve the QA plan.
4. Generate and approve the development plan.
5. Configure Jira/GitHub adapters for external ticket and repository actions.
6. Execute implementation on an isolated branch, run unit tests, and open a pull request.
7. Run Playwright E2E checks and retain failure traces.
8. Review audit events and telemetry as release evidence.

The deterministic provider keeps the core workflow reproducible without paid credentials. External side effects fail closed until credentials and adapters are explicitly configured.
