using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IAiTutorService
{
    Task<AiTutorChatResponse> ChatAsync(AiTutorChatRequest request, bool isEnglish);
    Task<AiExplainErrorResponse> ExplainErrorAsync(AiExplainErrorRequest request, bool isEnglish);
    Task<AiChallengeHintResponse> GetChallengeHintAsync(AiChallengeHintRequest request, bool isEnglish);
    Task<AiCodeReviewResponse> ReviewCodeAsync(AiCodeReviewRequest request, bool isEnglish);
}
