using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Integrations;
using SdlcAi.Api.Models;
using SdlcAi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddDbContext<SdlcAiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SdlcAi")
        ?? "Host=localhost;Port=5432;Database=sdlc_ai;Username=postgres;Password=postgres"));

builder.Services.AddScoped<PersistentProjectStore>();
builder.Services.AddScoped<QaPlanningService>();
builder.Services.AddScoped<DevelopmentPlanningService>();
builder.Services.AddScoped<ProjectMemoryService>();
builder.Services.AddScoped<RepositoryIntelligenceService>();
builder.Services.AddScoped<AgenticDeliveryService>();
builder.Services.AddScoped<E2ePlanningService>();
builder.Services.AddSingleton<IJiraAdapter, DisabledJiraAdapter>();

builder.Services.AddHttpClient<IGitHubDeliveryAdapter, GitHubRestDeliveryAdapter>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<AiAnalysisClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000");
});
builder.Services.AddScoped<IAnalysisEngine, RemoteAnalysisEngine>();

var app = builder.Build();
app.UseCors();
await app.InitializeDatabaseAsync();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "sdlc-ai-api" }));

app.MapPost("/api/projects", async (CreateProjectRequest request, PersistentProjectStore store, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Project name is required." });
    var project = await store.CreateAsync(request.Name.Trim(), request.Description?.Trim(), ct);
    return Results.Created($"/api/projects/{project.Id}", project);
});

app.MapGet("/api/projects", async (PersistentProjectStore store, CancellationToken ct) =>
    Results.Ok(await store.GetAllAsync(ct)));

app.MapPost("/api/projects/{projectId:guid}/analyses",
    async (Guid projectId, AnalyzeTranscriptRequest request, PersistentProjectStore store, IAnalysisEngine ai, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Transcript))
        return Results.BadRequest(new { error = "Transcript is required." });
    if (!await store.ExistsAsync(projectId, ct))
        return Results.NotFound();

    var result = await ai.AnalyzeAsync(request.Transcript, ct);
    var analysis = await store.AddAnalysisAsync(projectId, request.Transcript, result, ct);
    return Results.Created($"/api/projects/{projectId}/analyses/{analysis.Id}", analysis);
});

app.MapPost("/api/projects/{projectId:guid}/analyses/{analysisId:guid}/approve",
    async (Guid projectId, Guid analysisId, PersistentProjectStore store, CancellationToken ct) =>
    {
        var analysis = await store.ApproveAsync(projectId, analysisId, ct);
        return analysis is null ? Results.NotFound() : Results.Ok(analysis);
    });

app.MapGet("/api/projects/{projectId:guid}/audit",
    async (Guid projectId, PersistentProjectStore store, CancellationToken ct) =>
        Results.Ok(await store.AuditAsync(projectId, ct)));

app.MapPost("/api/projects/{projectId:guid}/memory",
    async (Guid projectId, AddProjectMemoryRequest request, ProjectMemoryService memory, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(new { error = "Memory content is required." });
        try
        {
            return Results.Ok(await memory.AddAsync(projectId, request, ct));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    });

app.MapGet("/api/projects/{projectId:guid}/memory",
    async (Guid projectId, string? q, ProjectMemoryService memory, CancellationToken ct) =>
        Results.Ok(await memory.SearchAsync(projectId, q, ct)));

app.MapPost("/api/projects/{projectId:guid}/repositories/analyze",
    async (Guid projectId, RepositoryAnalysisRequest request, RepositoryIntelligenceService repositories, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(request.Repository) || string.IsNullOrWhiteSpace(request.Query))
            return Results.BadRequest(new { error = "Repository and query are required." });
        return Results.Ok(await repositories.AnalyzeAsync(projectId, request, ct));
    });

app.MapPost("/api/projects/{projectId:guid}/qa/test-plans",
    async (Guid projectId, GenerateTestPlanRequest request, QaPlanningService qa, CancellationToken ct) =>
    {
        var plan = await qa.GenerateAsync(projectId, request.AnalysisId, ct);
        return plan is null ? Results.BadRequest(new { error = "An approved analysis is required." }) : Results.Ok(plan);
    });

app.MapPost("/api/projects/{projectId:guid}/qa/test-plans/{testPlanId:guid}/approve",
    async (Guid projectId, Guid testPlanId, QaPlanningService qa, CancellationToken ct) =>
    {
        var plan = await qa.ApproveAsync(projectId, testPlanId, ct);
        return plan is null ? Results.NotFound() : Results.Ok(plan);
    });

app.MapPost("/api/projects/{projectId:guid}/qa/test-plans/{testPlanId:guid}/e2e-proposal",
    async (Guid projectId, Guid testPlanId, E2ePlanningService e2e, CancellationToken ct) =>
    {
        var proposal = await e2e.GenerateAsync(projectId, testPlanId, ct);
        return proposal is null
            ? Results.BadRequest(new { error = "An approved QA plan is required." })
            : Results.Ok(proposal);
    });

app.MapPost("/api/projects/{projectId:guid}/development/plans",
    async (Guid projectId, GenerateImplementationPlanRequest request, DevelopmentPlanningService dev, CancellationToken ct) =>
    {
        var plan = await dev.GenerateAsync(projectId, request.AnalysisId, request.TestPlanId, ct);
        return plan is null
            ? Results.BadRequest(new { error = "Approved requirements and QA test plan are required." })
            : Results.Ok(plan);
    });

app.MapPost("/api/projects/{projectId:guid}/development/plans/{planId:guid}/approve",
    async (Guid projectId, Guid planId, DevelopmentPlanningService dev, CancellationToken ct) =>
    {
        var plan = await dev.ApproveAsync(projectId, planId, ct);
        return plan is null ? Results.NotFound() : Results.Ok(plan);
    });

app.MapPost("/api/projects/{projectId:guid}/development/delivery-runs",
    async (Guid projectId, CreateDeliveryRunRequest request, AgenticDeliveryService delivery, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(request.Repository))
            return Results.BadRequest(new { error = "Repository is required." });
        var run = await delivery.CreateAsync(projectId, request, ct);
        return run is null
            ? Results.BadRequest(new { error = "An approved implementation plan is required." })
            : Results.Ok(run);
    });

app.MapGet("/api/projects/{projectId:guid}/development/delivery-runs/{runId:guid}",
    async (Guid projectId, Guid runId, AgenticDeliveryService delivery, CancellationToken ct) =>
    {
        var run = await delivery.GetAsync(projectId, runId, ct);
        return run is null ? Results.NotFound() : Results.Ok(run);
    });

app.MapPost("/api/projects/{projectId:guid}/development/delivery-runs/{runId:guid}/approve",
    async (Guid projectId, Guid runId, AgenticDeliveryService delivery, CancellationToken ct) =>
    {
        try
        {
            var run = await delivery.ApproveAsync(projectId, runId, ct);
            return run is null ? Results.NotFound() : Results.Ok(run);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

app.Run();
