using System.Text.Json;
using SdlcAi.Api.Data;
using SdlcAi.Api.Data.Entities;
using SdlcAi.Api.Integrations;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class RepositoryIntelligenceService(
    SdlcAiDbContext db,
    IGitHubDeliveryAdapter github,
    ProjectMemoryService memory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<RepositoryIntelligence> AnalyzeAsync(
        Guid projectId, RepositoryAnalysisRequest request, CancellationToken ct)
    {
        var remembered = await memory.RecallAsync(projectId, new[] { request.Query }, ct);
        var memoryContext = remembered.Count == 0
            ? string.Empty
            : "\nProject memory:\n" + string.Join("\n", remembered.Select(x => $"- [{x.Kind}] {x.Content}"));

        var context = await github.GetContextAsync(request.Repository, request.Query + memoryContext, ct);
        var id = Guid.NewGuid();
        var result = new RepositoryIntelligence(
            id, projectId, request.Repository, context.DefaultBranch,
            context.RelevantFiles, context.Signals, DateTimeOffset.UtcNow);

        db.RepositoryAnalyses.Add(new RepositoryAnalysisEntity
        {
            Id = id,
            ProjectId = projectId,
            Repository = request.Repository,
            DefaultBranch = context.DefaultBranch,
            ContextJson = JsonSerializer.Serialize(result, Json),
            AnalyzedAt = result.AnalyzedAt
        });
        db.AuditEvents.Add(new AuditEventEntity
        {
            Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = id,
            Action = "repository.context.analyzed", Actor = "repository-intelligence",
            MetadataJson = JsonSerializer.Serialize(new { request.Repository, fileCount = context.RelevantFiles.Count }, Json),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return result;
    }
}
