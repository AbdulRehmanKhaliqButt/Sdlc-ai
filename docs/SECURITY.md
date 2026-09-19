# Security baseline

- Never commit model, Jira, GitHub, database, or deployment credentials.
- AI services cannot approve their own output.
- Treat transcripts and repository content as untrusted input.
- Validate structured AI output before persistence or tool execution.
- Use least-privilege credentials for every integration.
- Record actor, action, timestamp, source proposal, and external target for consequential actions.
- Keep tenant/workspace boundaries in all persistence queries.
- Require explicit confirmation before destructive or externally visible actions.
- Redact secrets and sensitive values from prompts, traces, and logs.
