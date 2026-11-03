using NET_Tutos.AiTutor.Application.Features.Analysis.Commands;
using NET_Tutos.AiTutor.Application.Features.Interview.Commands;
using NET_Tutos.AiTutor.Application.Services;
using NET_Tutos.AiTutor.Infrastructure.Engines;

namespace NET_Tutos.AiTutor.API.Extensions;

public static class AiTutorServiceExtensions
{
    public static IServiceCollection AddAiTutorServices(this IServiceCollection services)
    {
        services.AddSingleton<IAiTutorEngine, DeterministicAiTutorEngine>();

        // Register CQRS Command Handlers
        services.AddScoped<AnalyzeComplexityCommandHandler>();
        services.AddScoped<ExplainErrorCommandHandler>();
        services.AddScoped<GenerateUnitTestCommandHandler>();
        services.AddScoped<DiagnoseTestFailureCommandHandler>();
        services.AddScoped<EvaluateInterviewCommandHandler>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
