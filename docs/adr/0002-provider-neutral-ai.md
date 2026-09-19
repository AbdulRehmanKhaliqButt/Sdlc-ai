# ADR 0002: Keep AI provider-neutral

Status: Accepted

## Decision
The Core API depends on an analysis interface rather than an OpenAI, Anthropic, or local-model SDK. The Python AI boundary will own provider-specific adapters and normalize responses to versioned contracts.

## Why
This allows model comparison, local/private inference, fallback policies, and evaluation without rewriting the product workflow.
