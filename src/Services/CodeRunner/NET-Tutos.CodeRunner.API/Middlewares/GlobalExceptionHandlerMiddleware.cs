using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NET_Tutos.CodeRunner.Application.Common.Exceptions;
using NET_Tutos.CodeRunner.Domain.Exceptions;

namespace NET_Tutos.CodeRunner.API.Middlewares;

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
            _logger.LogError(ex, "Unhandled exception in CodeRunner: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            ForbiddenSyntaxException forbiddenEx => (
                (int)HttpStatusCode.Forbidden,
                "Security Policy Violation",
                forbiddenEx.Message
            ),
            ExecutionTimeoutException timeoutEx => (
                (int)HttpStatusCode.RequestTimeout,
                "Execution Timed Out",
                timeoutEx.Message
            ),
            CodeRunnerValidationException valEx => (
                (int)HttpStatusCode.BadRequest,
                "Validation Error",
                valEx.Message
            ),
            CodeRunnerDomainException domainEx => (
                (int)HttpStatusCode.UnprocessableEntity,
                "CodeRunner Domain Error",
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
