using System.Collections.Concurrent;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class ProjectStore
{
    private readonly ConcurrentDictionary<Guid, Project> _projects = new();
    private readonly ConcurrentDictionary<Guid, Analysis> _analyses = new();

    public Project Create(string name, string? description)
    {
        var project = new Project(Guid.NewGuid(), name, description, DateTimeOffset.UtcNow);
        _projects[project.Id] = project;
        return project;
    }

    public IEnumerable<Project> GetAll() =>
        _projects.Values.OrderByDescending(x => x.CreatedAt);

    public bool Exists(Guid projectId) => _projects.ContainsKey(projectId);

    public Analysis AddAnalysis(Guid projectId, string transcript, RequirementAnalysis result)
    {
        var analysis = new Analysis(
            Guid.NewGuid(), projectId, transcript, result,
            AnalysisStatus.PendingReview, DateTimeOffset.UtcNow, null);

        _analyses[analysis.Id] = analysis;
        return analysis;
    }

    public Analysis? Approve(Guid projectId, Guid analysisId)
    {
        if (!_analyses.TryGetValue(analysisId, out var current) || current.ProjectId != projectId)
            return null;

        var approved = current with
        {
            Status = AnalysisStatus.Approved,
            ApprovedAt = DateTimeOffset.UtcNow
        };

        _analyses[analysisId] = approved;
        return approved;
    }
}
