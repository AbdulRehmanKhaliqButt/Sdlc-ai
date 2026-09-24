using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Data.Entities;
using SdlcAi.Api.Integrations;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class AgenticDeliveryService(
    SdlcAiDbContext db,
    IGitHubDeliveryAdapter github,
    AiAnalysisClient ai,
    ProjectMemoryService memory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<DeliveryRun?> CreateAsync(
        Guid projectId, CreateDeliveryRunRequest request, CancellationToken ct)
    {
        var planEntity = await db.ImplementationPlans.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ImplementationPlanId && x.ProjectId == projectId, ct);
        if (planEntity is null || planEntity.Status != "Approved") return null;

        var plan = new ImplementationPlan(
            planEntity.Id, planEntity.ProjectId, planEntity.AnalysisId, planEntity.TestPlanId,
            planEntity.Status, planEntity.Summary,
            JsonSerializer.Deserialize<List<ImplementationTask>>(planEntity.TasksJson, Json) ?? new(),
            planEntity.CreatedAt, planEntity.ApprovedAt);

        var context = await github.GetContextAsync(
            request.Repository,
            string.Join("\n", plan.Tasks.Select(x => $"{x.Title} {x.Description} {string.Join(" ", x.Validation)}")),
            ct);

        var memories = await memory.RecallAsync(
            projectId,
            plan.Tasks.Select(x => x.Title).Append(plan.Summary),
            ct);

        var proposal = await ai.ProposeCodeChangesAsync(
            request.Repository,
            plan.Tasks,
            context.RelevantFiles,
            memories,
            ct);

        var status = proposal.Changes.Count == 0 ? "NoChanges" : "PendingApproval";
        var entity = new DeliveryRunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ImplementationPlanId = plan.Id,
            Repository = request.Repository,
            DefaultBranch = context.DefaultBranch,
            BranchName = request.BranchName,
            Status = status,
            ContextJson = JsonSerializer.Serialize(context, Json),
            ProposalJson = JsonSerializer.Serialize(proposal, Json),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.DeliveryRuns.Add(entity);
        db.AuditEvents.Add(Audit(projectId, entity.Id, "delivery.proposal.generated", "dev-agent",
            new { request.Repository, changes = proposal.Changes.Count }));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<DeliveryRun?> ApproveAsync(Guid projectId, Guid runId, CancellationToken ct)
    {
        var entity = await db.DeliveryRuns
            .SingleOrDefaultAsync(x => x.Id == runId && x.ProjectId == projectId, ct);
        if (entity is null) return null;
        if (entity.Status != "PendingApproval")
            throw new InvalidOperationException($"Delivery run cannot be approved from status '{entity.Status}'.");

        var proposal = JsonSerializer.Deserialize<CodeChangeProposal>(entity.ProposalJson, Json)
            ?? throw new InvalidOperationException("Delivery proposal is invalid.");

        var branch = string.IsNullOrWhiteSpace(entity.BranchName)
            ? $"sdlc-ai/{entity.Id.ToString("N")[..10]}"
            : entity.BranchName.Trim();

        await github.CreateBranchAsync(entity.Repository, branch, entity.DefaultBranch, ct);

        var context = JsonSerializer.Deserialize<RepositoryContext>(entity.ContextJson, Json)
            ?? throw new InvalidOperationException("Repository context is invalid.");
        var knownShas = context.RelevantFiles
            .Where(x => !string.IsNullOrWhiteSpace(x.Sha))
            .ToDictionary(x => x.Path, x => x.Sha, StringComparer.OrdinalIgnoreCase);

        foreach (var change in proposal.Changes)
        {
            knownShas.TryGetValue(change.Path, out var sha);
            await github.UpsertFileAsync(
                entity.Repository,
                change.Path,
                change.Content,
                $"SDLC AI: {change.Reason}",
                branch,
                change.Action.Equals("create", StringComparison.OrdinalIgnoreCase) ? null : sha,
                ct);
        }

        var body = BuildPullRequestBody(proposal);
        var pr = await github.CreatePullRequestAsync(
            entity.Repository,
            branch,
            $"SDLC AI: {proposal.Summary}",
            body,
            ct);

        entity.BranchName = branch;
        entity.PullRequestNumber = pr.Number;
        entity.PullRequestUrl = pr.Url;
        entity.Status = "PullRequestOpened";
        entity.ApprovedAt = DateTimeOffset.UtcNow;
        db.AuditEvents.Add(Audit(projectId, entity.Id, "delivery.pull-request.opened", "local-user",
            new { pr.Number, pr.Url, branch }));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<DeliveryRun?> GetAsync(Guid projectId, Guid runId, CancellationToken ct)
    {
        var entity = await db.DeliveryRuns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == runId && x.ProjectId == projectId, ct);
        return entity is null ? null : Map(entity);
    }

    private static string BuildPullRequestBody(CodeChangeProposal proposal) =>
        $"## SDLC AI delivery\n\n{proposal.Summary}\n\n" +
        "### Proposed validation\n" + string.Join("\n", proposal.Commands.Select(x => $"- `{x}`")) + "\n\n" +
        "### Risks / review focus\n" + (proposal.Risks.Count == 0 ? "- None identified by the agent." : string.Join("\n", proposal.Risks.Select(x => $"- {x}"))) +
        "\n\n> Generated from an approved implementation plan. Human review and CI remain required before merge.";

    private static DeliveryRun Map(DeliveryRunEntity x) => new(
        x.Id, x.ProjectId, x.ImplementationPlanId, x.Repository, x.DefaultBranch, x.BranchName,
        x.Status,
        JsonSerializer.Deserialize<CodeChangeProposal>(x.ProposalJson, Json)!,
        x.PullRequestNumber, x.PullRequestUrl, x.CreatedAt, x.ApprovedAt);

    private static AuditEventEntity Audit(Guid projectId, Guid resourceId, string action, string actor, object metadata) => new()
    {
        Id = Guid.NewGuid(), ProjectId = projectId, ResourceId = resourceId,
        Action = action, Actor = actor, MetadataJson = JsonSerializer.Serialize(metadata, Json),
        CreatedAt = DateTimeOffset.UtcNow
    };
}
