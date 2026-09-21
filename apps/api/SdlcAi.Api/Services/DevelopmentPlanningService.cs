using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Data.Entities;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class DevelopmentPlanningService(SdlcAiDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ImplementationPlan?> GenerateAsync(
        Guid projectId, Guid analysisId, Guid testPlanId, CancellationToken ct)
    {
        var analysis = await db.Analyses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == analysisId && x.ProjectId == projectId, ct);
        var tests = await db.TestPlans.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == testPlanId && x.ProjectId == projectId, ct);

        if (analysis is null || tests is null ||
            analysis.Status != AnalysisStatus.Approved.ToString() ||
            tests.Status != "Approved")
            return null;

        var requirements = JsonSerializer.Deserialize<RequirementAnalysis>(analysis.ResultJson, Json)!;
        var tasks = requirements.UserStories.Select((story, i) => new ImplementationTask(
            $"DEV-{i + 1:D2}",
            story.Title,
            story.Description,
            new[] { "To be resolved by repository-context analysis" },
            story.AcceptanceCriteria)).ToList();

        var entity = new ImplementationPlanEntity {
            Id = Guid.NewGuid(), ProjectId = projectId, AnalysisId = analysisId, TestPlanId = testPlanId,
            Summary = $"Implementation plan for {requirements.UserStories.Count} approved user stories.",
            TasksJson = JsonSerializer.Serialize(tasks, Json), Status = "PendingReview",
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ImplementationPlans.Add(entity);
        db.AuditEvents.Add(Audit(projectId, entity.Id, "dev.implementation-plan.generated", "dev-agent"));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ImplementationPlan?> ApproveAsync(Guid projectId, Guid planId, CancellationToken ct)
    {
        var entity = await db.ImplementationPlans
            .SingleOrDefaultAsync(x => x.Id == planId && x.ProjectId == projectId, ct);
        if (entity is null) return null;

        entity.Status = "Approved";
        entity.ApprovedAt = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(Audit(projectId, planId, "dev.implementation-plan.approved", "local-user"));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static AuditEventEntity Audit(Guid projectId, Guid resourceId, string action, string actor) => new() {
        Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = resourceId, Action = action,
        Actor = actor, MetadataJson = "{}", CreatedAt = DateTimeOffset.UtcNow
    };

    private static ImplementationPlan Map(ImplementationPlanEntity x) => new(
        x.Id, x.ProjectId, x.AnalysisId, x.TestPlanId, x.Status, x.Summary,
        JsonSerializer.Deserialize<List<ImplementationTask>>(x.TasksJson, Json)!,
        x.CreatedAt, x.ApprovedAt);
}
