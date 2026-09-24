import json
from fastapi import FastAPI, HTTPException

from .analysis_service import analyze, propose_code_changes
from .main_models import (
    AnalyzeRequest,
    CodeChangeProposal,
    CodeChangeRequest,
    RequirementAnalysis,
)
from .provider import get_provider

app = FastAPI(title="SDLC AI Service", version="0.3.0")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "sdlc-ai-service"}


@app.post("/v1/analyze-requirements", response_model=RequirementAnalysis)
def analyze_requirements(request: AnalyzeRequest) -> RequirementAnalysis:
    try:
        return analyze(request.transcript, get_provider())
    except (ValueError, RuntimeError, json.JSONDecodeError) as exc:
        raise HTTPException(status_code=502, detail=f"AI provider response failed validation: {exc}") from exc


@app.post("/v1/propose-code-changes", response_model=CodeChangeProposal)
def code_change_proposal(request: CodeChangeRequest) -> CodeChangeProposal:
    try:
        return propose_code_changes(request, get_provider())
    except (ValueError, RuntimeError, json.JSONDecodeError) as exc:
        raise HTTPException(status_code=502, detail=f"AI provider response failed validation: {exc}") from exc
