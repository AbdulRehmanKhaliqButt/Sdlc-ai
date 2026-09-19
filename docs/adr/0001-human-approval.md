# ADR 0001: Human approval is a domain boundary

Status: Accepted

## Context
AI can accelerate requirement extraction and software delivery, but generated output may be incomplete or wrong.

## Decision
Generation and approval are separate operations. AI services return proposals. Only authenticated application actors can approve proposals through the Core API.

## Consequences
- Every consequential downstream action can identify the human-approved source.
- AI providers can be replaced without changing workflow semantics.
- Fully autonomous delivery is intentionally not the default.
