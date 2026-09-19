# SDLC AI

An AI-assisted software delivery platform that turns grooming and requirements discussions into structured, reviewable engineering work.

## Vision

SDLC AI is designed around human approval gates rather than autonomous changes. The first vertical slice is:

1. Create a project
2. Submit a grooming transcript
3. Analyze requirements
4. Generate user stories, acceptance criteria, and open questions
5. Review and approve the analysis

Later phases will add Jira/GitHub integrations, QA agents, implementation agents, Playwright E2E testing, evaluations, and delivery analytics.

## Architecture

- **Web:** Next.js + TypeScript
- **Core API:** ASP.NET Core
- **AI service:** Python + FastAPI
- **Data:** PostgreSQL / pgvector
- **Infrastructure:** Docker, later Terraform
- **Observability:** OpenTelemetry

See `docs/architecture/README.md` for the evolving architecture.

## Status

Phase 1 foundation is under active development.
