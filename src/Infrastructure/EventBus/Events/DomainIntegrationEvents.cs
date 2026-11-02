namespace NET_Tutos.BuildingBlocks.EventBus.Events;

public record ChallengeSolvedIntegrationEvent(
    string UserId,
    int ChallengeId,
    string ChallengeTitle,
    int XpAwarded,
    long ExecutionDurationMs
) : IntegrationEvent;

public record LessonCompletedIntegrationEvent(
    string UserId,
    int TutorialId,
    string TutorialTitle,
    int XpAwarded
) : IntegrationEvent;

public record ExamPassedIntegrationEvent(
    string UserId,
    int? CategoryId,
    string ExamTitle,
    int ScorePercentage,
    int XpAwarded
) : IntegrationEvent;
