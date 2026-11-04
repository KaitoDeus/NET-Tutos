using FluentAssertions;
using NET_Tutos.Catalog.Application.Contracts;
using NET_Tutos.Catalog.Application.Features;
using NET_Tutos.Catalog.Domain.Enums;
using NET_Tutos.Catalog.Domain.Exceptions;
using NET_Tutos.Catalog.Domain.ValueObjects;
using NET_Tutos.Catalog.Infrastructure.Engines;
using Xunit;

namespace NET_Tutos.Catalog.Tests;

public class CatalogDomainAndEngineTests
{
    [Theory]
    [InlineData(5, "Quick Read")]
    [InlineData(12, "Standard Lesson")]
    [InlineData(25, "Deep-Dive Architecture")]
    public void ReadingDuration_ClassifiesPaceCategoryCorrectly(int minutes, string expectedCategory)
    {
        var duration = ReadingDuration.Create(minutes);
        duration.Minutes.Should().Be(minutes);
        duration.PaceCategory.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(300)]
    public void ReadingDuration_WhenOutOfRange_ThrowsInvalidReadingDurationException(int invalidMinutes)
    {
        Action act = () => ReadingDuration.Create(invalidMinutes);
        act.Should().Throw<InvalidReadingDurationException>();
    }

    [Theory]
    [InlineData(20, 20, 100.0, true, ExamGradeTier.Distinction, 300)]
    [InlineData(18, 20, 90.0, true, ExamGradeTier.Merit, 250)]
    [InlineData(16, 20, 80.0, true, ExamGradeTier.Pass, 200)]
    [InlineData(15, 20, 75.0, false, ExamGradeTier.Fail, 0)]
    public void ExamScore_EvaluatesPassingThresholdAndTierCorrectly(
        int correct,
        int total,
        double expectedPercent,
        bool expectedPassed,
        ExamGradeTier expectedTier,
        int expectedXp)
    {
        var score = ExamScore.Evaluate(correct, total);
        score.Percentage.Should().Be(expectedPercent);
        score.IsPassed.Should().Be(expectedPassed);
        score.GradeTier.Should().Be(expectedTier);
        score.CalculateAwardedXp().Should().Be(expectedXp);
    }

    [Fact]
    public async Task GetCatalogOverviewQueryHandler_ReturnsBilingualCurriculumAnd101Lessons()
    {
        var engine = new CatalogAssessmentEngine();
        var handler = new GetCatalogOverviewQueryHandler(engine);

        var viOverview = await handler.HandleAsync(new GetCatalogOverviewQuery("vi"));
        var enOverview = await handler.HandleAsync(new GetCatalogOverviewQuery("en"));

        viOverview.TotalLessons.Should().Be(101);
        viOverview.TotalModules.Should().Be(18);
        viOverview.Categories.Should().HaveCount(8);
        viOverview.Categories[0].Slug.Should().Be("dot-net-co-ban");

        enOverview.Language.Should().Be("en");
        enOverview.Categories.Should().HaveCount(8);
        enOverview.Categories[0].Slug.Should().Be("dotnet-fundamentals");
    }

    [Fact]
    public async Task EvaluateExamSubmissionCommandHandler_IssuesVerifiableCertificateWhenPassed()
    {
        var engine = new CatalogAssessmentEngine();
        var handler = new EvaluateExamSubmissionCommandHandler(engine);

        var result = await handler.HandleAsync(new EvaluateExamSubmissionCommand(
            new GradeExamRequest("user-123", "DOTNET10", 19, 20, "en")));

        result.IsPassed.Should().BeTrue();
        result.GradeTier.Should().Be("Distinction");
        result.AwardedXp.Should().Be(300);
        result.CertificateCode.Should().StartWith("NT-CERT-2026-");

        var verify = engine.VerifyCertificate(result.CertificateCode, "en");
        verify.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetCatalogSectionsQueryHandler_Returns7BilingualStagesWith101TotalLessons()
    {
        var engine = new CatalogAssessmentEngine();
        var handler = new GetCatalogSectionsQueryHandler(engine);

        var viSections = await handler.HandleAsync(new GetCatalogSectionsQuery("vi"));
        var enSections = await handler.HandleAsync(new GetCatalogSectionsQuery("en"));

        viSections.Should().HaveCount(7);
        viSections.Sum(s => s.LessonCount).Should().Be(101);
        viSections[0].Title.Should().Contain("Phần 1");

        enSections.Should().HaveCount(7);
        enSections.Sum(s => s.LessonCount).Should().Be(101);
        enSections[0].Title.Should().Contain("Part 1");

        var section6 = engine.GetSectionByNumber(6, "en");
        section6.Should().NotBeNull();
        section6!.DifficultyLevel.Should().Be("Advanced");
    }

    [Fact]
    public async Task EstimateLearningTrackQueryHandler_CalculatesCompletionDaysAndWeeks()
    {
        var engine = new CatalogAssessmentEngine();
        var handler = new EstimateLearningTrackQueryHandler(engine);

        var estimate = await handler.HandleAsync(new EstimateLearningTrackQuery("DOTNET10", 60, "en"));

        estimate.TrackCode.Should().Be("DOTNET10");
        estimate.TotalHours.Should().Be(129);
        estimate.DailyStudyMinutes.Should().Be(60);
        estimate.EstimatedDays.Should().Be(129);
        estimate.PaceCategory.Should().Be("Deep-Dive Architecture");
    }

    [Theory]
    [InlineData("NT-CERT-2026-ABCDEF12", true)]
    [InlineData("NET-2026-98AF12", true)]
    [InlineData("INVALID-CODE", false)]
    public void VerifyCertificate_RecognizesBothMicroserviceAndBffCertificateFormats(string code, bool expectedValid)
    {
        var engine = new CatalogAssessmentEngine();
        var result = engine.VerifyCertificate(code, "vi");
        result.IsValid.Should().Be(expectedValid);
    }
}

