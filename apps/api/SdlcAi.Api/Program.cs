using SdlcAi.Api.Models;
using SdlcAi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<ProjectStore>();
builder.Services.AddHttpClient<AiAnalysisClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000");
});
builder.Services.AddScoped<IAnalysisEngine, RemoteAnalysisEngine>();

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "sdlc-ai-api" }));

app.MapPost("/api/projects", (CreateProjectRequest request, ProjectStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Project name is required." });

    var project = store.Create(request.Name.Trim(), request.Description?.Trim());
    return Results.Created($"/api/projects/{project.Id}", project);
});

app.MapGet("/api/projects", (ProjectStore store) => Results.Ok(store.GetAll()));

app.MapPost("/api/projects/{projectId:guid}/analyses",
    async (Guid projectId, AnalyzeTranscriptRequest request, ProjectStore store, IAnalysisEngine ai, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Transcript))
        return Results.BadRequest(new { error = "Transcript is required." });

    if (!store.Exists(projectId))
        return Results.NotFound();

    var result = await ai.AnalyzeAsync(request.Transcript, ct);
    var analysis = store.AddAnalysis(projectId, request.Transcript, result);
    return Results.Created($"/api/projects/{projectId}/analyses/{analysis.Id}", analysis);
});

app.MapPost("/api/projects/{projectId:guid}/analyses/{analysisId:guid}/approve",
    (Guid projectId, Guid analysisId, ProjectStore store) =>
{
    var analysis = store.Approve(projectId, analysisId);
    return analysis is null ? Results.NotFound() : Results.Ok(analysis);
});

app.Run();
