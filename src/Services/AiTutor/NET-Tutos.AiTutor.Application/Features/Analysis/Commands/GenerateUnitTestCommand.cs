using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;

namespace NET_Tutos.AiTutor.Application.Features.Analysis.Commands;

public record GenerateUnitTestCommand(GenerateUnitTestRequestDto Request);

public class GenerateUnitTestCommandHandler
{
    private readonly IAiTutorEngine _engine;

    public GenerateUnitTestCommandHandler(IAiTutorEngine engine)
    {
        _engine = engine;
    }

    public async Task<GenerateUnitTestResponseDto> HandleAsync(GenerateUnitTestCommand command)
    {
        return await _engine.GenerateUnitTestAsync(command.Request);
    }
}
