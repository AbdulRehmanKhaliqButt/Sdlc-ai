using System.Net.Http.Json;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class SandboxRunnerClient(HttpClient http, IConfiguration configuration)
{
    private readonly string? _githubToken = configuration["GitHub:Token"];

    public async Task<SandboxExecutionResult> ExecuteAsync(
        string repository,
        string branch,
        IReadOnlyList<ProposedFileChange> changes,
        IReadOnlyList<string> commands,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/execute")
        {
            Content = JsonContent.Create(new
            {
                repository,
                branch,
                changes,
                commands
            })
        };

        if (!string.IsNullOrWhiteSpace(_githubToken))
            request.Headers.TryAddWithoutValidation("X-GitHub-Token", _githubToken);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Sandbox runner failed: {detail}");
        }

        return await response.Content.ReadFromJsonAsync<SandboxExecutionResult>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Sandbox runner returned an empty result.");
    }
}
