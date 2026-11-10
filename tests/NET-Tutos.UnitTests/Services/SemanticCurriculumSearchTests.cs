using FluentAssertions;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class SemanticCurriculumSearchTests
{
    private static SemanticCurriculumSearchService CreateService()
    {
        var context = MockHelpers.CreateInMemoryDbContext();
        return new SemanticCurriculumSearchService(new CurriculumService(context));
    }

    [Fact]
    public async Task SearchAsync_EnglishQuery_RanksDelegateLessonFirst()
    {
        var service = CreateService();

        var result = await service.SearchAsync("delegate Action Func Predicate", isEnglish: true, topK: 3);

        result.IndexedDocuments.Should().Be(101);
        result.VectorDimensions.Should().Be(SemanticCurriculumSearchService.Dimensions);
        result.Hits.Should().NotBeEmpty();
        result.Hits[0].Title.Should().Contain("Delegate");
        result.Hits.Should().BeInDescendingOrder(h => h.Score);
    }

    [Fact]
    public async Task SearchAsync_VietnameseQueryWithoutDiacritics_MatchesAccentedLesson()
    {
        var service = CreateService();

        // "vong lap" (no diacritics) must match "Vòng lặp" lesson via diacritic folding
        var result = await service.SearchAsync("vong lap for while", isEnglish: false, topK: 3);

        result.Hits.Should().NotBeEmpty();
        result.Hits.Select(h => h.LessonNumber).Should().Contain(6);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsNoHits()
    {
        var service = CreateService();

        var result = await service.SearchAsync("   ", isEnglish: true);

        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildRagContextAsync_ProducesCitedLessonLinks()
    {
        var service = CreateService();

        var context = await service.BuildRagContextAsync("null safety nullable reference types", isEnglish: true);

        context.Should().Contain("Relevant NET-Tutos curriculum lessons");
        context.Should().Contain("/csharp-curriculum/");
    }

    [Fact]
    public async Task SearchKnowledgeBaseAsync_IndexesMultiSourceDbAndCurriculumDocuments()
    {
        // Arrange
        using var db = MockHelpers.CreateInMemoryDbContext();
        db.Tutorials.Add(new NET_Tutos.Models.Entities.Tutorial
        {
            Title = "Fixing Captive Dependency in ASP.NET Core",
            Slug = "captive-dependency-di",
            Summary = "Understand Scoped vs Singleton lifetimes cleanly.",
            ContentMarkdown = "Never inject a Scoped service into a Singleton service directly."
        });
        db.CodingChallenges.Add(new NET_Tutos.Models.Entities.CodingChallenge
        {
            Title = "Captive Dependency Lifetime Bugfix",
            Slug = "captive-di-bugfix",
            ShortDescription = "Refactor Singleton consuming Scoped DbContext using IServiceScopeFactory.",
            InstructionsMarkdown = "Use IServiceScopeFactory inside Singleton background workers."
        });
        db.InterviewFlashcards.Add(new NET_Tutos.Models.Entities.InterviewFlashcard
        {
            Slug = "captive-di-flashcard",
            QuestionVi = "Lỗi Captive Dependency trong Dependency Injection là gì?",
            QuestionEn = "What is a Captive Dependency bug in .NET DI?",
            AnswerMarkdownVi = "Xảy ra khi service Singleton giữ tham chiếu đến Scoped service.",
            AnswerMarkdownEn = "Occurs when a longer-lived Singleton captures a Scoped service."
        });
        await db.SaveChangesAsync();

        var service = new SemanticCurriculumSearchService(new CurriculumService(db), directDbContext: db);

        // Act
        var searchResult = await service.SearchKnowledgeBaseAsync("Captive Dependency Singleton Scoped", isEnglish: true, topK: 5);
        var ragContext = await service.BuildUnifiedRagContextAsync("Captive Dependency Singleton Scoped", isEnglish: true, topK: 5);

        // Assert
        searchResult.TotalIndexedDocuments.Should().BeGreaterThanOrEqualTo(104);
        searchResult.SourceCounts["Curriculum"].Should().Be(101);
        searchResult.SourceCounts["Tutorial"].Should().BeGreaterThanOrEqualTo(1);
        searchResult.SourceCounts["Challenge"].Should().BeGreaterThanOrEqualTo(1);
        searchResult.SourceCounts["Flashcard"].Should().BeGreaterThanOrEqualTo(1);

        searchResult.Hits.Should().Contain(h => h.SourceType == "Tutorial" && h.Url == "/lessons/captive-dependency-di");
        searchResult.Hits.Should().Contain(h => h.SourceType == "Challenge" && h.Url == "/Playground?slug=captive-di-bugfix");
        ragContext.Should().Contain("Multi-Source Knowledge RAG Context");
    }
}
