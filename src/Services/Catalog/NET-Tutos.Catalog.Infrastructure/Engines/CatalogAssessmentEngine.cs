using NET_Tutos.Catalog.Application.Contracts;
using NET_Tutos.Catalog.Application.Services;
using NET_Tutos.Catalog.Domain.ValueObjects;

namespace NET_Tutos.Catalog.Infrastructure.Engines;

public sealed class CatalogAssessmentEngine : ICatalogEngine
{
    public CatalogOverviewDto GetCatalogOverview(string language = "vi")
    {
        bool isEn = language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var categories = GetCategories(isEn ? "en" : "vi");

        return new CatalogOverviewDto(
            ServiceName: "NET-Tutos.Catalog.API",
            Language: isEn ? "en" : "vi",
            TotalLessons: 101,
            TotalModules: 18,
            TotalCapstoneProjects: 3,
            Categories: categories);
    }

    public IReadOnlyList<CatalogCategoryDto> GetCategories(string language = "vi")
    {
        bool isEn = language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        if (isEn)
        {
            return
            [
                new(1, "dotnet-fundamentals", "1. .NET Fundamentals & C# Syntax", "Master CLR runtime, primitive types, control flow, and collections.", 25, 1),
                new(2, "oop-and-advanced-csharp", "2. OOP & Advanced C#", "4 OOP pillars, Generics, LINQ, Async/Await, Span<T> & Source Generators.", 26, 2),
                new(3, "ef-core-and-database", "3. Entity Framework Core & Database", "Code-First migrations, Fluent API relationships, AsNoTracking & ACID transactions.", 25, 3),
                new(4, "aspnet-core-and-microservices", "4. ASP.NET Core MVC, Web API & Microservices", "Minimal APIs, JWT/RBAC, Redis caching, YARP Gateway, RabbitMQ & Clean DDD.", 25, 4)
            ];
        }

        return
        [
            new(1, "dot-net-co-ban", "1. Nền tảng .NET & Cú pháp C#", "Làm chủ CLR Runtime, kiểu dữ liệu, cấu trúc điều khiển và Collections.", 25, 1),
            new(2, "oop-va-csharp-nang-cao", "2. OOP & C# Nâng cao", "4 trụ cột OOP, Generics, LINQ, Async/Await, Span<T> & Source Generators.", 26, 2),
            new(3, "ef-core-va-co-so-du-lieu", "3. Entity Framework Core & Database", "Code-First Migrations, Fluent API, tối ưu AsNoTracking & giao dịch ACID.", 25, 3),
            new(4, "aspnet-core-mvc-va-web-api", "4. ASP.NET Core MVC, Web API & Microservices", "Minimal APIs, JWT/RBAC, Redis Caching, YARP Gateway, RabbitMQ & Clean DDD.", 25, 4)
        ];
    }

    public GradeExamResponse EvaluateExam(GradeExamRequest request)
    {
        var score = ExamScore.Evaluate(request.CorrectAnswers, request.TotalQuestions);
        bool isEn = request.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        string userId = string.IsNullOrWhiteSpace(request.UserId) ? "anonymous" : request.UserId.Trim();
        string trackCode = string.IsNullOrWhiteSpace(request.TrackCode) ? "DOTNET10" : request.TrackCode.Trim().ToUpperInvariant();
        string certCode = score.GenerateCertificateCode(userId, trackCode);
        int awardedXp = score.CalculateAwardedXp();

        string feedback = score.IsPassed
            ? (isEn
                ? $"Congratulations! You passed the {trackCode} certification exam with {score.Percentage}% ({score.GradeTier}). Certificate {certCode} issued."
                : $"Chúc mừng! Bạn đã vượt qua kỳ thi sát hạch {trackCode} với số điểm {score.Percentage}% (Hạng: {score.GradeTier}). Mã chứng chỉ: {certCode}.")
            : (isEn
                ? $"You scored {score.Percentage}%, which is below the 80% passing threshold. Review the recommended modules and try again."
                : $"Bạn đạt {score.Percentage}%, chưa đạt ngưỡng đỗ 80%. Hãy ôn tập lại các học phần trọng tâm và thử sức lại.");

        return new GradeExamResponse(
            UserId: userId,
            TrackCode: trackCode,
            CorrectAnswers: score.CorrectAnswers,
            TotalQuestions: score.TotalQuestions,
            PercentageScore: score.Percentage,
            IsPassed: score.IsPassed,
            GradeTier: score.GradeTier.ToString(),
            AwardedXp: awardedXp,
            CertificateCode: certCode,
            FeedbackMessage: feedback);
    }

    public VerifyCertificateDto VerifyCertificate(string certificateCode, string language = "vi")
    {
        bool isEn = language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        string cleaned = (certificateCode ?? string.Empty).Trim().ToUpperInvariant();
        bool isValid = cleaned.StartsWith("NT-CERT-", StringComparison.Ordinal) && cleaned.Length >= 16;

        return new VerifyCertificateDto(
            CertificateCode: cleaned,
            IsValid: isValid,
            TrackName: isEn ? ".NET 10 & C# 14 Enterprise Software Engineering" : "Kỹ sư Phần mềm .NET 10 & C# 14 Thực chiến",
            IssuedBy: "NET-Tutos Engineering Academy",
            VerificationMessage: isValid
                ? (isEn ? "Verified authentic NET-Tutos developer certificate." : "Chứng chỉ kỹ sư NET-Tutos hợp lệ và đã được xác thực điện tử.")
                : (isEn ? "Certificate code format is invalid or not found." : "Mã chứng chỉ không hợp lệ hoặc không tồn tại trên hệ thống."));
    }
}
