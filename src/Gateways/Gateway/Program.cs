using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.BuildingBlocks.Common.Telemetry;
using NET_Tutos.YarpGateway.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var gatewayTelemetry = new ServiceTelemetry("NET-Tutos.YarpGateway");
builder.Services.AddSingleton(gatewayTelemetry);
builder.Services.AddNetTutosOpenTelemetry(builder.Configuration, "NET-Tutos.YarpGateway", gatewayTelemetry);

// 1. Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Add Rate Limiting for Sandbox Execution
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("runner-policy", opt =>
    {
        opt.PermitLimit = 60; // Max 60 script executions per minute
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// 3. Add Unified CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 4. Middlewares Pipeline
app.UseCors("GatewayCorsPolicy");
app.UseMiddleware<CorrelationIdMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/metrics"))
    {
        await next();
        return;
    }

    var sw = Stopwatch.StartNew();
    using var activity = gatewayTelemetry.StartActivity($"YARP {context.Request.Method}", ActivityKind.Server);
    try
    {
        await next();
        sw.Stop();
        gatewayTelemetry.RecordOperation(context.Request.Method, sw.Elapsed.TotalMilliseconds, context.Response.StatusCode < 500);
    }
    catch
    {
        sw.Stop();
        gatewayTelemetry.RecordOperation(context.Request.Method, sw.Elapsed.TotalMilliseconds, isSuccess: false);
        throw;
    }
});
app.UseRateLimiter();

// 5. Gateway Health Endpoints
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "Healthy",
    gateway = "Yarp.API.Gateway",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
}))
.WithName("GatewayHealth")
.WithSummary("Gateway health check");

app.MapGet("/api/v1/gateway/status", (IConfiguration configuration) =>
{
    var clusters = configuration.GetSection("ReverseProxy:Clusters").GetChildren().Select(c => c.Key).ToList();
    var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren().Select(r => r.Key).ToList();

    return Results.Ok(ApiResponse<object>.Ok(new
    {
        service = "NET-Tutos YARP Gateway",
        status = "Active",
        registeredClusters = clusters,
        registeredRoutes = routes,
        timestamp = DateTime.UtcNow
    }));
})
.WithName("GatewayStatus")
.WithSummary("Returns gateway routing topology and registered clusters");

app.MapGet("/api/v1/gateway/telemetry", (ServiceTelemetry telemetry) =>
    Results.Ok(ApiResponse<ServiceTelemetrySnapshotDto>.Ok(telemetry.GetSnapshot())))
.WithName("GatewayTelemetry")
.WithSummary("Returns OpenTelemetry ActivitySource and Meter metrics for the YARP API Gateway");

app.MapGet("/api/v1/gateway/otlp", (OtlpCollectorDiagnosticsDto otlp) =>
    Results.Ok(ApiResponse<OtlpCollectorDiagnosticsDto>.Ok(otlp)))
.WithName("GatewayOtlpDiagnostics")
.WithSummary("Returns OTLP collector export status for the YARP API Gateway");

app.MapGet("/metrics", (ServiceTelemetry telemetry) =>
    Results.Text(telemetry.ToPrometheusMetrics(), PrometheusMetricsBuilder.ContentType))
.WithName("GatewayPrometheusMetrics")
.WithSummary("Exports Prometheus Exposition Format metrics for the YARP API Gateway");

// 6. Map Reverse Proxy Routes
app.MapReverseProxy();

app.Run();

public partial class Program { }
