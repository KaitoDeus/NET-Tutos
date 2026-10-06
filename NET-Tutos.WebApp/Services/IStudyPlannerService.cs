using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IStudyPlannerService
{
    Task<StudyPlannerDashboardViewModel> GetDashboardAsync(string userId);
    Task<bool> CreatePlanAsync(string userId, CreateStudyPlanRequest request);
    Task<ToggleItemResponse> ToggleItemCompletionAsync(string userId, int itemId);
    Task<bool> ReschedulePlanAsync(string userId);
    Task<bool> ResetPlanAsync(string userId);
}
