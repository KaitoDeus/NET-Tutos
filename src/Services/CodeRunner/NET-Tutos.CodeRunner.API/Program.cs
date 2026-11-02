using Microsoft.AspNetCore.Mvc;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.CodeRunner.Application.Contracts;
using NET_Tutos.CodeRunner.Application.Services;
using NET_Tutos.CodeRunner.Infrastructure.Engines;

var builder = WebApplication.CreateBuilder(args);

// Register Services
builder.Services.AddSingleton<ICodeRunnerEngine, RoslynCodeRunnerEngine>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "CodeRunner.API", timestamp = DateTime.UtcNow }));

// Code execution endpoint
app.MapPost("/api/v1/runner/execute", async (
    [FromBody] ExecuteScriptRequest request,
    [FromServices] ICodeRunnerEngine engine) =>
{
    var result = await engine.ExecuteAsync(request.Code, request.TimeoutMs);
    return Results.Ok(ApiResponse<ExecuteScriptResponse>.Ok(result));
})
.WithName("ExecuteScript")
.WithSummary("Executes a standalone C# script in an isolated Roslyn sandbox");

// Challenge evaluation endpoint
app.MapPost("/api/v1/runner/evaluate", async (
    [FromBody] EvaluateChallengeRequest request,
    [FromServices] ICodeRunnerEngine engine) =>
{
    var result = await engine.EvaluateChallengeAsync(request);
    return Results.Ok(ApiResponse<EvaluateChallengeResponse>.Ok(result));
})
.WithName("EvaluateChallenge")
.WithSummary("Evaluates a coding challenge solution against a matrix of test cases");

app.Run();

public partial class Program { }
