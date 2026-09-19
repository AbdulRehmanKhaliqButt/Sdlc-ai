# Architecture

SDLC AI uses a modular, service-oriented architecture while keeping Phase 1 deliberately small.

## Components

### Web
Next.js/TypeScript application for project setup, transcript submission, analysis review, and approval.

### Core API
ASP.NET Core owns the business workflow and remains the system of record. AI providers do not directly mutate core domain state.

### AI Service
FastAPI service responsible for AI-oriented transformations. The first contract converts a grooming transcript into structured requirements.

## Phase 1 workflow

```text
User
  |
  v
Next.js Web
  |
  v
ASP.NET Core API
  |
  +--> Project / Analysis state
  |
  v
FastAPI AI Service
  |
  v
Structured Requirement Analysis
  |
  v
Human Review -> Approval
```

## Architectural principles

1. Human approval before downstream actions.
2. Structured AI outputs instead of unvalidated prose.
3. Core API owns workflow state and authorization.
4. AI providers remain replaceable behind service contracts.
5. Every AI run will eventually be traceable and evaluable.
6. Integrations are adapters, not domain dependencies.
