using System.Net.Http.Json;
using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public sealed class AiAnalysisClient(HttpClient httpClient)
{
    public async Task<RequirementAnalysis> AnalyzeAsync(string transcript, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/v1/analyze-requirements",
            new { transcript },
            ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RequirementAnalysis>(cancellationToken: ct)
            ?? throw new InvalidOperationException("AI service returned an empty response.");
    }

    public async Task<CodeChangeProposal> ProposeCodeChangesAsync(
        string repository,
        IReadOnlyList<ImplementationTask> tasks,
        IReadOnlyList<RepositoryFileContext> files,
        IReadOnlyList<ProjectMemory> memory,
        CancellationToken ct)
    {
        var compactFiles = files.Select(x =>
            x.Content.Length > 16_000
                ? x with { Content = x.Content[..16_000] }
                : x).ToList();

        using var response = await httpClient.PostAsJsonAsync(
            "/v1/propose-code-changes",
            new
            {
                repository,
                tasks,
                files = compactFiles,
                memory
            },
            ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CodeChangeProposal>(cancellationToken: ct)
            ?? throw new InvalidOperationException("AI service returned an empty code-change proposal.");
    }
}