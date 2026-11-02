using NET_Tutos.CodeRunner.Application.Services;
using NET_Tutos.CodeRunner.Infrastructure.Engines;

namespace NET_Tutos.CodeRunner.API.Extensions;

public static class CodeRunnerServiceExtensions
{
    public static IServiceCollection AddCodeRunnerServices(this IServiceCollection services)
    {
        services.AddSingleton<ICodeRunnerEngine, RoslynCodeRunnerEngine>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        return services;
    }
}
