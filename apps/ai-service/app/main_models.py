from typing import Literal
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


class DevelopmentTask(BaseModel):
    id: str
    title: str
    description: str
    filesLikelyAffected: list[str]
    validation: list[str]


class RepositoryFile(BaseModel):
    path: str
    sha: str | None = None
    content: str


class ProjectMemory(BaseModel):
    id: str
    projectId: str
    kind: str
    content: str
    tags: list[str]
    createdAt: str


class CodeChangeRequest(BaseModel):
    repository: str
    tasks: list[DevelopmentTask] = Field(min_length=1)
    files: list[RepositoryFile]
    memory: list[ProjectMemory] = []


class FileChange(BaseModel):
    path: str = Field(min_length=1)
    action: Literal["create", "update"]
    content: str
    reason: str = Field(min_length=1)


class CodeChangeProposal(BaseModel):
    summary: str = Field(min_length=1)
    changes: list[FileChange]
    commands: list[str]
    risks: list[str]
