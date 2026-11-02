using FluentAssertions;
using NET_Tutos.Gamification.Domain.Exceptions;
using NET_Tutos.Gamification.Domain.ValueObjects;
using Xunit;

namespace NET_Tutos.Gamification.Tests;

public class GamificationDomainTests
{
    [Fact]
    public void XpValue_ValidPositive_CreatesSuccessfully()
    {
        var xp = new XpValue(100);
        xp.Value.Should().Be(100);
        ((int)xp).Should().Be(100);
    }

    [Fact]
    public void XpValue_Negative_ThrowsNegativeXpValueException()
    {
        var act = () => new XpValue(-50);
        act.Should().Throw<NegativeXpValueException>();
    }

    [Fact]
    public void XpValue_Addition_SumsValues()
    {
        var xp1 = new XpValue(50);
        var xp2 = new XpValue(150);
        var total = xp1 + xp2;
        total.Value.Should().Be(200);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(7, true)]
    [InlineData(14, true)]
    [InlineData(30, true)]
    [InlineData(5, false)]
    public void StreakCount_MilestoneEvaluation_MatchesExpected(int days, bool expectedMilestone)
    {
        var streak = new StreakCount(days);
        streak.IsMilestone().Should().Be(expectedMilestone);
    }

    [Fact]
    public void StreakCount_Increment_IncreasesDaysByOne()
    {
        var streak = new StreakCount(4);
        var incremented = streak.Increment();
        incremented.Days.Should().Be(5);
    }
}
