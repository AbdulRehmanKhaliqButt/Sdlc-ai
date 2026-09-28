using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Integrations;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class WorkspaceService(SdlcAiDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<WorkspaceSnapshot?> GetAsync(Guid projectId, CancellationToken ct)
    {
        var projectEntity = await db.Projects.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == projectId, ct);
        if (projectEntity is null) return null;

        var analysisEntity = await db.Analyses.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var testPlanEntity = await db.TestPlans.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var implementationEntity = await db.ImplementationPlans.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var repositoryEntity = await db.RepositoryAnalyses.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.AnalyzedAt)
            .FirstOrDefaultAsync(ct);

        var deliveryEntity = await db.DeliveryRuns.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var memories = await db.ProjectMemories.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        var project = new Project(projectEntity.Id, projectEntity.Name, projectEntity.Description, projectEntity.CreatedAt);

        Analysis? analysis = null;
        if (analysisEntity is not null)
        {
            analysis = new Analysis(
                analysisEntity.Id,
                analysisEntity.ProjectId,
                analysisEntity.Transcript,
                JsonSerializer.Deserialize<RequirementAnalysis>(analysisEntity.ResultJson, Json)!,
                Enum.Parse<AnalysisStatus>(analysisEntity.Status),
                analysisEntity.CreatedAt,
                analysisEntity.ApprovedAt);
        }

        TestPlan? testPlan = null;
        if (testPlanEntity is not null)
        {
            testPlan = new TestPlan(
                testPlanEntity.Id,
                testPlanEntity.ProjectId,
                testPlanEntity.AnalysisId,
                testPlanEntity.Status,
                JsonSerializer.Deserialize<List<TestCase>>(testPlanEntity.TestCasesJson, Json) ?? new(),
                testPlanEntity.CreatedAt,
                testPlanEntity.ApprovedAt);
        }

        ImplementationPlan? implementation = null;
        if (implementationEntity is not null)
        {
            implementation = new ImplementationPlan(
                implementationEntity.Id,
                implementationEntity.ProjectId,
                implementationEntity.AnalysisId,
                implementationEntity.TestPlanId,
                implementationEntity.Status,
                implementationEntity.Summary,
                JsonSerializer.Deserialize<List<ImplementationTask>>(implementationEntity.TasksJson, Json) ?? new(),
                implementationEntity.CreatedAt,
                implementationEntity.ApprovedAt);
        }

        RepositoryIntelligence? repository = null;
        if (repositoryEntity is not null)
            repository = JsonSerializer.Deserialize<RepositoryIntelligence>(repositoryEntity.ContextJson, Json);

        DeliveryRun? delivery = null;
        if (deliveryEntity is not null)
        {
            delivery = new DeliveryRun(
                deliveryEntity.Id,
                deliveryEntity.ProjectId,
                deliveryEntity.ImplementationPlanId,
                deliveryEntity.Repository,
                deliveryEntity.DefaultBranch,
                deliveryEntity.BranchName,
                deliveryEntity.Status,
                JsonSerializer.Deserialize<CodeChangeProposal>(deliveryEntity.ProposalJson, Json)!,
                string.IsNullOrWhiteSpace(deliveryEntity.ValidationJson)
                    ? null
                    : JsonSerializer.Deserialize<ValidationEvidence>(deliveryEntity.ValidationJson, Json),
                deliveryEntity.PullRequestNumber,
                deliveryEntity.PullRequestUrl,
                deliveryEntity.CreatedAt,
                deliveryEntity.ApprovedAt);
        }

        var memory = memories.Select(x => new ProjectMemory(
            x.Id,
            x.ProjectId,
            x.Kind,
            x.Content,
            JsonSerializer.Deserialize<List<string>>(x.TagsJson, Json) ?? new(),
            x.CreatedAt)).ToList();

        return new WorkspaceSnapshot(project, analysis, testPlan, implementation, repository, delivery, memory);
    }
}
