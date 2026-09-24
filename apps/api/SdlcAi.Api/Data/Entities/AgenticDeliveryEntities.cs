namespace SdlcAi.Api.Data.Entities;

public sealed class ProjectMemoryEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Kind { get; set; }
    public required string Content { get; set; }
    public required string TagsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RepositoryAnalysisEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Repository { get; set; }
    public required string DefaultBranch { get; set; }
    public required string ContextJson { get; set; }
    public DateTimeOffset AnalyzedAt { get; set; }
}

public sealed class DeliveryRunEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ImplementationPlanId { get; set; }
    public required string Repository { get; set; }
    public required string DefaultBranch { get; set; }
    public string? BranchName { get; set; }
    public required string Status { get; set; }
    public required string ContextJson { get; set; }
    public required string ProposalJson { get; set; }
    public int? PullRequestNumber { get; set; }
    public string? PullRequestUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}
