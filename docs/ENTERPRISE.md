# Enterprise readiness

The platform uses explicit human approval boundaries for requirements, QA, and development planning. Viewer, Product Owner, QA, Developer, and Admin roles are modeled separately. Production identity providers should map authenticated identities to workspace-scoped roles.

Jira and GitHub are ports behind adapter interfaces so provider SDKs and future MCP-backed connectors remain outside the domain. Unconfigured integrations fail closed. Code-writing agents must operate on isolated branches; pull requests, CI, reviewer feedback, and E2E evidence remain delivery gates.

Workflow audit events are durable business evidence. Metrics expose workflow activity and AI latency. Secrets are injected at runtime and never committed.
