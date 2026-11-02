using NET_Tutos.CodeRunner.Application.Contracts;
using NET_Tutos.CodeRunner.Application.Services;

namespace NET_Tutos.CodeRunner.Application.Features.Execution.Commands;

public record EvaluateChallengeCommand(EvaluateChallengeRequest Request);

public class EvaluateChallengeCommandHandler
{
    private readonly ICodeRunnerEngine _engine;

    public EvaluateChallengeCommandHandler(ICodeRunnerEngine engine)
    {
        _engine = engine;
    }

    public async Task<EvaluateChallengeResponse> HandleAsync(EvaluateChallengeCommand command)
    {
        return await _engine.EvaluateChallengeAsync(command.Request);
    }
}
