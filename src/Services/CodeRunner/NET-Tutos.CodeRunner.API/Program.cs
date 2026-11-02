using NET_Tutos.CodeRunner.API.Endpoints;
using NET_Tutos.CodeRunner.API.Extensions;
using NET_Tutos.CodeRunner.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddCodeRunnerServices();

var app = builder.Build();

// Global Exception Handler Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "CodeRunner.API", timestamp = DateTime.UtcNow }));

// Map Endpoint Modules
app.MapRunnerEndpoints();

app.Run();

public partial class Program { }
