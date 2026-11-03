using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;

namespace NET_Tutos.AiTutor.Application.Features.Analysis.Commands;

public record ExplainErrorCommand(ExplainErrorRequestDto Request);

public class ExplainErrorCommandHandler
{
    private readonly IAiTutorEngine _engine;

    public ExplainErrorCommandHandler(IAiTutorEngine engine)
    {
        _engine = engine;
    }

    public async Task<ExplainErrorResponseDto> HandleAsync(ExplainErrorCommand command)
    {
        return await _engine.ExplainErrorAsync(command.Request);
    }
}
