using SdlcAi.Api.Models;

namespace SdlcAi.Api.Services;

public interface IAnalysisEngine
{
    Task<RequirementAnalysis> AnalyzeAsync(string transcript, CancellationToken ct);
}

public sealed class RemoteAnalysisEngine(AiAnalysisClient client) : IAnalysisEngine
{
    public Task<RequirementAnalysis> AnalyzeAsync(string transcript, CancellationToken ct) =>
        client.AnalyzeAsync(transcript, ct);
}
