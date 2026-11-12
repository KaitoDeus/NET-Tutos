using FluentAssertions;
using NET_Tutos.Services;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class SqlPlaygroundServiceTests
{
    private readonly SqlPlaygroundService _service = new();

    [Fact]
    public void GetSchemaOverviewAndChallenges_ReturnsRelationalTablesAndChallenges()
    {
        var schema = _service.GetSchemaOverview();
        var challenges = _service.GetChallenges();

        schema.Should().HaveCount(4);
        schema.Select(s => s.TableName).Should().Contain(["Students", "Tracks", "Enrollments", "Submissions"]);
        challenges.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ValidSelectAndChallengeVerification_PassesCleanly()
    {
        var challenge = _service.GetChallenges().First(c => c.Id == 1);

        var result = await _service.ExecuteQueryAsync(challenge.StarterQuery, challengeId: 1, isEnglish: true);

        result.IsSuccess.Should().BeTrue();
        result.Columns.Should().Contain(["FullName", "Country", "XpPoints"]);
        result.RowCount.Should().Be(3);
        result.ChallengePassed.Should().BeTrue();
        result.ChallengeMessage.Should().Contain("passed");
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithCteChallenge_EvaluatesExpectedMatrix()
    {
        var cteChallenge = _service.GetChallenges().First(c => c.Id == 5);

        var result = await _service.ExecuteQueryAsync(cteChallenge.StarterQuery, challengeId: 5, isEnglish: true);

        result.IsSuccess.Should().BeTrue();
        result.RowCount.Should().BeGreaterThan(0);
        result.ChallengePassed.Should().BeTrue();
    }

    [Theory]
    [InlineData("DROP TABLE Students;")]
    [InlineData("DELETE FROM Students WHERE Id = 1")]
    [InlineData("SELECT * FROM Students; DROP TABLE Students;")]
    [InlineData("PRAGMA table_info(Students)")]
    [InlineData("ATTACH DATABASE 'evil.db' AS evil")]
    public async Task ExecuteQueryAsync_DangerousOrMultiStatementSql_IsBlockedBySandbox(string unsafeQuery)
    {
        var result = await _service.ExecuteQueryAsync(unsafeQuery, isEnglish: true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
