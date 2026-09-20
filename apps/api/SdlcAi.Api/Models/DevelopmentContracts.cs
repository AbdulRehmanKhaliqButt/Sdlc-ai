namespace SdlcAi.Api.Models;

public sealed record GenerateImplementationPlanRequest(Guid AnalysisId, Guid TestPlanId);

public sealed record ImplementationTask(
    string Id,
    string Title,
    string Description,
    IReadOnlyList<string> FilesLikelyAffected,
    IReadOnlyList<string> Validation);

public sealed record ImplementationPlan(
    Guid Id,
    Guid ProjectId,
    Guid AnalysisId,
    Guid TestPlanId,
    string Status,
    string Summary,
    IReadOnlyList<ImplementationTask> Tasks,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);
