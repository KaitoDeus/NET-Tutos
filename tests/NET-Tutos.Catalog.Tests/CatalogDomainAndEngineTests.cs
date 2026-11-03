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
        viOverview.Categories.Should().HaveCount(4);
        viOverview.Categories[0].Slug.Should().Be("dot-net-co-ban");

        enOverview.Language.Should().Be("en");
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
}
