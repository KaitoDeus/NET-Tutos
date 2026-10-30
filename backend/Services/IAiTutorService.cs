using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IAiTutorService
{
    Task<AiTutorChatResponse> ChatAsync(AiTutorChatRequest request, bool isEnglish);
    Task<AiExplainErrorResponse> ExplainErrorAsync(AiExplainErrorRequest request, bool isEnglish);
    Task<AiChallengeHintResponse> GetChallengeHintAsync(AiChallengeHintRequest request, bool isEnglish);
    Task<AiCodeReviewResponse> ReviewCodeAsync(AiCodeReviewRequest request, bool isEnglish);
    Task<AiComplexityResponse> AnalyzeComplexityAsync(AiComplexityRequest request, bool isEnglish);
    Task<AiDiagnoseTestFailureResponse> DiagnoseTestFailureAsync(AiDiagnoseTestFailureRequest request, bool isEnglish);
    Task<AiGenerateUnitTestResponse> GenerateUnitTestAsync(AiGenerateUnitTestRequest request, bool isEnglish);
    Task<AiInterviewEvaluationResponse> EvaluateInterviewAnswerAsync(AiInterviewEvaluationRequest request, bool isEnglish);
    Task<AiInterviewFollowUpResponse> AnswerFollowUpAsync(AiInterviewFollowUpRequest request, bool isEnglish);
    Task<AiInterviewReportResponse> GenerateInterviewReportAsync(AiInterviewReportRequest request, bool isEnglish);
}
