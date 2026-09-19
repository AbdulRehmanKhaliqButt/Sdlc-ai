from dataclasses import dataclass


@dataclass(frozen=True)
class EvaluationResult:
    score: float
    checks: dict[str, bool]


def evaluate_analysis(payload: dict) -> EvaluationResult:
    checks = {
        "has_summary": bool(payload.get("summary")),
        "has_stories": bool(payload.get("userStories")),
        "has_questions": isinstance(payload.get("openQuestions"), list),
    }
    return EvaluationResult(
        score=sum(checks.values()) / len(checks),
        checks=checks,
    )
