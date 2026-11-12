using System.Diagnostics;
using NET_Tutos.AiTutor.API.Endpoints;
using NET_Tutos.AiTutor.API.Extensions;
using NET_Tutos.AiTutor.API.Middlewares;
using NET_Tutos.BuildingBlocks.Common.Telemetry;

var builder = WebApplication.CreateBuilder(args);

var serviceTelemetry = new ServiceTelemetry("NET-Tutos.AiTutor.API");
builder.Services.AddSingleton(serviceTelemetry);
builder.Services.AddNetTutosOpenTelemetry(builder.Configuration, "NET-Tutos.AiTutor.API", serviceTelemetry);

// Add Services
builder.Services.AddAiTutorServices();

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
    using var activity = serviceTelemetry.StartActivity($"AiTutor {context.Request.Method}", ActivityKind.Server);
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
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "Healthy",
    service = "AiTutor.API",
    timestamp = DateTime.UtcNow
}));
app.MapGet("/metrics", (ServiceTelemetry telemetry) => Results.Text(telemetry.ToPrometheusMetrics(), PrometheusMetricsBuilder.ContentType));

// Map Endpoint Modules
app.MapAiAnalysisEndpoints();
app.MapAiInterviewEndpoints();

app.Run();

public partial class Program { }
