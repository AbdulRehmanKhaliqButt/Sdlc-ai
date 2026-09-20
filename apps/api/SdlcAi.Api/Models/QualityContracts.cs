namespace SdlcAi.Api.Models;

public sealed record GenerateTestPlanRequest(Guid AnalysisId);

public sealed record TestCase(
    string Id,
    string Title,
    string Type,
    string Preconditions,
    IReadOnlyList<string> Steps,
    string ExpectedResult,
    IReadOnlyList<string> AcceptanceCriteria);

public sealed record TestPlan(
    Guid Id,
    Guid ProjectId,
    Guid AnalysisId,
    string Status,
    IReadOnlyList<TestCase> TestCases,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);
