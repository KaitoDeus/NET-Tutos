using NET_Tutos.CodeRunner.Application.Contracts;
using NET_Tutos.CodeRunner.Application.Services;

namespace NET_Tutos.CodeRunner.Application.Features.Execution.Commands;

public record ExecuteScriptCommand(ExecuteScriptRequest Request);

public class ExecuteScriptCommandHandler
{
    private readonly ICodeRunnerEngine _engine;

    public ExecuteScriptCommandHandler(ICodeRunnerEngine engine)
    {
        _engine = engine;
    }

    public async Task<ExecuteScriptResponse> HandleAsync(ExecuteScriptCommand command)
    {
        return await _engine.ExecuteAsync(command.Request.Code, command.Request.TimeoutMs);
    }
}
