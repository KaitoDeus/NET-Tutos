using MassTransit;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Gamification.Application.Common.Interfaces;
using NET_Tutos.Gamification.Application.Services;
using NET_Tutos.Gamification.Infrastructure.Consumers;
using NET_Tutos.Gamification.Infrastructure.Data;
using NET_Tutos.Gamification.Infrastructure.Services;

namespace NET_Tutos.Gamification.API.Extensions;

public static class GamificationServiceExtensions
{
    public static IServiceCollection AddGamificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Persistence
        var postgresConn = configuration.GetConnectionString("PostgreSqlConnection");
        services.AddDbContext<GamificationDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(postgresConn))
            {
                options.UseNpgsql(postgresConn);
            }
            else
            {
                options.UseInMemoryDatabase("GamificationDb");
            }
        });

        services.AddScoped<IGamificationDbContext>(sp => sp.GetRequiredService<GamificationDbContext>());
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // 2. Messaging with MassTransit & RabbitMQ
        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ChallengeSolvedConsumer>();
            x.AddConsumer<LessonCompletedConsumer>();
            x.AddConsumer<ExamPassedConsumer>();

            if (configuration.GetValue<bool>("UseInMemoryBus", false) || string.IsNullOrEmpty(configuration["RabbitMQ:Host"]))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        // 3. Application Services
        services.AddScoped<IGamificationService, GamificationService>();

        // 4. CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        return services;
    }
}
