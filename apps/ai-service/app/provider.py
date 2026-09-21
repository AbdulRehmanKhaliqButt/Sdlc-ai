from abc import ABC, abstractmethod
from dataclasses import dataclass
import json

from openai import OpenAI

from .settings import settings


@dataclass(frozen=True)
class ProviderResult:
    content: str
    provider: str
    model: str


class ModelProvider(ABC):
    @abstractmethod
    def generate(self, system_prompt: str, user_prompt: str) -> ProviderResult:
        raise NotImplementedError


class DeterministicProvider(ModelProvider):
    def generate(self, system_prompt: str, user_prompt: str) -> ProviderResult:
        return ProviderResult(content=user_prompt, provider="deterministic", model="contract-fixture-v1")


class OpenAIProvider(ModelProvider):
    def __init__(self) -> None:
        if not settings.openai_api_key:
            raise RuntimeError("OPENAI_API_KEY is required when AI_PROVIDER=openai")
        self.client = OpenAI(api_key=settings.openai_api_key, timeout=settings.timeout_seconds,
                             max_retries=settings.max_retries)

    def generate(self, system_prompt: str, user_prompt: str) -> ProviderResult:
        response = self.client.responses.create(
            model=settings.model,
            instructions=system_prompt,
            input=user_prompt,
            text={"format": {"type": "json_object"}},
        )
        return ProviderResult(content=response.output_text, provider="openai", model=settings.model)


def get_provider() -> ModelProvider:
    if settings.provider == "openai":
        return OpenAIProvider()
    if settings.provider == "deterministic":
        return DeterministicProvider()
    raise RuntimeError(f"Unsupported AI_PROVIDER: {settings.provider}")
