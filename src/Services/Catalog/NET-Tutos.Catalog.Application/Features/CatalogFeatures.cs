using NET_Tutos.Catalog.Application.Contracts;
using NET_Tutos.Catalog.Application.Services;

namespace NET_Tutos.Catalog.Application.Features;

public record GetCatalogOverviewQuery(string Language = "vi");

public sealed class GetCatalogOverviewQueryHandler
{
    private readonly ICatalogEngine _engine;

    public GetCatalogOverviewQueryHandler(ICatalogEngine engine)
    {
        _engine = engine;
    }

    public Task<CatalogOverviewDto> HandleAsync(GetCatalogOverviewQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_engine.GetCatalogOverview(query.Language));
    }
}

public record EvaluateExamSubmissionCommand(GradeExamRequest Request);

public sealed class EvaluateExamSubmissionCommandHandler
{
    private readonly ICatalogEngine _engine;

    public EvaluateExamSubmissionCommandHandler(ICatalogEngine engine)
    {
        _engine = engine;
    }

    public Task<GradeExamResponse> HandleAsync(EvaluateExamSubmissionCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_engine.EvaluateExam(command.Request));
    }
}
