using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface ILearningProgressService
{
    Task<bool> ToggleLessonCompletedAsync(string userId, int tutorialId);
    Task<bool> IsLessonCompletedAsync(string userId, int tutorialId);
    Task<List<int>> GetCompletedLessonIdsAsync(string userId);
    Task<StudentProfileViewModel> GetStudentProfileAsync(string userId);
    Task RecordLessonAccessAsync(string userId, int tutorialId);
    Task<Tutorial?> GetLastAccessedTutorialAsync(string userId);
}
