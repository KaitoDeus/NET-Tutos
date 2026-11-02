using NET_Tutos.CodeRunner.API.Contracts;

namespace NET_Tutos.CodeRunner.API.Services;

public interface ICodeRunnerEngine
{
    Task<ExecuteScriptResponse> ExecuteAsync(string code, int timeoutMs = 4000);
    Task<EvaluateChallengeResponse> EvaluateChallengeAsync(EvaluateChallengeRequest request);
}
