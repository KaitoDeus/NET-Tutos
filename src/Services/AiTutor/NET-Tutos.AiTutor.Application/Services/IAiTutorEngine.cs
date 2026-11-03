using NET_Tutos.AiTutor.Application.Contracts;

namespace NET_Tutos.AiTutor.Application.Services;

public interface IAiTutorEngine
{
    Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request);
    Task<ExplainErrorResponseDto> ExplainErrorAsync(ExplainErrorRequestDto request);
    Task<ChallengeHintResponseDto> GetChallengeHintAsync(ChallengeHintRequestDto request);
    Task<CodeReviewResponseDto> ReviewCodeAsync(CodeReviewRequestDto request);
    Task<AnalyzeComplexityResponseDto> AnalyzeComplexityAsync(AnalyzeComplexityRequestDto request);
    Task<DiagnoseTestFailureResponseDto> DiagnoseTestFailureAsync(DiagnoseTestFailureRequestDto request);
    Task<GenerateUnitTestResponseDto> GenerateUnitTestAsync(GenerateUnitTestRequestDto request);
    Task<EvaluateInterviewResponseDto> EvaluateInterviewAnswerAsync(EvaluateInterviewRequestDto request);
    Task<InterviewFollowUpResponseDto> AnswerFollowUpAsync(InterviewFollowUpRequestDto request);
    Task<InterviewReportResponseDto> GenerateInterviewReportAsync(InterviewReportRequestDto request);
    Task<AiTutorStatusResponseDto> CheckStatusAsync();
}
