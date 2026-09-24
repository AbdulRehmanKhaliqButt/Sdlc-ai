import json

from .main_models import (
    CodeChangeProposal,
    CodeChangeRequest,
    RequirementAnalysis,
    UserStory,
)
from .prompts import CODE_CHANGE_SYSTEM_PROMPT, REQUIREMENTS_SYSTEM_PROMPT
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


def propose_code_changes(request: CodeChangeRequest, provider: ModelProvider) -> CodeChangeProposal:
    if provider.__class__.__name__ == "DeterministicProvider":
        return CodeChangeProposal(
            summary="Deterministic mode validated the delivery contract but does not generate repository mutations.",
            changes=[],
            commands=[],
            risks=["Set AI_PROVIDER=openai to generate reviewable code changes from repository context."],
        )

    task_text = "\n".join(
        f"- {task.id}: {task.title}\n  {task.description}\n  Validation: {'; '.join(task.validation)}"
        for task in request.tasks
    )
    memory_text = "\n".join(
        f"- [{item.kind}] {item.content}" for item in request.memory
    ) or "(none)"
    file_text = "\n\n".join(
        f"--- FILE: {file.path} ---\n{file.content}" for file in request.files
    ) or "(no repository files were selected)"

    prompt = f"""Repository: {request.repository}

APPROVED IMPLEMENTATION TASKS
{task_text}

RELEVANT PROJECT MEMORY
{memory_text}

REPOSITORY CONTEXT
{file_text}

Return the complete JSON code-change proposal defined by the system instructions."""
    result = provider.generate(CODE_CHANGE_SYSTEM_PROMPT, prompt)
    payload = json.loads(result.content)
    return CodeChangeProposal.model_validate(payload)
