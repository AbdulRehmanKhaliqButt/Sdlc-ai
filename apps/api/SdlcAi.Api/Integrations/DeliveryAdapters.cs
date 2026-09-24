using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Integrations;

public sealed record ExternalTicket(string Key, string Url);
public sealed record PullRequestResult(int Number, string Url, string Branch);
public sealed record RepositoryContext(
    string DefaultBranch,
    IReadOnlyList<RepositoryFileContext> RelevantFiles,
    IReadOnlyList<string> Signals);

public interface IJiraAdapter
{
    Task<ExternalTicket> CreateTicketAsync(string title, string description, IReadOnlyList<string> acceptanceCriteria, CancellationToken ct);
}

public interface IGitHubDeliveryAdapter
{
    Task<RepositoryContext> GetContextAsync(string repository, string query, CancellationToken ct);
    Task CreateBranchAsync(string repository, string branch, string fromBranch, CancellationToken ct);
    Task UpsertFileAsync(string repository, string path, string content, string message, string branch, string? currentSha, CancellationToken ct);
    Task<PullRequestResult> CreatePullRequestAsync(string repository, string branch, string title, string body, CancellationToken ct);
}

public sealed class DisabledJiraAdapter : IJiraAdapter
{
    public Task<ExternalTicket> CreateTicketAsync(string title, string description, IReadOnlyList<string> acceptanceCriteria, CancellationToken ct) =>
        throw new InvalidOperationException("Jira integration is not configured.");
}

public sealed class GitHubRestDeliveryAdapter(HttpClient http, IConfiguration configuration) : IGitHubDeliveryAdapter
{
    private readonly string? _token = configuration["GitHub:Token"];

    public async Task<RepositoryContext> GetContextAsync(string repository, string query, CancellationToken ct)
    {
        ConfigureHeaders(requireWrite: false);

        using var repoResponse = await http.GetAsync($"/repos/{repository}", ct);
        repoResponse.EnsureSuccessStatusCode();
        using var repoDoc = JsonDocument.Parse(await repoResponse.Content.ReadAsStringAsync(ct));
        var defaultBranch = repoDoc.RootElement.GetProperty("default_branch").GetString() ?? "main";

        using var treeResponse = await http.GetAsync($"/repos/{repository}/git/trees/{Uri.EscapeDataString(defaultBranch)}?recursive=1", ct);
        treeResponse.EnsureSuccessStatusCode();
        using var treeDoc = JsonDocument.Parse(await treeResponse.Content.ReadAsStringAsync(ct));

        var tokens = query.Split(new[] { ' ', '\r', '\n', '\t', '/', '.', '-', '_' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToArray();

        var candidates = treeDoc.RootElement.GetProperty("tree").EnumerateArray()
            .Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "blob")
            .Select(x => new {
                Path = x.GetProperty("path").GetString()!,
                Sha = x.GetProperty("sha").GetString()
            })
            .Where(x => IsUsefulTextFile(x.Path))
            .Select(x => new {
                x.Path,
                x.Sha,
                Score = ScorePath(x.Path, tokens)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path.Length)
            .Take(14)
            .ToList();

        var files = new List<RepositoryFileContext>();
        foreach (var candidate in candidates)
        {
            using var response = await http.GetAsync(
                $"/repos/{repository}/contents/{EscapePath(candidate.Path)}?ref={Uri.EscapeDataString(defaultBranch)}", ct);
            if (!response.IsSuccessStatusCode) continue;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("content", out var contentElement)) continue;
            var encoded = contentElement.GetString()?.Replace("\n", "");
            if (string.IsNullOrWhiteSpace(encoded)) continue;

            var bytes = Convert.FromBase64String(encoded);
            if (bytes.Length > 80_000) continue;
            files.Add(new RepositoryFileContext(candidate.Path, candidate.Sha, Encoding.UTF8.GetString(bytes)));
        }

        var signals = new List<string>();
        if (candidates.Any(x => x.Path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || x.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))) signals.Add(".NET");
        if (candidates.Any(x => x.Path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))) signals.Add("Node/TypeScript");
        if (candidates.Any(x => x.Path.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase) || x.Path.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase))) signals.Add("Python");
        if (candidates.Any(x => x.Path.Contains("playwright", StringComparison.OrdinalIgnoreCase))) signals.Add("Playwright");
        if (candidates.Any(x => x.Path.Contains("Dockerfile", StringComparison.OrdinalIgnoreCase) || x.Path.EndsWith("docker-compose.yml", StringComparison.OrdinalIgnoreCase))) signals.Add("Containers");

        return new RepositoryContext(defaultBranch, files, signals);
    }

    public async Task CreateBranchAsync(string repository, string branch, string fromBranch, CancellationToken ct)
    {
        ConfigureHeaders(requireWrite: true);
        using var sourceResponse = await http.GetAsync($"/repos/{repository}/git/ref/heads/{Uri.EscapeDataString(fromBranch)}", ct);
        sourceResponse.EnsureSuccessStatusCode();
        using var sourceDoc = JsonDocument.Parse(await sourceResponse.Content.ReadAsStringAsync(ct));
        var sha = sourceDoc.RootElement.GetProperty("object").GetProperty("sha").GetString()
            ?? throw new InvalidOperationException("GitHub did not return the source branch SHA.");

        using var createResponse = await http.PostAsJsonAsync($"/repos/{repository}/git/refs",
            new { @ref = $"refs/heads/{branch}", sha }, ct);
        createResponse.EnsureSuccessStatusCode();
    }

    public async Task UpsertFileAsync(string repository, string path, string content, string message, string branch, string? currentSha, CancellationToken ct)
    {
        ConfigureHeaders(requireWrite: true);

        if (string.IsNullOrWhiteSpace(currentSha))
        {
            using var existing = await http.GetAsync(
                $"/repos/{repository}/contents/{EscapePath(path)}?ref={Uri.EscapeDataString(branch)}", ct);
            if (existing.IsSuccessStatusCode)
            {
                using var existingDoc = JsonDocument.Parse(await existing.Content.ReadAsStringAsync(ct));
                currentSha = existingDoc.RootElement.GetProperty("sha").GetString();
            }
            else if (existing.StatusCode != HttpStatusCode.NotFound)
            {
                existing.EnsureSuccessStatusCode();
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            ["branch"] = branch
        };
        if (!string.IsNullOrWhiteSpace(currentSha)) payload["sha"] = currentSha;

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/repos/{repository}/contents/{EscapePath(path)}")
        {
            Content = JsonContent.Create(payload)
        };
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PullRequestResult> CreatePullRequestAsync(string repository, string branch, string title, string body, CancellationToken ct)
    {
        ConfigureHeaders(requireWrite: true);

        using var repoResponse = await http.GetAsync($"/repos/{repository}", ct);
        repoResponse.EnsureSuccessStatusCode();
        using var repoDoc = JsonDocument.Parse(await repoResponse.Content.ReadAsStringAsync(ct));
        var defaultBranch = repoDoc.RootElement.GetProperty("default_branch").GetString() ?? "main";

        using var response = await http.PostAsJsonAsync($"/repos/{repository}/pulls",
            new { title, head = branch, @base = defaultBranch, body }, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return new PullRequestResult(
            doc.RootElement.GetProperty("number").GetInt32(),
            doc.RootElement.GetProperty("html_url").GetString()!,
            branch);
    }

    private void ConfigureHeaders(bool requireWrite)
    {
        http.DefaultRequestHeaders.UserAgent.Clear();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("sdlc-ai", "1.0"));
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Remove("X-GitHub-Api-Version");
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        if (!string.IsNullOrWhiteSpace(_token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        else if (requireWrite)
            throw new InvalidOperationException("GitHub:Token is required for repository write operations.");
    }

    private static int ScorePath(string path, IReadOnlyList<string> tokens)
    {
        var score = tokens.Count(t => path.Contains(t, StringComparison.OrdinalIgnoreCase)) * 10;
        if (path.EndsWith("README.md", StringComparison.OrdinalIgnoreCase)) score += 4;
        if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase) || path.EndsWith("pyproject.toml", StringComparison.OrdinalIgnoreCase)) score += 5;
        if (path.Contains("src/", StringComparison.OrdinalIgnoreCase) || path.Contains("apps/", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (path.Contains("test", StringComparison.OrdinalIgnoreCase)) score += 2;
        return score;
    }

    private static bool IsUsefulTextFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".cs" or ".csproj" or ".sln" or ".ts" or ".tsx" or ".js" or ".jsx" or ".py" or ".json" or ".md" or ".yml" or ".yaml" or ".xml" or ".sql"
            || Path.GetFileName(path).Equals("Dockerfile", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapePath(string path) =>
        string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
}
