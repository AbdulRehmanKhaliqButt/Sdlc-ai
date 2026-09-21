from fastapi import FastAPI, HTTPException

from .analysis_service import analyze
from .main_models import AnalyzeRequest, RequirementAnalysis
from .provider import get_provider

app = FastAPI(title="SDLC AI Service", version="0.2.0")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "sdlc-ai-service"}


@app.post("/v1/analyze-requirements", response_model=RequirementAnalysis)
def analyze_requirements(request: AnalyzeRequest) -> RequirementAnalysis:
    try:
        return analyze(request.transcript, get_provider())
    except (ValueError, RuntimeError) as exc:
        raise HTTPException(status_code=502, detail=f"AI provider response failed validation: {exc}") from exc
