namespace SdlcAi.Api.Data.Entities;

public sealed class TestPlanEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid AnalysisId { get; set; }
    public required string TestCasesJson { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}
