using System.Diagnostics;
using NET_Tutos.BuildingBlocks.Common.Telemetry;
using NET_Tutos.CodeRunner.API.Endpoints;
using NET_Tutos.CodeRunner.API.Extensions;
using NET_Tutos.CodeRunner.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var serviceTelemetry = new ServiceTelemetry("NET-Tutos.CodeRunner.API");
builder.Services.AddSingleton(serviceTelemetry);

// Add Services
builder.Services.AddCodeRunnerServices();

var app = builder.Build();

// Global Exception Handler Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/metrics"))
    {
        await next();
        return;
    }

    var sw = Stopwatch.StartNew();
    using var activity = serviceTelemetry.StartActivity($"CodeRunner {context.Request.Method}", ActivityKind.Server);
    try
    {
        await next();
        sw.Stop();
        serviceTelemetry.RecordOperation($"{context.Request.Method} {context.Request.Path}", sw.Elapsed.TotalMilliseconds, context.Response.StatusCode < 500);
    }
    catch
    {
        sw.Stop();
        serviceTelemetry.RecordOperation($"{context.Request.Method} {context.Request.Path}", sw.Elapsed.TotalMilliseconds, isSuccess: false);
        throw;
    }
});

// Health check & Prometheus metrics endpoints
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "CodeRunner.API", timestamp = DateTime.UtcNow }));
app.MapGet("/metrics", (ServiceTelemetry telemetry) => Results.Text(telemetry.ToPrometheusMetrics(), PrometheusMetricsBuilder.ContentType));

// Map Endpoint Modules
app.MapRunnerEndpoints();

app.Run();

public partial class Program { }
