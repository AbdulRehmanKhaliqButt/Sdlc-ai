from fastapi import FastAPI
from pydantic import BaseModel, Field

app = FastAPI(title="SDLC AI Service", version="0.1.0")


class AnalyzeRequest(BaseModel):
    transcript: str = Field(min_length=1)


class UserStory(BaseModel):
    title: str
    description: str
    acceptanceCriteria: list[str]


class RequirementAnalysis(BaseModel):
    summary: str
    userStories: list[UserStory]
    openQuestions: list[str]


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "sdlc-ai-service"}


@app.post("/v1/analyze-requirements", response_model=RequirementAnalysis)
def analyze_requirements(request: AnalyzeRequest) -> RequirementAnalysis:
    # Phase 1 starts with a deterministic adapter so the end-to-end contract
    # can be developed and tested before binding the system to one LLM vendor.
    preview = request.transcript.strip().replace("\n", " ")[:240]

    return RequirementAnalysis(
        summary=f"Initial analysis of grooming discussion: {preview}",
        userStories=[
            UserStory(
                title="Review extracted requirements",
                description=(
                    "As a product owner, I want requirements extracted from a "
                    "grooming transcript so that I can review engineering work "
                    "before it enters delivery."
                ),
                acceptanceCriteria=[
                    "The original transcript remains available for review.",
                    "Generated requirements are presented as structured data.",
                    "No downstream delivery action occurs before human approval.",
                ],
            )
        ],
        openQuestions=[
            "Which statements in the transcript are confirmed decisions versus proposals?",
            "Are there external dependencies or API contracts that must be confirmed?",
        ],
    )
