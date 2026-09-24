# SDLC AI

An enterprise-oriented, human-in-the-loop AI software delivery platform.

## Workflow

Grooming transcript → structured requirements → Product Owner approval → QA test plan → QA approval → development plan → developer approval → repository intelligence → AI code-change proposal → human approval → GitHub branch/PR → CI → Playwright evidence.

## Architecture

- **Web:** Next.js + TypeScript
- **Core workflow API:** ASP.NET Core
- **AI boundary:** FastAPI + Pydantic, deterministic or OpenAI provider
- **System of record:** PostgreSQL (Supabase-compatible)
- **Repository delivery:** GitHub REST adapter with explicit write approval
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
- Human approval creates an isolated GitHub branch
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

The deterministic AI provider is the default. It supports requirements analysis and contract testing without a paid API key, but intentionally returns **no repository mutations**.

> If you already ran an older schema locally, reset the local demo database once with `docker compose down -v` before starting this version. The project currently uses EF Core `EnsureCreated` for its portfolio/demo database.

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
POST /api/projects/{projectId}/development/delivery-runs/{runId}/approve
```

## Quality

CI builds the .NET API and Next.js app, runs Python tests, boots the full Docker stack, and executes Playwright E2E smoke coverage.

## Safety / delivery invariants

1. AI output is a proposal, never an approval.
2. Requirements, QA plans and implementation plans preserve human approval gates.
3. Deterministic mode cannot mutate repositories.
4. Code-change proposals are persisted before any repository write.
5. Repository writes occur on an isolated branch after explicit approval.
6. Pull requests still require normal CI and human review.
7. Secrets are configuration only and must never be committed.

## Design documentation

See `docs/architecture`, `docs/adr`, `docs/SECURITY.md`, `docs/ENTERPRISE.md`, `docs/AI-PROVIDERS.md`, `docs/DEMO.md`, and `docs/ROADMAP.md`.
