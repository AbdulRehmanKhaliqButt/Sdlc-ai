import json

from .main_models import RequirementAnalysis, UserStory
from .prompts import REQUIREMENTS_SYSTEM_PROMPT
from .provider import ModelProvider


def deterministic_analysis(transcript: str) -> RequirementAnalysis:
    preview = transcript.strip().replace("\n", " ")[:240]
    return RequirementAnalysis(
        summary=f"Initial analysis of grooming discussion: {preview}",
        userStories=[UserStory(
            title="Review extracted requirements",
            description="As a product owner, I want requirements extracted from a grooming transcript so that I can review engineering work before it enters delivery.",
            acceptanceCriteria=[
                "The original transcript remains available for review.",
                "Generated requirements are presented as structured data.",
                "No downstream delivery action occurs before human approval.",
            ],
        )],
        openQuestions=[
            "Which statements in the transcript are confirmed decisions versus proposals?",
            "Are there external dependencies or API contracts that must be confirmed?",
        ],
    )


def analyze(transcript: str, provider: ModelProvider) -> RequirementAnalysis:
    if provider.__class__.__name__ == "DeterministicProvider":
        return deterministic_analysis(transcript)

    prompt = f"""Analyze this grooming transcript and return JSON with exactly:
summary: string
userStories: array of objects with title, description, acceptanceCriteria
openQuestions: array of strings

Transcript:
{transcript}"""
    result = provider.generate(REQUIREMENTS_SYSTEM_PROMPT, prompt)
    payload = json.loads(result.content)
    return RequirementAnalysis.model_validate(payload)
