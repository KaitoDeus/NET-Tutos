using NET_Tutos.AiTutor.Application.Contracts;
using NET_Tutos.AiTutor.Application.Services;

namespace NET_Tutos.AiTutor.Application.Features.Interview.Commands;

public record EvaluateInterviewCommand(EvaluateInterviewRequestDto Request);

public class EvaluateInterviewCommandHandler
{
    private readonly IAiTutorEngine _engine;

    public EvaluateInterviewCommandHandler(IAiTutorEngine engine)
    {
        _engine = engine;
    }

    public async Task<EvaluateInterviewResponseDto> HandleAsync(EvaluateInterviewCommand command)
    {
        return await _engine.EvaluateInterviewAnswerAsync(command.Request);
    }
}
