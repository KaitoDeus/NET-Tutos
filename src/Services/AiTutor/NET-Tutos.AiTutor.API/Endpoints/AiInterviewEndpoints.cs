using Microsoft.AspNetCore.Mvc;
using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;
using NET_Tutos.BuildingBlocks.Common.Models;

namespace NET_Tutos.AiTutor.API.Endpoints;

public static class AiInterviewEndpoints
{
    public static IEndpointRouteBuilder MapAiInterviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/aitutor/interview")
                             .WithTags("AI Mock Technical Interview");

        group.MapPost("/evaluate", async (
            [FromBody] EvaluateInterviewRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.EvaluateInterviewAnswerAsync(request);
            return Results.Ok(ApiResponse<EvaluateInterviewResponseDto>.Ok(result));
        })
        .WithName("EvaluateInterviewAnswer")
        .WithSummary("Evaluates a candidate response across 3 technical rubric dimensions");

        group.MapPost("/follow-up", async (
            [FromBody] InterviewFollowUpRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.AnswerFollowUpAsync(request);
            return Results.Ok(ApiResponse<InterviewFollowUpResponseDto>.Ok(result));
        })
        .WithName("EvaluateInterviewFollowUp")
        .WithSummary("Evaluates a candidate's justification on a deep-dive follow-up challenge");

        group.MapPost("/report", async (
            [FromBody] InterviewReportRequestDto request,
            [FromServices] IAiTutorEngine engine) =>
        {
            var result = await engine.GenerateInterviewReportAsync(request);
            return Results.Ok(ApiResponse<InterviewReportResponseDto>.Ok(result));
        })
        .WithName("GenerateInterviewReport")
        .WithSummary("Generates a 5-dimension competency report and study roadmap");

        return endpoints;
    }
}
