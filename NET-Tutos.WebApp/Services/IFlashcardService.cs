using NET_Tutos.Models.Entities;
using NET_Tutos.Models.ViewModels;

namespace NET_Tutos.Services;

public interface IFlashcardService
{
    Task<InterviewIndexViewModel> GetIndexDataAsync(
        string? userId, 
        FlashcardTopic? topic = null, 
        DifficultyLevel? difficulty = null, 
        string? search = null, 
        int page = 1, 
        int pageSize = 12, 
        bool isEn = false);

    Task<StudySessionViewModel> GetStudySessionAsync(
        string? userId, 
        FlashcardTopic? topic = null, 
        DifficultyLevel? difficulty = null, 
        int limit = 15, 
        bool isEn = false);

    Task<FlashcardReviewResponse> RecordReviewAsync(
        string userId, 
        int flashcardId, 
        FlashcardRating rating, 
        bool isEn = false);

    Task<MockInterviewViewModel> CreateMockInterviewAsync(
        DifficultyLevel? difficulty = null, 
        int count = 5, 
        bool isEn = false);

    Task<MockInterviewResultViewModel> SubmitMockInterviewAsync(
        string userId, 
        SubmitMockInterviewRequest request, 
        bool isEn = false);

    Task<FlashcardItemViewModel?> GetFlashcardBySlugAsync(
        string slug, 
        string? userId = null, 
        bool isEn = false);
}
