using NET_Tutos.Catalog.Application.Contracts;

namespace NET_Tutos.Catalog.Application.Services;

public interface ICatalogEngine
{
    CatalogOverviewDto GetCatalogOverview(string language = "vi");
    IReadOnlyList<CatalogCategoryDto> GetCategories(string language = "vi");
    IReadOnlyList<CatalogSectionDto> GetSections(string language = "vi");
    CatalogSectionDto? GetSectionByNumber(int sectionNumber, string language = "vi");
    LearningTrackEstimateDto EstimateLearningTrack(string trackCode, int dailyStudyMinutes, string language = "vi");
    GradeExamResponse EvaluateExam(GradeExamRequest request);
    VerifyCertificateDto VerifyCertificate(string certificateCode, string language = "vi");
}

