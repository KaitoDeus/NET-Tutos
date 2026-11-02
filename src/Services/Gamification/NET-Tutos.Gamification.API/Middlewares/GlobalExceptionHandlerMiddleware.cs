using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.Gamification.Application.Common.Exceptions;
using NET_Tutos.Gamification.Domain.Exceptions;

namespace NET_Tutos.Gamification.API.Middlewares;

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
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            GamificationValidationException valEx => (
                (int)HttpStatusCode.BadRequest,
                "Validation Error",
                valEx.Message
            ),
            GamificationNotFoundException notFoundEx => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                notFoundEx.Message
            ),
            GamificationDomainException domainEx => (
                (int)HttpStatusCode.UnprocessableEntity,
                "Domain Rule Violation",
                domainEx.Message
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "An unexpected internal error occurred",
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
