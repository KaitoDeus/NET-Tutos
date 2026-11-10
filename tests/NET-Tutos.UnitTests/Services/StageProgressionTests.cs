using FluentAssertions;
using Moq;
using NET_Tutos.Models.Entities;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class StageProgressionTests
{
    private static LearningProgressService CreateService(Data.AppDbContext db)
    {
        var streakMock = new Mock<IStreakService>();
        var notifMock = new Mock<INotificationService>();
        var feedMock = new Mock<IActivityFeedService>();
        return new LearningProgressService(db, streakMock.Object, notifMock.Object, feedMock.Object);
    }

    [Fact]
    public async Task StageProgression_UnlocksStage1OnlyInitially_AndUnlocksStage2WhenStage1Is100PercentCompleted()
    {
        // Arrange
        using var db = MockHelpers.CreateInMemoryDbContext();
        var user = new ApplicationUser { Id = "learner-1", UserName = "learner1", ExperiencePoints = 0 };
        db.Users.Add(user);

        var cat1 = new Category { Id = 1, OrderIndex = 1, Slug = "csharp-co-ban", Name = "1. C# Basics" };
        var cat2 = new Category { Id = 2, OrderIndex = 2, Slug = "oop-csharp-nang-cao", Name = "2. OOP & Async" };
        var cat3 = new Category { Id = 3, OrderIndex = 3, Slug = "csharp-dsa-leetcode", Name = "3. DSA & LeetCode" };
        db.Categories.AddRange(cat1, cat2, cat3);

        var s1t1 = new Tutorial { Id = 101, CategoryId = 1, OrderIndex = 1, Slug = "s1-lesson-1", Title = "Stage 1 L1" };
        var s1t2 = new Tutorial { Id = 102, CategoryId = 1, OrderIndex = 2, Slug = "s1-lesson-2", Title = "Stage 1 L2" };
        var s2t1 = new Tutorial { Id = 201, CategoryId = 2, OrderIndex = 1, Slug = "s2-lesson-1", Title = "Stage 2 L1" };
        var s3t1 = new Tutorial { Id = 301, CategoryId = 3, OrderIndex = 1, Slug = "s3-lesson-1", Title = "Stage 3 L1" };
        db.Tutorials.AddRange(s1t1, s1t2, s2t1, s3t1);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act 1: Initial state
        var initial = await service.GetStageProgressionAsync(user.Id);

        // Assert 1: Stage 1 unlocked, Stage 2 & 3 locked
        initial.StagesByCategoryId[1].IsUnlocked.Should().BeTrue();
        initial.StagesByCategoryId[2].IsUnlocked.Should().BeFalse();
        initial.StagesByCategoryId[2].BlockingCategoryId.Should().Be(1);
        initial.StagesByCategoryId[2].BlockingRemainingLessons.Should().Be(2);
        initial.StagesByCategoryId[2].NextPrerequisiteTutorialSlug.Should().Be("s1-lesson-1");
        initial.StagesByCategoryId[3].IsUnlocked.Should().BeFalse();

        // Attempting to complete Stage 2 while Stage 1 incomplete must be blocked
        var blockedToggle = await service.ToggleLessonCompletedAsync(user.Id, s2t1.Id);
        blockedToggle.Should().BeFalse();

        // Act 2: Complete 1 of 2 lessons in Stage 1
        (await service.ToggleLessonCompletedAsync(user.Id, s1t1.Id)).Should().BeTrue();
        var mid = await service.GetStageProgressionAsync(user.Id);
        mid.StagesByCategoryId[2].IsUnlocked.Should().BeFalse();
        mid.StagesByCategoryId[2].BlockingRemainingLessons.Should().Be(1);
        mid.StagesByCategoryId[2].NextPrerequisiteTutorialSlug.Should().Be("s1-lesson-2");

        // Act 3: Complete final lesson of Stage 1 -> Stage 2 unlocks!
        (await service.ToggleLessonCompletedAsync(user.Id, s1t2.Id)).Should().BeTrue();
        var afterStage1 = await service.GetStageProgressionAsync(user.Id);

        // Assert 3
        afterStage1.StagesByCategoryId[1].IsCompleted.Should().BeTrue();
        afterStage1.StagesByCategoryId[2].IsUnlocked.Should().BeTrue();
        afterStage1.StagesByCategoryId[2].BlockingCategoryId.Should().BeNull();
        afterStage1.StagesByCategoryId[3].IsUnlocked.Should().BeFalse();
        afterStage1.StagesByCategoryId[3].BlockingCategoryId.Should().Be(2);

        // Completing Stage 2 lesson now succeeds
        (await service.ToggleLessonCompletedAsync(user.Id, s2t1.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task StageProgression_BypassLocks_UnlocksAllStagesForAdmins()
    {
        using var db = MockHelpers.CreateInMemoryDbContext();
        var cat1 = new Category { Id = 1, OrderIndex = 1, Slug = "csharp-co-ban", Name = "1. C# Basics" };
        var cat2 = new Category { Id = 2, OrderIndex = 2, Slug = "oop-csharp-nang-cao", Name = "2. OOP" };
        db.Categories.AddRange(cat1, cat2);
        db.Tutorials.Add(new Tutorial { Id = 101, CategoryId = 1, OrderIndex = 1, Slug = "s1-1", Title = "S1" });
        db.Tutorials.Add(new Tutorial { Id = 201, CategoryId = 2, OrderIndex = 1, Slug = "s2-1", Title = "S2" });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var adminProg = await service.GetStageProgressionAsync("admin-1", bypassLocks: true);

        adminProg.StagesByCategoryId[1].IsUnlocked.Should().BeTrue();
        adminProg.StagesByCategoryId[2].IsUnlocked.Should().BeTrue();
        adminProg.StagesByCategoryId[2].BlockingCategoryId.Should().BeNull();
    }
}
