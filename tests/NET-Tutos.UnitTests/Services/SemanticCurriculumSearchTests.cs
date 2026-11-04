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
}
