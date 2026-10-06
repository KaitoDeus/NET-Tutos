using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IExamService
{
    Task<ExamListViewModel> GetExamListAsync(string? userId);
    Task<ExamSessionViewModel?> StartExamSessionAsync(string userId, int? categoryId);
    Task<ExamResultViewModel> EvaluateExamAsync(string userId, ExamSubmissionViewModel submission, string hostUrl);
    Task<ExamResultViewModel?> GetExamAttemptResultAsync(int attemptId, string userId);
}
