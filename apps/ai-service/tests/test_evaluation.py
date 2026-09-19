from app.evaluation import evaluate_analysis


def test_complete_analysis_scores_one():
    result = evaluate_analysis({
        "summary": "Summary",
        "userStories": [{"title": "Story"}],
        "openQuestions": [],
    })
    assert result.score == 1.0
    assert all(result.checks.values())


def test_missing_story_is_detected():
    result = evaluate_analysis({
        "summary": "Summary",
        "userStories": [],
        "openQuestions": [],
    })
    assert result.score < 1.0
    assert result.checks["has_stories"] is False
