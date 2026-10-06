using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface ICapstoneProjectService
{
    Task<ProjectListViewModel> GetProjectsListAsync(int? categoryId = null, DifficultyLevel? level = null, string? userId = null);
    Task<ProjectDetailsViewModel?> GetProjectDetailsAsync(string slug, string? userId = null);
    Task<CapstoneProject?> GetProjectByIdAsync(int id);
    Task<ProjectSubmission?> GetUserSubmissionAsync(int projectId, string userId);
    Task<MyProjectsViewModel> GetUserProjectsAsync(string userId);
    Task<(bool Success, string Message, int SubmissionId)> SubmitProjectAsync(string userId, SubmitProjectInputModel input);
    Task<AdminProjectSubmissionsViewModel> GetAdminSubmissionsAsync(ProjectSubmissionStatus? status = null);
    Task<ProjectSubmission?> GetSubmissionByIdAsync(int submissionId);
    Task<(bool Success, string Message)> ReviewSubmissionAsync(string reviewerId, AdminReviewInputModel input);
}
