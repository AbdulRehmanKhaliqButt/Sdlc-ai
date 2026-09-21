namespace SdlcAi.Api.Integrations;

public sealed record ExternalTicket(string Key, string Url);
public sealed record PullRequestResult(int Number, string Url, string Branch);
public sealed record RepositoryContext(string DefaultBranch, IReadOnlyList<string> RelevantFiles);

public interface IJiraAdapter
{
    Task<ExternalTicket> CreateTicketAsync(string title, string description, IReadOnlyList<string> acceptanceCriteria, CancellationToken ct);
}

public interface IGitHubDeliveryAdapter
{
    Task<RepositoryContext> GetContextAsync(string repository, string query, CancellationToken ct);
    Task<PullRequestResult> CreatePullRequestAsync(string repository, string branch, string title, string body, CancellationToken ct);
}

public sealed class DisabledJiraAdapter : IJiraAdapter
{
    public Task<ExternalTicket> CreateTicketAsync(string title, string description, IReadOnlyList<string> acceptanceCriteria, CancellationToken ct) =>
        throw new InvalidOperationException("Jira integration is not configured.");
}

public sealed class DisabledGitHubDeliveryAdapter : IGitHubDeliveryAdapter
{
    public Task<RepositoryContext> GetContextAsync(string repository, string query, CancellationToken ct) =>
        throw new InvalidOperationException("GitHub delivery integration is not configured.");
    public Task<PullRequestResult> CreatePullRequestAsync(string repository, string branch, string title, string body, CancellationToken ct) =>
        throw new InvalidOperationException("GitHub delivery integration is not configured.");
}
