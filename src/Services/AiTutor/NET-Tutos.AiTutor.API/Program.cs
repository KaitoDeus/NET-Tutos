using NET_Tutos.AiTutor.API.Endpoints;
using NET_Tutos.AiTutor.API.Extensions;
using NET_Tutos.AiTutor.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddAiTutorServices();

var app = builder.Build();

// Global Exception Handler Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "Healthy",
    service = "AiTutor.API",
    timestamp = DateTime.UtcNow
}));

// Map Endpoint Modules
app.MapAiAnalysisEndpoints();
app.MapAiInterviewEndpoints();

app.Run();

public partial class Program { }
