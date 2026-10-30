using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface ICodeExecutionService
{
    Task<CodeExecutionResponse> ExecuteAsync(string code, int timeoutMs = 4000);
    Task<ChallengeSubmissionResponse> EvaluateChallengeAsync(CodingChallenge challenge, string userCode, int timeoutMs = 4000);
}
