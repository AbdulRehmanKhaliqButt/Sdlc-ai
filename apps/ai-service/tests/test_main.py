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
