using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SdlcAi.Api.Observability;

public static class Telemetry
{
    public const string SourceName = "SdlcAi";
    public static readonly ActivitySource Activities = new(SourceName);
    public static readonly Meter Meter = new(SourceName);
    public static readonly Counter<long> WorkflowActions = Meter.CreateCounter<long>("sdlc_ai.workflow.actions");
    public static readonly Histogram<double> AiLatencyMs = Meter.CreateHistogram<double>("sdlc_ai.ai.latency_ms");
}
