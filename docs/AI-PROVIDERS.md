# AI providers

The AI service is provider-neutral and remains runnable without paid credentials.

## Local/CI mode
`AI_PROVIDER=deterministic` is the default. It is deterministic and secret-free, making integration tests stable.

## OpenAI mode
Set:
- `AI_PROVIDER=openai`
- `AI_MODEL=<supported model>`
- `OPENAI_API_KEY=<secret>`

Provider responses are parsed and validated through Pydantic before they cross the AI-service boundary. Invalid JSON or invalid contracts fail rather than being persisted as trusted workflow data.

Additional providers can implement `ModelProvider` without changing the Core API.
