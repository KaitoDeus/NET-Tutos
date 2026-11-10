using FluentAssertions;
using Moq;
using NET_Tutos.BuildingBlocks.EventBus.Events;
using NET_Tutos.BuildingBlocks.EventBus.Kafka;
using NET_Tutos.Services;
using NET_Tutos.UnitTests.Helpers;
using Xunit;

namespace NET_Tutos.UnitTests.Services;

public class KafkaAnalyticsAndAdaptiveTests
{
    [Fact]
    public void KafkaStreamAnalyticsStore_AggregatesTopicsCompilerErrorsAndWeakSpots()
    {
        // Arrange
        var store = new KafkaStreamAnalyticsStore();

        store.RecordAiTelemetry(new AtusAiTelemetryEvent(
            SessionId: "sess-1",
            UserId: "user-adaptive-1",
            PromptSnippet: "Giải thích async và await cùng EF Core AsNoTracking",
            ModelUsed: "gemini-3.8-flash",
            LatencyMs: 420,
            IsSuccess: true,
            IsFallback: false));

        store.RecordCodeExecution(new CodeExecutionTelemetryEvent(
            ExecutionId: "exec-1",
            UserId: "user-adaptive-1",
            Language: "csharp",
            ExecutionDurationMs: 120,
            MemoryBytes: 2048,
            IsSuccess: false,
            HasCompileErrors: true,
            ErrorSnippet: "[Dòng 3] error CS0103: The name 'foo' does not exist in the current context"));

        store.RecordLearningActivity(new UserLearningActivityEvent(
            UserId: "user-adaptive-1",
            ActivityType: "MockInterviewEval",
            EntityId: "card-12",
            Title: "Dependency Injection",
            Metadata: new Dictionary<string, string>
            {
                ["score"] = "58",
                ["topic"] = "Dependency Injection"
            }));

        // Act
        var snapshot = store.GetSnapshot();
        var weakSpots = store.GetUserWeakSpots("user-adaptive-1");

        // Assert
        snapshot.TotalEventsConsumed.Should().Be(3);
        snapshot.AiTelemetryEventsConsumed.Should().Be(1);
        snapshot.CodeExecutionEventsConsumed.Should().Be(1);
        snapshot.LearningActivityEventsConsumed.Should().Be(1);
        snapshot.AverageAiLatencyMs.Should().Be(420);
        snapshot.AverageCodeExecutionMs.Should().Be(120);
        snapshot.TopAiTopics.Should().ContainKey("Async/Await & ThreadPool");
        snapshot.TopAiTopics.Should().ContainKey("Entity Framework Core");
        snapshot.TopCompilerErrors.Should().ContainKey("CS0103");
        snapshot.RecentEvents.Should().HaveCount(3);

        weakSpots.Should().HaveCount(2);
        weakSpots.Should().Contain(w => w.Topic == "Dependency Injection" && w.LowestScore == 58);
        weakSpots.Should().Contain(w => w.Topic == "C# Scope & Variable Declaration" && w.Source == "CodeExecution");
    }

    [Fact]
    public async Task AdaptiveLearningService_WhenUserHasWeakSpots_RecommendsTargetedRagLessons()
    {
        // Arrange
        using var context = MockHelpers.CreateInMemoryDbContext();
        var store = new KafkaStreamAnalyticsStore();
        store.RecordLearningActivity(new UserLearningActivityEvent(
            UserId: "learner-99",
            ActivityType: "MockInterviewEval",
            EntityId: "card-5",
            Title: "Async/Await",
            Metadata: new Dictionary<string, string>
            {
                ["score"] = "50",
                ["topic"] = "Async/Await"
            }));

        var mockSearch = new Mock<ISemanticCurriculumSearchService>();
        mockSearch.Setup(s => s.SearchAsync(It.IsAny<string>(), false, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SemanticSearchResponse
            {
                Hits =
                [
                    new SemanticSearchHit
                    {
                        LessonNumber = 18,
                        SectionNumber = 3,
                        Title = "Lập trình Bất đồng bộ (Async/Await) & ThreadPool",
                        Summary = "Làm chủ Task, ValueTask và tránh deadlock.",
                        Score = 0.91
                    }
                ]
            });

        var service = new AdaptiveLearningService(context, mockSearch.Object, store);

        // Act
        var profile = await service.GetRecommendationsAsync("learner-99", isEnglish: false);

        // Assert
        profile.IsAuthenticated.Should().BeTrue();
        profile.DetectedWeakSpots.Should().ContainSingle(w => w.Topic == "Async/Await" && w.LowestScore == 50);
        profile.RecommendedLessons.Should().ContainSingle(r =>
            r.Url == "/chuong-trinh-csharp/18" &&
            r.Category == "Bù đắp lỗ hổng");
        profile.SuggestedInterviewLevel.Should().Contain("Junior");
    }

    [Fact]
    public async Task CodeExecutionService_OnCompilationError_EmitsKafkaTelemetryEvent()
    {
        // Arrange
        var kafkaMock = new Mock<IKafkaProducer>();
        var service = new CodeExecutionService(kafkaProducer: kafkaMock.Object);

        // Act
        var result = await service.ExecuteAsync("Console.WriteLine(undeclaredVariable);", timeoutMs: 3000);

        // Assert
        result.IsSuccess.Should().BeFalse();
        kafkaMock.Verify(k => k.ProduceFireAndForget(
            KafkaTopics.CodeExecutionTelemetry,
            It.Is<CodeExecutionTelemetryEvent>(e => !e.IsSuccess && e.HasCompileErrors)),
            Times.Once);
    }
}
