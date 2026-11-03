using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.AiTutor.Application.Common.Exceptions;
using NET_Tutos.AiTutor.Domain.Exceptions;

namespace NET_Tutos.AiTutor.API.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in AiTutor.API: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            EmptySourceCodeException emptyEx => (
                (int)HttpStatusCode.BadRequest,
                "Empty Source Code",
                emptyEx.Message
            ),
            InvalidRubricScoreException rubricEx => (
                (int)HttpStatusCode.UnprocessableEntity,
                "Invalid Rubric Score",
                rubricEx.Message
            ),
            AiTutorValidationException valEx => (
                (int)HttpStatusCode.BadRequest,
                "Validation Error",
                valEx.Message
            ),
            AiTutorDomainException domainEx => (
                (int)HttpStatusCode.UnprocessableEntity,
                "AiTutor Domain Error",
                domainEx.Message
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                exception.Message
            )
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
