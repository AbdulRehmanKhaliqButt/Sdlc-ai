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
    ProjectMemoryService memory,
    SandboxRunnerClient sandbox,
    IConfiguration configuration)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly int _maxRepairAttempts = Math.Clamp(
        configuration.GetValue<int?>("Sandbox:MaxRepairAttempts") ?? 2, 0, 5);

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

        var entity = new DeliveryRunEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ImplementationPlanId = plan.Id,
            Repository = request.Repository,
            DefaultBranch = context.DefaultBranch,
            BranchName = request.BranchName,
            Status = proposal.Changes.Count == 0 ? "NoChanges" : "PendingValidation",
            ContextJson = JsonSerializer.Serialize(context, Json),
            ProposalJson = JsonSerializer.Serialize(proposal, Json),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.DeliveryRuns.Add(entity);
        db.AuditEvents.Add(Audit(projectId, entity.Id, "delivery.proposal.generated", "dev-agent",
            new { request.Repository, changes = proposal.Changes.Count, commands = proposal.Commands.Count }));
        await db.SaveChangesAsync(ct);

        if (proposal.Changes.Count == 0)
            return Map(entity);

        return await ValidateAndRepairAsync(entity, context, memories, proposal, ct);
    }

    public async Task<DeliveryRun?> RevalidateAsync(Guid projectId, Guid runId, CancellationToken ct)
    {
        var entity = await db.DeliveryRuns
            .SingleOrDefaultAsync(x => x.Id == runId && x.ProjectId == projectId, ct);
        if (entity is null) return null;
        if (entity.Status == "PullRequestOpened")
            throw new InvalidOperationException("A pull request has already been created for this delivery run.");
        if (entity.Status == "NoChanges")
            throw new InvalidOperationException("There are no proposed changes to validate.");

        var context = JsonSerializer.Deserialize<RepositoryContext>(entity.ContextJson, Json)
            ?? throw new InvalidOperationException("Repository context is invalid.");
        var proposal = JsonSerializer.Deserialize<CodeChangeProposal>(entity.ProposalJson, Json)
            ?? throw new InvalidOperationException("Delivery proposal is invalid.");
        var memories = await memory.RecallAsync(
            projectId,
            proposal.Changes.Select(x => x.Path).Append(proposal.Summary),
            ct);

        entity.Status = "PendingValidation";
        await db.SaveChangesAsync(ct);
        return await ValidateAndRepairAsync(entity, context, memories, proposal, ct);
    }

    private async Task<DeliveryRun> ValidateAndRepairAsync(
        DeliveryRunEntity entity,
        RepositoryContext context,
        IReadOnlyList<ProjectMemory> memories,
        CodeChangeProposal initialProposal,
        CancellationToken ct)
    {
        var proposal = initialProposal;
        var attempts = new List<RepairAttempt>();

        if (proposal.Commands.Count == 0)
        {
            var evidence = new ValidationEvidence(
                false,
                0,
                attempts,
                "The AI proposal did not include validation commands. PR creation remains blocked.");
            entity.Status = "ValidationFailed";
            entity.ValidationJson = JsonSerializer.Serialize(evidence, Json);
            await db.SaveChangesAsync(ct);
            return Map(entity);
        }

        for (var attemptNumber = 1; attemptNumber <= _maxRepairAttempts + 1; attemptNumber++)
        {
            SandboxExecutionResult validation;
            try
            {
                validation = await sandbox.ExecuteAsync(
                    entity.Repository,
                    entity.DefaultBranch,
                    proposal.Changes,
                    proposal.Commands,
                    ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                validation = new SandboxExecutionResult(
                    false,
                    entity.Repository,
                    entity.DefaultBranch,
                    proposal.Changes.Select(x => x.Path).ToList(),
                    new[]
                    {
                        new SandboxCommandResult(
                            "sandbox-runner",
                            125,
                            0,
                            "",
                            ex.Message,
                            true)
                    });
            }

            attempts.Add(new RepairAttempt(attemptNumber, proposal.Summary, validation));
            db.AuditEvents.Add(Audit(entity.ProjectId, entity.Id, "delivery.sandbox.executed", "sandbox-runner",
                new
                {
                    attempt = attemptNumber,
                    validation.Passed,
                    commands = validation.Commands.Select(x => new { x.Command, x.ExitCode, x.Blocked })
                }));

            if (validation.Passed)
            {
                var evidence = new ValidationEvidence(true, attemptNumber - 1, attempts, null);
                entity.ProposalJson = JsonSerializer.Serialize(proposal, Json);
                entity.ValidationJson = JsonSerializer.Serialize(evidence, Json);
                entity.Status = "ValidatedPendingApproval";
                db.AuditEvents.Add(Audit(entity.ProjectId, entity.Id, "delivery.sandbox.passed", "sandbox-runner",
                    new { attempt = attemptNumber, repairs = attemptNumber - 1 }));
                await db.SaveChangesAsync(ct);
                return Map(entity);
            }

            if (attemptNumber > _maxRepairAttempts)
            {
                var evidence = new ValidationEvidence(
                    false,
                    attemptNumber - 1,
                    attempts,
                    BuildFailureSummary(validation));
                entity.ProposalJson = JsonSerializer.Serialize(proposal, Json);
                entity.ValidationJson = JsonSerializer.Serialize(evidence, Json);
                entity.Status = "ValidationFailed";
                db.AuditEvents.Add(Audit(entity.ProjectId, entity.Id, "delivery.sandbox.failed", "sandbox-runner",
                    new { attempts = attemptNumber }));
                await db.SaveChangesAsync(ct);
                return Map(entity);
            }

            proposal = await ai.RepairCodeChangesAsync(
                entity.Repository,
                proposal,
                validation,
                context.RelevantFiles,
                memories,
                ct);

            if (proposal.Changes.Count == 0)
            {
                var evidence = new ValidationEvidence(
                    false,
                    attemptNumber,
                    attempts,
                    "The repair agent returned no code changes.");
                entity.ProposalJson = JsonSerializer.Serialize(proposal, Json);
                entity.ValidationJson = JsonSerializer.Serialize(evidence, Json);
                entity.Status = "ValidationFailed";
                await db.SaveChangesAsync(ct);
                return Map(entity);
            }

            db.AuditEvents.Add(Audit(entity.ProjectId, entity.Id, "delivery.repair.proposed", "dev-agent",
                new { afterAttempt = attemptNumber, changes = proposal.Changes.Count, commands = proposal.Commands.Count }));
            entity.ProposalJson = JsonSerializer.Serialize(proposal, Json);
            await db.SaveChangesAsync(ct);
        }

        throw new InvalidOperationException("Unexpected sandbox validation state.");
    }

    public async Task<DeliveryRun?> ApproveAsync(Guid projectId, Guid runId, CancellationToken ct)
    {
        var entity = await db.DeliveryRuns
            .SingleOrDefaultAsync(x => x.Id == runId && x.ProjectId == projectId, ct);
        if (entity is null) return null;
        if (entity.Status != "ValidatedPendingApproval")
            throw new InvalidOperationException(
                $"Delivery run cannot be approved from status '{entity.Status}'. Sandbox validation must pass first.");

        var evidence = string.IsNullOrWhiteSpace(entity.ValidationJson)
            ? null
            : JsonSerializer.Deserialize<ValidationEvidence>(entity.ValidationJson, Json);
        if (evidence?.Passed != true)
            throw new InvalidOperationException("Sandbox validation evidence is missing or did not pass.");

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

        var body = BuildPullRequestBody(proposal, evidence);
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
            new { pr.Number, pr.Url, branch, sandboxValidated = true, repairs = evidence.RepairCount }));
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<DeliveryRun?> GetAsync(Guid projectId, Guid runId, CancellationToken ct)
    {
        var entity = await db.DeliveryRuns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == runId && x.ProjectId == projectId, ct);
        return entity is null ? null : Map(entity);
    }

    private static string BuildFailureSummary(SandboxExecutionResult validation)
    {
        var failed = validation.Commands.LastOrDefault(x => x.ExitCode != 0);
        return failed is null
            ? "Sandbox validation did not pass."
            : $"{failed.Command} failed with exit code {failed.ExitCode}: {Trim(failed.Stderr, 1200)}";
    }

    private static string BuildPullRequestBody(CodeChangeProposal proposal, ValidationEvidence validation) =>
        $"## SDLC AI delivery\n\n{proposal.Summary}\n\n" +
        $"### Sandbox validation\n- Passed: yes\n- Execution attempts: {validation.Attempts.Count}\n- AI repairs: {validation.RepairCount}\n\n" +
        "### Validated commands\n" +
        string.Join("\n", validation.Attempts.Last().Validation.Commands.Select(
            x => $"- {x.Command} — exit {x.ExitCode} ({x.DurationMs} ms)")) +
        "\n\n### Risks / review focus\n" +
        (proposal.Risks.Count == 0
            ? "- None identified by the agent."
            : string.Join("\n", proposal.Risks.Select(x => $"- {x}"))) +
        "\n\n> Changes passed isolated pre-PR validation. Human review and normal CI are still required before merge.";

    private static string Trim(string value, int max) =>
        string.IsNullOrWhiteSpace(value) || value.Length <= max ? value : value[^max..];

    private static DeliveryRun Map(DeliveryRunEntity x) => new(
        x.Id,
        x.ProjectId,
        x.ImplementationPlanId,
        x.Repository,
        x.DefaultBranch,
        x.BranchName,
        x.Status,
        JsonSerializer.Deserialize<CodeChangeProposal>(x.ProposalJson, Json)!,
        string.IsNullOrWhiteSpace(x.ValidationJson)
            ? null
            : JsonSerializer.Deserialize<ValidationEvidence>(x.ValidationJson, Json),
        x.PullRequestNumber,
        x.PullRequestUrl,
        x.CreatedAt,
        x.ApprovedAt);

    private static AuditEventEntity Audit(Guid projectId, Guid resourceId, string action, string actor, object metadata) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        ResourceId = resourceId,
        Action = action,
        Actor = actor,
        MetadataJson = JsonSerializer.Serialize(metadata, Json),
        CreatedAt = DateTimeOffset.UtcNow
    };
}
