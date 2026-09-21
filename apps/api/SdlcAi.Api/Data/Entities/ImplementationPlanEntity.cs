namespace SdlcAi.Api.Data.Entities;

public sealed class ImplementationPlanEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid AnalysisId { get; set; }
    public Guid TestPlanId { get; set; }
    public required string Summary { get; set; }
    public required string TasksJson { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}
