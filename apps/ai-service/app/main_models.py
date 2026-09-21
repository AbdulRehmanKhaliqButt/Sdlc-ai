from pydantic import BaseModel, Field


class AnalyzeRequest(BaseModel):
    transcript: str = Field(min_length=1)


class UserStory(BaseModel):
    title: str = Field(min_length=1)
    description: str = Field(min_length=1)
    acceptanceCriteria: list[str] = Field(min_length=1)


class RequirementAnalysis(BaseModel):
    summary: str = Field(min_length=1)
    userStories: list[UserStory] = Field(min_length=1)
    openQuestions: list[str]
