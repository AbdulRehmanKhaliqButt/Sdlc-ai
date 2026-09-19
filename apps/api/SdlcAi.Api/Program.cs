using Microsoft.EntityFrameworkCore;
using SdlcAi.Api.Data;
using SdlcAi.Api.Models;
using SdlcAi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddDbContext<SdlcAiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SdlcAi")
        ?? "Host=localhost;Port=5432;Database=sdlc_ai;Username=postgres;Password=postgres"));
builder.Services.AddScoped<PersistentProjectStore>();
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

app.Run();
