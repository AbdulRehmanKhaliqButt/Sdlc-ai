from abc import ABC, abstractmethod
from dataclasses import dataclass


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
    """Development provider used until credentials are configured."""

    def generate(self, system_prompt: str, user_prompt: str) -> ProviderResult:
        return ProviderResult(
            content=user_prompt,
            provider="deterministic",
            model="contract-fixture-v1",
        )
