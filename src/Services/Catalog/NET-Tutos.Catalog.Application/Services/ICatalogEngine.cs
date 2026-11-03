using NET_Tutos.Catalog.Application.Contracts;

namespace NET_Tutos.Catalog.Application.Services;

public interface ICatalogEngine
{
    CatalogOverviewDto GetCatalogOverview(string language = "vi");
    IReadOnlyList<CatalogCategoryDto> GetCategories(string language = "vi");
    GradeExamResponse EvaluateExam(GradeExamRequest request);
    VerifyCertificateDto VerifyCertificate(string certificateCode, string language = "vi");
}
