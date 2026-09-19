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
}
