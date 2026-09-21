import pytest

from app.provider import DeterministicProvider, get_provider
from app.analysis_service import analyze


def test_deterministic_provider_keeps_ci_secret_free():
    result = DeterministicProvider().generate("system", "hello")
    assert result.provider == "deterministic"
    assert result.content == "hello"


def test_analysis_is_structurally_valid_with_deterministic_provider():
    result = analyze("We need a reviewed ticket before implementation.", DeterministicProvider())
    assert result.summary
    assert result.userStories
    assert result.userStories[0].acceptanceCriteria
