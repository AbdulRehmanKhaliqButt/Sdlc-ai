using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Data.Entities;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class PersistentProjectStore(SdlcAiDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Project> CreateAsync(string name, string? description, CancellationToken ct)
    {
        var entity = new ProjectEntity { Id = Guid.NewGuid(), Name = name, Description = description, CreatedAt = DateTimeOffset.UtcNow };
        db.Projects.Add(entity);
        db.AuditEvents.Add(Audit(entity.Id, entity.Id, "project.created"));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<List<Project>> GetAllAsync(CancellationToken ct) =>
        (await db.Projects.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(Map).ToList();

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => db.Projects.AnyAsync(x => x.Id == id, ct);

    public async Task<Analysis> AddAnalysisAsync(Guid projectId, string transcript, RequirementAnalysis result, CancellationToken ct)
    {
        var entity = new AnalysisEntity {
            Id = Guid.NewGuid(), ProjectId = projectId, Transcript = transcript,
            ResultJson = JsonSerializer.Serialize(result, Json),
            Status = AnalysisStatus.PendingReview.ToString(), CreatedAt = DateTimeOffset.UtcNow
        };
        db.Analyses.Add(entity);
        db.AuditEvents.Add(Audit(projectId, entity.Id, "analysis.generated"));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<Analysis?> ApproveAsync(Guid projectId, Guid analysisId, CancellationToken ct)
    {
        var entity = await db.Analyses.SingleOrDefaultAsync(x => x.Id == analysisId && x.ProjectId == projectId, ct);
        if (entity is null) return null;
        entity.Status = AnalysisStatus.Approved.ToString();
        entity.ApprovedAt = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(Audit(projectId, analysisId, "analysis.approved"));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<List<AuditEventEntity>> AuditAsync(Guid projectId, CancellationToken ct) =>
        await db.AuditEvents.AsNoTracking().Where(x => x.ProjectId == projectId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    private static AuditEventEntity Audit(Guid projectId, Guid resourceId, string action) => new() {
        Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = resourceId, Action = action,
        Actor = "local-user", MetadataJson = "{}", CreatedAt = DateTimeOffset.UtcNow
    };
    private static Project Map(ProjectEntity x) => new(x.Id, x.Name, x.Description, x.CreatedAt);
    private static Analysis Map(AnalysisEntity x) => new(x.Id, x.ProjectId, x.Transcript,
        JsonSerializer.Deserialize<RequirementAnalysis>(x.ResultJson, Json)!,
        Enum.Parse<AnalysisStatus>(x.Status), x.CreatedAt, x.ApprovedAt);
}
