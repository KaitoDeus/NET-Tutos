using NET_Tutos.CodeRunner.Application.Contracts;

namespace NET_Tutos.CodeRunner.Application.Services;

public interface ICodeRunnerEngine
{
    Task<ExecuteScriptResponse> ExecuteAsync(string code, int timeoutMs = 4000);
    Task<EvaluateChallengeResponse> EvaluateChallengeAsync(EvaluateChallengeRequest request);
}
