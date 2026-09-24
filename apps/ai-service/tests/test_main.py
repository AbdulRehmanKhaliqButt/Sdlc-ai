from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)


def test_health():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] == "ok"


def test_analysis_requires_transcript():
    response = client.post("/v1/analyze-requirements", json={"transcript": ""})
    assert response.status_code == 422


def test_analysis_is_structured():
    response = client.post(
        "/v1/analyze-requirements",
        json={"transcript": "We need users to review requirements before Jira tickets are created."},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["userStories"]
    assert body["userStories"][0]["acceptanceCriteria"]
    assert body["openQuestions"]


def test_code_change_contract_is_safe_in_deterministic_mode():
    response = client.post(
        "/v1/propose-code-changes",
        json={
            "repository": "owner/repo",
            "tasks": [{
                "id": "DEV-01",
                "title": "Add endpoint",
                "description": "Add a reviewed endpoint.",
                "filesLikelyAffected": [],
                "validation": ["Run tests"],
            }],
            "files": [],
            "memory": [],
        },
    )
    assert response.status_code == 200
    body = response.json()
    assert body["changes"] == []
    assert body["risks"]
