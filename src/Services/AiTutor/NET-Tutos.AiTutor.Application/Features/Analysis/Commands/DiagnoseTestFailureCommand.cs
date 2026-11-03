using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;

namespace NET_Tutos.AiTutor.Application.Features.Analysis.Commands;

public record DiagnoseTestFailureCommand(DiagnoseTestFailureRequestDto Request);

public class DiagnoseTestFailureCommandHandler
{
    private readonly IAiTutorEngine _engine;

    public DiagnoseTestFailureCommandHandler(IAiTutorEngine engine)
    {
        _engine = engine;
    }

    public async Task<DiagnoseTestFailureResponseDto> HandleAsync(DiagnoseTestFailureCommand command)
    {
        return await _engine.DiagnoseTestFailureAsync(command.Request);
    }
}
