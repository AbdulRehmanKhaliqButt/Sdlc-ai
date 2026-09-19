namespace SdlcAi.Api.Models;

public sealed record CreateProjectRequest(string Name, string? Description);
public sealed record AnalyzeTranscriptRequest(string Transcript);

public sealed record RequirementAnalysis(
    string Summary,
    IReadOnlyList<UserStory> UserStories,
    IReadOnlyList<string> OpenQuestions);

public sealed record UserStory(
    string Title,
    string Description,
    IReadOnlyList<string> AcceptanceCriteria);

public sealed record Project(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt);

public sealed record Analysis(
    Guid Id,
    Guid ProjectId,
    string Transcript,
    RequirementAnalysis Result,
    AnalysisStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);

public enum AnalysisStatus
{
    PendingReview,
    Approved
}
