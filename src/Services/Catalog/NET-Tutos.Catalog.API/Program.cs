using NET_Tutos.BuildingBlocks.Common.Models;
using NET_Tutos.Catalog.Application.Contracts;
using NET_Tutos.Catalog.Application.Features;
using NET_Tutos.Catalog.Application.Services;
using NET_Tutos.Catalog.Domain.Exceptions;
using NET_Tutos.Catalog.Infrastructure.Engines;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICatalogEngine, CatalogAssessmentEngine>();
builder.Services.AddScoped<GetCatalogOverviewQueryHandler>();
builder.Services.AddScoped<EvaluateExamSubmissionCommandHandler>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (CatalogDomainException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Catalog Domain Validation Error",
            status = 400,
            code = ex.Code,
            detail = ex.Message
        });
    }
});

app.MapGet("/healthz", () => Results.Ok(new
{
    service = "NET-Tutos.Catalog.API",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

var group = app.MapGroup("/api/v1/catalog");

group.MapGet("/overview", async (string? lang, GetCatalogOverviewQueryHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(new GetCatalogOverviewQuery(lang ?? "vi"), ct);
    return Results.Ok(ApiResponse<CatalogOverviewDto>.Ok(result));
});

group.MapGet("/categories", (string? lang, ICatalogEngine engine) =>
{
    var categories = engine.GetCategories(lang ?? "vi");
    return Results.Ok(ApiResponse<IReadOnlyList<CatalogCategoryDto>>.Ok(categories));
});

group.MapPost("/assessment/grade-exam", async (GradeExamRequest request, EvaluateExamSubmissionCommandHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(new EvaluateExamSubmissionCommand(request), ct);
    return Results.Ok(ApiResponse<GradeExamResponse>.Ok(result));
});

group.MapGet("/certificates/verify/{code}", (string code, string? lang, ICatalogEngine engine) =>
{
    var verification = engine.VerifyCertificate(code, lang ?? "vi");
    return Results.Ok(ApiResponse<VerifyCertificateDto>.Ok(verification));
});

app.Run();

public partial class Program { }
