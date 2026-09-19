namespace SdlcAi.Api.Data.Entities;

public sealed class ProjectEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AnalysisEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string Transcript { get; set; }
    public required string ResultJson { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}

public sealed class AuditEventEntity
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ResourceId { get; set; }
    public required string Action { get; set; }
    public required string Actor { get; set; }
    public required string MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
