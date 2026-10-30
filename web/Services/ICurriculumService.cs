using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface ICurriculumService
{
    Task<CurriculumIndexViewModel> GetCurriculumOverviewAsync(int? sectionNumber, DifficultyLevel? difficulty, string? search);
    Task<List<CurriculumSectionViewModel>> GetAllSectionsAsync();
    Task<CurriculumLessonViewModel?> GetLessonByNumberAsync(int lessonNumber);
    Task<CurriculumLessonViewModel?> GetLessonBySlugAsync(string slug);
    Task<CurriculumLessonDetailViewModel?> GetLessonDetailAsync(int lessonNumber);
    Task<List<CurriculumLessonViewModel>> GetAllLessonsAsync();
}
