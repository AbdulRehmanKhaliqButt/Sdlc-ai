using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Data.Entities;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class QaPlanningService(SdlcAiDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<TestPlan?> GenerateAsync(Guid projectId, Guid analysisId, CancellationToken ct)
    {
        var analysis = await db.Analyses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == analysisId && x.ProjectId == projectId, ct);
        if (analysis is null || analysis.Status != AnalysisStatus.Approved.ToString()) return null;

        var requirements = JsonSerializer.Deserialize<RequirementAnalysis>(analysis.ResultJson, Json)!;
        var cases = requirements.UserStories.SelectMany((story, storyIndex) =>
            story.AcceptanceCriteria.Select((criterion, criterionIndex) => new TestCase(
                $"TC-{storyIndex + 1:D2}-{criterionIndex + 1:D2}",
                $"Verify {story.Title}",
                "Acceptance",
                "The approved requirement is available and the system is running.",
                new[] { $"Execute the workflow described by: {story.Description}", $"Validate acceptance criterion: {criterion}" },
                criterion,
                new[] { criterion }))).ToList();

        var entity = new TestPlanEntity {
            Id = Guid.NewGuid(), ProjectId = projectId, AnalysisId = analysisId,
            TestCasesJson = JsonSerializer.Serialize(cases, Json), Status = "PendingReview",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.TestPlans.Add(entity);
        db.AuditEvents.Add(new AuditEventEntity {
            Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = entity.Id,
            Action = "qa.test-plan.generated", Actor = "qa-agent", MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TestPlan?> ApproveAsync(Guid projectId, Guid testPlanId, CancellationToken ct)
    {
        var entity = await db.TestPlans.SingleOrDefaultAsync(x => x.Id == testPlanId && x.ProjectId == projectId, ct);
        if (entity is null) return null;
        entity.Status = "Approved"; entity.ApprovedAt = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(new AuditEventEntity {
            Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = testPlanId,
            Action = "qa.test-plan.approved", Actor = "local-user", MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static TestPlan Map(TestPlanEntity x) => new(x.Id, x.ProjectId, x.AnalysisId, x.Status,
        JsonSerializer.Deserialize<List<TestCase>>(x.TestCasesJson, Json)!, x.CreatedAt, x.ApprovedAt);
}
