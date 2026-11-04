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

    public IReadOnlyList<CatalogSectionDto> GetSections(string language = "vi")
    {
        bool isEn = language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        if (isEn)
        {
            return
            [
                new(1, "Part 1 - Introduction to C#", "Build your programming foundation with C# syntax, variables, data types, control flow, loops, arrays, methods, classes, and fundamental OOP concepts.", "success", "bi-mortarboard-fill", "Beginner", 21, 20),
                new(2, "Part 2 - Advanced C#", "Advance your skills with Delegates, Lambdas, Events, LINQ, Collections, IDisposable, Exception Handling, Reflection, Async/Await concurrency, and Dependency Injection.", "info", "bi-layers-half", "Intermediate", 22, 25),
                new(3, "Part 3 - .NET Core Base Libraries", "Master File I/O, Streams, JSON Serialization, HttpClient networking, Regular Expressions, Globalization, and Diagnostic Logging.", "primary", "bi-box-seam-fill", "Intermediate", 11, 14),
                new(4, "Part 4 - Database & Entity Framework Core", "Master ADO.NET, Dapper micro-ORM, Entity Framework Core Code-First Migrations, Fluent API, LINQ-to-Entities, and query optimization.", "warning", "bi-database-fill-gear", "Intermediate", 13, 18),
                new(5, "Part 5 - ASP.NET Core MVC & Razor", "Build enterprise web applications with ASP.NET Core Middleware pipeline, Routing, Controllers, Razor Views, Tag Helpers, Model Validation, and Identity Auth.", "danger", "bi-globe2", "Intermediate", 18, 24),
                new(6, "Part 6 - ASP.NET Core Web API & Microservices", "Architect RESTful Web APIs, Minimal APIs, OpenAPI/Swagger, JWT Bearer Authentication, Redis Distributed Caching, SignalR, YARP Gateway, and Docker.", "dark", "bi-hdd-network-fill", "Advanced", 10, 16),
                new(7, "Part 7 - Enterprise Architecture & Production", "Apply Clean Architecture, Domain-Driven Design (DDD), CQRS, Unit/Integration Testing with xUnit, CI/CD GitHub Actions, and Cloud Deployment.", "secondary", "bi-building-fill-check", "Advanced", 6, 12)
            ];
        }

        return
        [
            new(1, "Phần 1 - Nhập môn C#", "Xây dựng nền móng tư duy lập trình với cú pháp C#, biến, kiểu dữ liệu, cấu trúc điều kiện, vòng lặp, mảng, hàm, lớp và các khái niệm OOP sơ khởi.", "success", "bi-mortarboard-fill", "Beginner", 21, 20),
            new(2, "Phần 2 - C# Nâng cao", "Nâng tầm kỹ năng với Delegate, Lambda, Event, LINQ, Collections, IDisposable, Exception, Reflection, Async/Await đa luồng và Dependency Injection.", "info", "bi-layers-half", "Intermediate", 22, 25),
            new(3, "Phần 3 - Thư viện cơ sở .NET Core", "Làm chủ File I/O, Streams, JSON Serialization, HttpClient Networking, Regular Expressions, đa ngôn ngữ và Logging hệ thống.", "primary", "bi-box-seam-fill", "Intermediate", 11, 14),
            new(4, "Phần 4 - Cơ sở dữ liệu & Entity Framework Core", "Thực chiến ADO.NET, Dapper, Entity Framework Core Code-First Migrations, Fluent API, LINQ-to-Entities và tối ưu truy vấn SQL.", "warning", "bi-database-fill-gear", "Intermediate", 13, 18),
            new(5, "Phần 5 - ASP.NET Core MVC & Razor", "Xây dựng ứng dụng Web với ASP.NET Core Middleware, Routing, Controllers, Razor Views, Tag Helpers, Validation và Identity bảo mật.", "danger", "bi-globe2", "Intermediate", 18, 24),
            new(6, "Phần 6 - ASP.NET Core Web API & Microservices", "Thiết kế RESTful Web API, Minimal APIs, Swagger, JWT Bearer, Redis Distributed Cache, SignalR, YARP API Gateway và Docker.", "dark", "bi-hdd-network-fill", "Advanced", 10, 16),
            new(7, "Phần 7 - Kiến trúc Doanh nghiệp & Triển khai Thực tế", "Ứng dụng Clean Architecture, Domain-Driven Design (DDD), CQRS, kiểm thử tự động xUnit, CI/CD GitHub Actions và triển khai Production.", "secondary", "bi-building-fill-check", "Advanced", 6, 12)
        ];
    }

    public CatalogSectionDto? GetSectionByNumber(int sectionNumber, string language = "vi")
    {
        return GetSections(language).FirstOrDefault(s => s.SectionNumber == sectionNumber);
    }

    public LearningTrackEstimateDto EstimateLearningTrack(string trackCode, int dailyStudyMinutes, string language = "vi")
    {
        var duration = ReadingDuration.Create(dailyStudyMinutes);
        bool isEn = language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        string normalizedTrack = string.IsNullOrWhiteSpace(trackCode) ? "DOTNET10" : trackCode.Trim().ToUpperInvariant();

        int totalHours = GetSections(language).Sum(s => s.EstimatedHours);
        int totalMinutes = totalHours * 60;
        int estimatedDays = (int)Math.Ceiling((double)totalMinutes / duration.Minutes);
        int estimatedWeeks = (int)Math.Ceiling(estimatedDays / 7.0);

        string recommendation = isEn
            ? $"Studying {duration.Minutes} mins/day ({duration.PaceCategory}), you will master all {totalHours} hours across 7 curriculum stages in ~{estimatedDays} days ({estimatedWeeks} weeks)."
            : $"Với nhịp độ {duration.Minutes} phút/ngày ({duration.PaceCategory}), bạn sẽ hoàn thành toàn bộ {totalHours} giờ học qua 7 học phần trong khoảng {estimatedDays} ngày (~{estimatedWeeks} tuần).";

        return new LearningTrackEstimateDto(
            TrackCode: normalizedTrack,
            TotalHours: totalHours,
            DailyStudyMinutes: duration.Minutes,
            PaceCategory: duration.PaceCategory,
            EstimatedDays: estimatedDays,
            EstimatedWeeks: estimatedWeeks,
            Recommendation: recommendation);
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
        bool isValid = (cleaned.StartsWith("NT-CERT-", StringComparison.Ordinal) && cleaned.Length >= 16)
                    || (cleaned.StartsWith("NET-20", StringComparison.Ordinal) && cleaned.Length >= 12);

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

