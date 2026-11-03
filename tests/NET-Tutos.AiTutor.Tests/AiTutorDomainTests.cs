using FluentAssertions;
using NET_Tutos.AiTutor.Domain.Enums;
using NET_Tutos.AiTutor.Domain.Exceptions;
using NET_Tutos.AiTutor.Domain.Rules;
using NET_Tutos.AiTutor.Domain.ValueObjects;
using Xunit;

namespace NET_Tutos.AiTutor.Tests;

public class AiTutorDomainTests
{
    [Theory]
    [InlineData(-10, 1, false)]
    [InlineData(70, 70, false)]
    [InlineData(92, 92, true)]
    [InlineData(150, 100, true)]
    public void EfficiencyScore_ClampsToValidBoundaries_AndIdentifiesOptimal(int rawScore, int expectedScore, bool expectedOptimal)
    {
        var score = new EfficiencyScore(rawScore);

        score.Value.Should().Be(expectedScore);
        score.IsOptimal.Should().Be(expectedOptimal);
        ((int)score).Should().Be(expectedScore);
    }

    [Fact]
    public void InterviewRubricScore_ValidScores_CalculatesWeightedAverageAndTier()
    {
        // 90 * 0.5 + 80 * 0.25 + 88 * 0.25 = 45 + 20 + 22 = 87 (SeniorReady)
        var rubric = new InterviewRubricScore(90, 80, 88);

        rubric.WeightedOverall.Should().Be(87);
        rubric.ReadinessTier.Should().Be(InterviewReadinessTier.SeniorReady);
        rubric.GetPerformanceRating(true).Should().Contain("Senior");
    }

    [Theory]
    [InlineData(-5, 80, 80)]
    [InlineData(80, 105, 80)]
    [InlineData(80, 80, -1)]
    public void InterviewRubricScore_OutOfBounds_ThrowsInvalidRubricScoreException(int tech, int clarity, int depth)
    {
        var act = () => new InterviewRubricScore(tech, clarity, depth);

        act.Should().Throw<InvalidRubricScoreException>();
    }

    [Fact]
    public void AiAnalysisRules_DetectsQuadraticNestedLoop()
    {
        var code = @"
for (int i = 0; i < n; i++) {
    for (int j = 0; j < n; j++) {
        sum += i * j;
    }
}";
        var tier = AiAnalysisRules.DetectTimeComplexity(code);

        tier.Should().Be(ComplexityTier.Quadratic);
        AiAnalysisRules.FormatBigONotation(tier).Should().Be("O(N²)");
    }

    [Fact]
    public void AiAnalysisRules_DetectsLinearSpaceAllocation()
    {
        var code = "var lookup = new Dictionary<int, int>();";

        AiAnalysisRules.HasLinearSpaceAllocation(code).Should().BeTrue();
    }
}
