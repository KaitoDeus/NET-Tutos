using Microsoft.AspNetCore.Mvc;
using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;
using NET_Tutos.BuildingBlocks.Common.Models;

namespace NET_Tutos.AiTutor.API.Endpoints;

public static class AiAnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAiAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/aitutor")
                             .WithTags("AI Tutor Analysis");

        group.MapPost("/chat", async (
            [FromBody] AiChatRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.ChatAsync(request);
            return Results.Ok(ApiResponse<AiChatResponseDto>.Ok(result));
        })
        .WithName("AiTutorChat")
        .WithSummary("Conversational mentorship with Atus AI Agent");

        group.MapPost("/explain-error", async (
            [FromBody] ExplainErrorRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.ExplainErrorAsync(request);
            return Results.Ok(ApiResponse<ExplainErrorResponseDto>.Ok(result));
        })
        .WithName("ExplainCompilerError")
        .WithSummary("Diagnoses C# compiler or runtime errors and provides fixes");

        group.MapPost("/analyze-complexity", async (
            [FromBody] AnalyzeComplexityRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.AnalyzeComplexityAsync(request);
            return Results.Ok(ApiResponse<AnalyzeComplexityResponseDto>.Ok(result));
        })
        .WithName("AnalyzeComplexity")
        .WithSummary("Analyzes algorithmic Big-O time and space complexity");

        group.MapPost("/diagnose-failure", async (
            [FromBody] DiagnoseTestFailureRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.DiagnoseTestFailureAsync(request);
            return Results.Ok(ApiResponse<DiagnoseTestFailureResponseDto>.Ok(result));
        })
        .WithName("DiagnoseTestFailure")
        .WithSummary("Diagnoses why a coding challenge test case failed");

        group.MapPost("/generate-unit-test", async (
            [FromBody] GenerateUnitTestRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.GenerateUnitTestAsync(request);
            return Results.Ok(ApiResponse<GenerateUnitTestResponseDto>.Ok(result));
        })
        .WithName("GenerateUnitTest")
        .WithSummary("Synthesizes an xUnit AAA unit test suite for C# methods");

        group.MapPost("/review-code", async (
            [FromBody] CodeReviewRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.ReviewCodeAsync(request);
            return Results.Ok(ApiResponse<CodeReviewResponseDto>.Ok(result));
        })
        .WithName("ReviewCode")
        .WithSummary("Reviews C# source code against Modern .NET 10 best practices");

        group.MapPost("/hint", async (
            [FromBody] ChallengeHintRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.GetChallengeHintAsync(request);
            return Results.Ok(ApiResponse<ChallengeHintResponseDto>.Ok(result));
        })
        .WithName("GetChallengeHint")
        .WithSummary("Provides progressive Socratic hints for coding challenges");

        group.MapGet("/status", async ([FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.CheckStatusAsync();
            return Results.Ok(ApiResponse<AiTutorStatusResponseDto>.Ok(result));
        })
        .WithName("GetAiTutorStatus")
        .WithSummary("Checks health and availability of the AI Tutor engine");

        return endpoints;
    }
}
