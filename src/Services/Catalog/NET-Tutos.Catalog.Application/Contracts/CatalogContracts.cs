namespace NET_Tutos.Catalog.Application.Contracts;

public record CatalogCategoryDto(
    int Id,
    string Slug,
    string Name,
    string Description,
    int LessonCount,
    int OrderIndex);

public record CatalogSectionDto(
    int SectionNumber,
    string Title,
    string Description,
    string BadgeColor,
    string Icon,
    string DifficultyLevel,
    int LessonCount,
    int EstimatedHours);

public record LearningTrackEstimateDto(
    string TrackCode,
    int TotalHours,
    int DailyStudyMinutes,
    string PaceCategory,
    int EstimatedDays,
    int EstimatedWeeks,
    string Recommendation);

public record CatalogOverviewDto(
    string ServiceName,
    string Language,
    int TotalLessons,
    int TotalModules,
    int TotalCapstoneProjects,
    IReadOnlyList<CatalogCategoryDto> Categories);

public record GradeExamRequest(
    string UserId,
    string TrackCode,
    int CorrectAnswers,
    int TotalQuestions,
    string Language = "vi");

public record GradeExamResponse(
    string UserId,
    string TrackCode,
    int CorrectAnswers,
    int TotalQuestions,
    double PercentageScore,
    bool IsPassed,
    string GradeTier,
    int AwardedXp,
    string CertificateCode,
    string FeedbackMessage);

public record VerifyCertificateDto(
    string CertificateCode,
    bool IsValid,
    string TrackName,
    string IssuedBy,
    string VerificationMessage);

