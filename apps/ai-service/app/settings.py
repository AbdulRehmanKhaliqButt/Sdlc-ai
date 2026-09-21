import os
from dataclasses import dataclass


@dataclass(frozen=True)
class Settings:
    provider: str = os.getenv("AI_PROVIDER", "deterministic").lower()
    model: str = os.getenv("AI_MODEL", "gpt-5-mini")
    openai_api_key: str | None = os.getenv("OPENAI_API_KEY")
    timeout_seconds: float = float(os.getenv("AI_TIMEOUT_SECONDS", "45"))
    max_retries: int = int(os.getenv("AI_MAX_RETRIES", "2"))


settings = Settings()
