# Product roadmap

## Implemented foundation
- Project creation
- Transcript-to-structured-requirements contract
- User stories and acceptance criteria
- Open-question extraction contract
- Human approval gate
- Web review workspace
- .NET orchestration API
- Python AI boundary
- Containerized local stack

## Phase 2 — persistence and production AI
- PostgreSQL + migrations
- Provider-neutral LLM interface
- Structured-output validation
- Prompt/version registry
- Analysis history and editing
- Authentication and workspace isolation

## Phase 3 — planning integrations
- Jira adapter
- GitHub adapter
- Ticket proposal and approval
- Architecture/dependency analysis
- Audit trail

## Phase 4 — QA agent
- Test-plan generation
- Traceability from acceptance criteria to tests
- QA review gate
- Playwright test proposal/export

## Phase 5 — development agent
- Repository context retrieval
- Implementation plan
- Sandboxed code changes
- Unit-test generation
- PR creation
- Reviewer-feedback loop

## Phase 6 — E2E and release
- Playwright execution
- CI quality gates
- Deployment adapters
- Release evidence

## Phase 7 — evaluations and observability
- Golden datasets
- Requirement-extraction scoring
- Tool-call success metrics
- Latency/token/cost tracking
- OpenTelemetry traces
- Regression gates

## Phase 8 — enterprise
- RBAC
- Multi-tenancy
- Secrets management
- MCP/connectors
- Data-retention policy
- Admin/audit views

Each phase preserves explicit human approval for consequential actions.
