using FluentAssertions;
using Moq;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class ExamServiceTests
{
    private readonly Mock<ICertificateService> _mockCertificateService = new();

    [Fact]
    public async Task GetExamListAsync_ReturnsCategoryExamsWithConfiguredPassingThreshold()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        context.Categories.AddRange(
            new Category { Id = 1, Name = "C# & .NET Cơ bản", Slug = "csharp-basic", OrderIndex = 1, BadgeColor = "primary", IconClass = "bi-code" },
            new Category { Id = 2, Name = "OOP & C# Nâng cao", Slug = "oop-advanced", OrderIndex = 2, BadgeColor = "success", IconClass = "bi-boxes" }
        );
        await context.SaveChangesAsync();

        var service = new ExamService(context, _mockCertificateService.Object, mockUserManager.Object);

        // Act
        var result = await service.GetExamListAsync(userId: null);

        // Assert
        result.Should().NotBeNull();
        result.AvailableExams.Should().HaveCount(3); // 2 category exams + 1 comprehensive graduation exam
        result.AvailableExams.Where(e => e.CategoryId != null).Should().AllSatisfy(t =>
        {
            t.PassingScorePercent.Should().Be(80);
            t.DurationMinutes.Should().Be(15);
            t.QuestionCount.Should().BeInRange(5, 10);
        });
        result.AvailableExams.Should().ContainSingle(e => e.CategoryId == null); // Comprehensive exam
    }

    [Fact]
    public async Task GetExamListAsync_WithUserPassedAttempt_MarksHasPassedAsTrue()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var mockUserManager = MockHelpers.CreateMockUserManager();

        var userId = "student-1";
        context.Categories.Add(new Category { Id = 1, Name = "Entity Framework Core", Slug = "ef-core", OrderIndex = 1 });
        context.QuizAttempts.Add(new QuizAttempt
        {
            UserId = userId,
            CategoryId = 1,
            ExamTitle = "Kỳ thi: Entity Framework Core",
            ScorePercentage = 90,
            IsPassed = true,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ExamService(context, _mockCertificateService.Object, mockUserManager.Object);

        // Act
        var result = await service.GetExamListAsync(userId);

        // Assert
        result.AvailableExams.Should().ContainSingle(t => t.CategoryId == 1);
        var topic = result.AvailableExams.First(t => t.CategoryId == 1);
        topic.HasPassed.Should().BeTrue();
        topic.BestScore.Should().Be(90);
    }
}
