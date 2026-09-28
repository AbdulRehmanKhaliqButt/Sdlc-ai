# SDLC AI

An enterprise-oriented, human-in-the-loop AI software delivery platform.

## Workflow

Grooming transcript → structured requirements → Product Owner approval → QA test plan → QA approval → development plan → developer approval → repository intelligence → AI code-change proposal → isolated sandbox validation → AI repair loop when needed → human approval → GitHub branch/PR → CI → Playwright evidence.

## Architecture

- **Web:** Next.js + TypeScript
- **Core workflow API:** ASP.NET Core
- **AI boundary:** FastAPI + Pydantic, deterministic or OpenAI provider
- **System of record:** PostgreSQL (Supabase-compatible)
- **Repository delivery:** GitHub REST adapter with explicit write approval
- **Sandbox runner:** isolated FastAPI worker with .NET, Node, Python and Git tooling
- **Project brain:** persisted project memory recalled during repository analysis and code generation
- **E2E:** Playwright
- **Runtime:** Docker Compose
- **CI/CD:** GitHub Actions

The Core API owns workflow state, approvals and audit history. AI output is always a proposal. Repository mutations only occur after an explicit delivery-run approval.

## What is implemented

### Requirements and planning
- Grooming transcript → structured requirements
- User stories, acceptance criteria and open questions
- PO approval gate
- Acceptance-criteria-linked QA plans
- QA approval gate
- Development plans and developer approval

### Project brain
- Store architecture decisions, coding conventions, domain knowledge, previous fixes and review notes per project
- Search project memory
- Recall relevant memories automatically during repository analysis and code-change generation

### Repository intelligence
- Connect to a GitHub repository
- Detect common .NET, Node/TypeScript, Python, Playwright and container signals
- Rank repository files against the approved work
- Retrieve bounded source context for the developer agent
- Persist repository-analysis snapshots for auditability

### Agentic delivery
- Generate structured, reviewable full-file code-change proposals from approved plans
- Deterministic mode never proposes repository mutations
- OpenAI mode can propose create/update operations, validation commands and review risks
- Proposed files are first applied to an isolated runner
- Allowlisted build/test commands run before any GitHub write
- Failed validation output is fed back to the AI repair agent and revalidated
- Validation evidence, command logs, timings and repair attempts are persisted
- PR approval remains blocked until sandbox validation passes
- Human approval then creates an isolated GitHub branch
- Approved file changes are written through GitHub's contents API
- A pull request is opened with validation and review-risk context
- CI and human review remain required before merge

### QA / Playwright
- Generate Playwright proposal skeletons from approved QA plans
- Preserve traceability from acceptance criteria to generated E2E artifacts
- Existing CI executes the Playwright suite against the Docker Compose stack

## Run locally

```bash
cp .env.example .env
docker compose up --build
```

Open the web workspace at `http://localhost:3000`.

The browser now exposes the complete guided workflow: project selection, requirements and approvals, Project Brain, QA planning, Playwright proposal generation, development planning, repository intelligence, code-proposal review, and approved pull-request creation. Existing projects can be reopened and resume from their latest persisted state.

The deterministic AI provider is the default. It supports requirements analysis and contract testing without a paid API key, but intentionally returns **no repository mutations**.

> If you already ran an older schema locally, reset the local demo database once with `docker compose down -v` before starting this version. The project currently uses EF Core `EnsureCreated` for its portfolio/demo database.

## Use the browser workflow

1. Create or select a project.
2. Paste a grooming transcript and generate requirements.
3. Review and approve requirements.
4. Add durable architecture/domain/convention notes to **Project Brain**.
5. Generate and approve the QA plan; optionally inspect the Playwright proposal.
6. Generate and approve the implementation plan.
7. Enter a GitHub repository in `owner/repository` form and run repository analysis.
8. Generate the code proposal. SDLC AI automatically clones the target branch inside the sandbox, applies the proposal, and runs the allowlisted validation commands.
9. If validation fails, the exact command output is sent to the repair agent. The repaired proposal is applied to a fresh clone and validation is rerun, up to the configured repair limit.
10. Review the final files, risks, command logs, exit codes, timings and repair history. No GitHub write has happened yet.
11. Click **Approve validated changes & create pull request** only after sandbox validation has passed and the proposal is acceptable.

For a safe first run, leave `AI_PROVIDER=deterministic`. The final code proposal will contain zero changes by design.

## Enable production AI

Set:

```bash
AI_PROVIDER=openai
AI_MODEL=<supported-model>
OPENAI_API_KEY=<secret>
```

AI responses are contract-validated before crossing the AI-service boundary.

## Enable GitHub repository delivery

Set a fine-grained token scoped only to repositories used for the demo:

```bash
GITHUB_TOKEN=<fine-grained-token>
```

Read-only repository intelligence can work against public repositories without a token. Branch creation, file updates and PR creation require the configured token.

## Core API additions

```text
POST /api/projects/{projectId}/memory
GET  /api/projects/{projectId}/memory?q=...
POST /api/projects/{projectId}/repositories/analyze

POST /api/projects/{projectId}/qa/test-plans/{testPlanId}/e2e-proposal

POST /api/projects/{projectId}/development/delivery-runs
GET  /api/projects/{projectId}/development/delivery-runs/{runId}
POST /api/projects/{projectId}/development/delivery-runs/{runId}/validate
POST /api/projects/{projectId}/development/delivery-runs/{runId}/approve
```

## Quality

CI builds the .NET API and Next.js app, runs Python tests, boots the full Docker stack, and executes Playwright E2E smoke coverage.

## Safety / delivery invariants

1. AI output is a proposal, never an approval.
2. Requirements, QA plans and implementation plans preserve human approval gates.
3. Deterministic mode cannot mutate repositories.
4. Code-change proposals are persisted before any repository write.
5. Proposed code must pass isolated sandbox validation before PR approval is enabled.
6. The sandbox has no Docker socket, drops Linux capabilities, uses a read-only container filesystem, and executes commands without a shell.
7. Validation commands are allowlisted and path traversal / command chaining are rejected.
8. Repository writes occur on an isolated branch after explicit approval.
9. Pull requests still require normal CI and human review.
10. Secrets are configuration only and must never be committed.

## Design documentation

See `docs/architecture`, `docs/adr`, `docs/SECURITY.md`, `docs/ENTERPRISE.md`, `docs/AI-PROVIDERS.md`, `docs/DEMO.md`, and `docs/ROADMAP.md`.
