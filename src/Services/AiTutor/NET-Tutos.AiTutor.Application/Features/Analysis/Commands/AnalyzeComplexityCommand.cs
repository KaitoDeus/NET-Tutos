using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;

namespace NET_Tutos.AiTutor.Application.Features.Analysis.Commands;

public record AnalyzeComplexityCommand(AnalyzeComplexityRequestDto Request);

public class AnalyzeComplexityCommandHandler
{
    private readonly IAiTutorEngine _engine;

    public AnalyzeComplexityCommandHandler(IAiTutorEngine engine)
    {
        _engine = engine;
    }

    public async Task<AnalyzeComplexityResponseDto> HandleAsync(AnalyzeComplexityCommand command)
    {
        return await _engine.AnalyzeComplexityAsync(command.Request);
    }
}
