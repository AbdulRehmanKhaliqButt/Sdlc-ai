using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Data.Entities;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class ProjectMemoryService(SdlcAiDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ProjectMemory> AddAsync(Guid projectId, AddProjectMemoryRequest request, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct))
            throw new InvalidOperationException("Project not found.");

        var entity = new ProjectMemoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "note" : request.Kind.Trim(),
            Content = request.Content.Trim(),
            TagsJson = JsonSerializer.Serialize(request.Tags ?? Array.Empty<string>(), Json),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProjectMemories.Add(entity);
        db.AuditEvents.Add(Audit(projectId, entity.Id, "memory.added", "local-user"));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<List<ProjectMemory>> SearchAsync(Guid projectId, string? query, CancellationToken ct)
    {
        var source = db.ProjectMemories.AsNoTracking().Where(x => x.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            source = source.Where(x => EF.Functions.ILike(x.Content, $"%{q}%")
                                      || EF.Functions.ILike(x.Kind, $"%{q}%")
                                      || EF.Functions.ILike(x.TagsJson, $"%{q}%"));
        }

        var rows = await source.OrderByDescending(x => x.CreatedAt).Take(25).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ProjectMemory>> RecallAsync(Guid projectId, IEnumerable<string> hints, CancellationToken ct)
    {
        var terms = hints.SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        var all = await db.ProjectMemories.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return all.Select(x => new {
                Entity = x,
                Score = terms.Count(t => x.Content.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || x.Kind.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || x.TagsJson.Contains(t, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Entity.CreatedAt)
            .Take(12)
            .Select(x => Map(x.Entity))
            .ToList();
    }

    private static ProjectMemory Map(ProjectMemoryEntity x) => new(
        x.Id, x.ProjectId, x.Kind, x.Content,
        JsonSerializer.Deserialize<List<string>>(x.TagsJson, Json) ?? new List<string>(),
        x.CreatedAt);

    private static AuditEventEntity Audit(Guid projectId, Guid resourceId, string action, string actor) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = resourceId,
        Action = action, Actor = actor, MetadataJson = "{}", CreatedAt = DateTimeOffset.UtcNow
    };
}
