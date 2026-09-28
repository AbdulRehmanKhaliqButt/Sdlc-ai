namespace SdlcAi.Api.Models;

public sealed record RepositoryAnalysisRequest(string Repository, string Query);
public sealed record RepositoryFileContext(string Path, string? Sha, string Content);
public sealed record RepositoryIntelligence(
    Guid Id,
    Guid ProjectId,
    string Repository,
    string DefaultBranch,
    IReadOnlyList<RepositoryFileContext> RelevantFiles,
    IReadOnlyList<string> Signals,
    DateTimeOffset AnalyzedAt);

public sealed record AddProjectMemoryRequest(string Kind, string Content, IReadOnlyList<string>? Tags);
public sealed record ProjectMemory(
    Guid Id,
    Guid ProjectId,
    string Kind,
    string Content,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt);

public sealed record CreateDeliveryRunRequest(Guid ImplementationPlanId, string Repository, string? BranchName);

public sealed record ProposedFileChange(
    string Path,
    string Action,
    string Content,
    string Reason);

public sealed record CodeChangeProposal(
    string Summary,
    IReadOnlyList<ProposedFileChange> Changes,
    IReadOnlyList<string> Commands,
    IReadOnlyList<string> Risks);

public sealed record SandboxCommandResult(
    string Command,
    int ExitCode,
    int DurationMs,
    string Stdout,
    string Stderr,
    bool Blocked);

public sealed record SandboxExecutionResult(
    bool Passed,
    string Repository,
    string Branch,
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<SandboxCommandResult> Commands);

public sealed record RepairAttempt(
    int Attempt,
    string ProposalSummary,
    SandboxExecutionResult Validation);

public sealed record ValidationEvidence(
    bool Passed,
    int RepairCount,
    IReadOnlyList<RepairAttempt> Attempts,
    string? FailureSummary);

public sealed record DeliveryRun(
    Guid Id,
    Guid ProjectId,
    Guid ImplementationPlanId,
    string Repository,
    string DefaultBranch,
    string? BranchName,
    string Status,
    CodeChangeProposal Proposal,
    ValidationEvidence? Validation,
    int? PullRequestNumber,
    string? PullRequestUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);

public sealed record E2eProposal(
    Guid ProjectId,
    Guid TestPlanId,
    string SuggestedPath,
    string PlaywrightSpec,
    IReadOnlyList<string> Traceability);
