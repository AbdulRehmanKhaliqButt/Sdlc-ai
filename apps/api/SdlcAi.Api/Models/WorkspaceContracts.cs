namespace SdlcAi.Api.Models;

public sealed record WorkspaceSnapshot(
    Project Project,
    Analysis? LatestAnalysis,
    TestPlan? LatestTestPlan,
    ImplementationPlan? LatestImplementationPlan,
    RepositoryIntelligence? LatestRepositoryAnalysis,
    DeliveryRun? LatestDeliveryRun,
    IReadOnlyList<ProjectMemory> Memory);
