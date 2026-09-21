# SDLC AI

An enterprise-oriented, human-in-the-loop AI software delivery platform.

## Workflow

Grooming transcript → structured requirements → Product Owner approval → QA test plan → QA approval → development plan → developer approval → Jira/GitHub delivery adapters → pull request/CI → Playwright evidence.

## Architecture

- **Web:** Next.js + TypeScript
- **Core workflow API:** ASP.NET Core
- **AI boundary:** FastAPI + Pydantic, deterministic or OpenAI provider
- **System of record:** PostgreSQL (Supabase-compatible)
- **E2E:** Playwright
- **Runtime:** Docker Compose
- **CI/CD:** GitHub Actions

The Core API owns workflow state, approvals and audit history. AI output is always a proposal. External integrations are adapter ports and fail closed when they are not configured.

## Run locally

```bash
cp .env.example .env
docker compose up --build
```

Open the web workspace at `http://localhost:3000`. The deterministic AI provider is the default, so the core demo needs no paid API key.

## Production AI

Set `AI_PROVIDER=openai`, `AI_MODEL`, and `OPENAI_API_KEY`. AI responses are contract-validated before crossing the AI-service boundary.

## Quality

CI builds the .NET API and Next.js app, runs Python tests, boots the full Docker stack, and executes Playwright E2E smoke coverage.

## Design documentation

See `docs/architecture`, `docs/adr`, `docs/SECURITY.md`, `docs/ENTERPRISE.md`, `docs/AI-PROVIDERS.md`, and `docs/DEMO.md`.
