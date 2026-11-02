using Microsoft.AspNetCore.Mvc;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.CodeRunner.Application.Contracts;
using NET_Tutos.CodeRunner.Application.Services;

namespace NET_Tutos.CodeRunner.API.Endpoints;

public static class RunnerEndpoints
{
    public static IEndpointRouteBuilder MapRunnerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/runner")
                             .WithTags("Code Runner");

        group.MapPost("/execute", async (
            [FromBody] ExecuteScriptRequest request,
            [FromServices] ICodeRunnerEngine engine) =>
        {
            var result = await engine.ExecuteAsync(request.Code, request.TimeoutMs);
            return Results.Ok(ApiResponse<ExecuteScriptResponse>.Ok(result));
        })
        .WithName("ExecuteScript")
        .WithSummary("Executes a standalone C# script in an isolated Roslyn sandbox");

        group.MapPost("/evaluate", async (
            [FromBody] EvaluateChallengeRequest request,
            [FromServices] ICodeRunnerEngine engine) =>
        {
            var result = await engine.EvaluateChallengeAsync(request);
            return Results.Ok(ApiResponse<EvaluateChallengeResponse>.Ok(result));
        })
        .WithName("EvaluateChallenge")
        .WithSummary("Evaluates a coding challenge solution against a matrix of test cases");

        return endpoints;
    }
}
