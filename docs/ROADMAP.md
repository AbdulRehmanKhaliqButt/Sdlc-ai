# Product roadmap

## Implemented foundation
- Project creation
- Transcript-to-structured-requirements contract
- User stories and acceptance criteria
- Open-question extraction
- Human approval gates
- Web review workspace
- .NET orchestration API
- Python AI boundary
- PostgreSQL persistence and audit history
- Provider-neutral deterministic/OpenAI execution
- Containerized local stack
- Full-stack CI + Playwright smoke gate

## Implemented — QA and development planning
- Acceptance-criteria-linked QA plan generation
- QA approval gate
- Development plan generation
- Developer approval gate
- Delivery adapter boundaries

## Implemented — Project Brain
- Per-project persistent memory
- Searchable memory API
- Memory kinds/tags for architecture decisions, conventions, domain notes and review feedback
- Automatic recall during repository analysis and code-change generation

## Implemented — Repository Intelligence
- Real GitHub REST repository adapter
- Default-branch discovery
- Recursive repository tree inspection
- Relevant-file scoring and bounded content retrieval
- Technology signals for .NET, Node/TypeScript, Python, Playwright and containers
- Persisted repository-analysis snapshots

## Implemented — Agentic Delivery
- Structured code-change proposal contract
- Full-file create/update proposals
- Validation-command and risk output
- Deterministic provider safety mode with zero mutations
- Explicit human approval before repository writes
- Isolated branch creation
- GitHub file updates
- Pull-request creation
- Delivery audit events

## Implemented — E2E proposal
- Generate Playwright test skeletons from approved QA plans
- Acceptance-criteria traceability
- Existing CI executes repository Playwright tests

## Next — make repository intelligence deeper
- Symbol-aware code indexing rather than path/token scoring
- Dependency graph and call-graph extraction
- Roslyn-based .NET semantic analysis
- TypeScript AST analysis
- Incremental repository embeddings / pgvector retrieval
- Context-budget optimization and deduplication

## Next — execution sandbox
- Clone approved branch into an isolated worker
- Apply proposal in sandbox before GitHub writes
- Run build, unit tests, linters and static analysis
- Feed failures back to a repair loop
- Persist command output, diffs and artifacts
- Block PR creation when mandatory quality gates fail

## Next — richer QA automation
- AI-generated product-specific Playwright selectors/actions from repository context
- Browser execution before PR
- Screenshot/video/trace retention
- Flaky-test detection
- Coverage mapping from acceptance criteria → unit/integration/E2E evidence

## Next — integrations and enterprise
- Real Jira ticket create/update adapter
- GitHub App installation flow instead of raw token configuration
- RBAC and multi-tenancy
- Secrets vault integration
- Webhooks for PR review feedback and CI completion
- MCP/connectors
- Data-retention controls
- Admin/audit views

## Next — evaluations and observability
- Golden datasets
- Requirement-extraction scoring
- Code-change acceptance rate
- Tool-call success metrics
- Latency/token/cost tracking
- OpenTelemetry traces
- Regression gates

Every phase preserves explicit human approval for consequential actions.
