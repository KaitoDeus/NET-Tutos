namespace NET_Tutos.BuildingBlocks.EventBus.Events;

/// <summary>
/// Predefined, standardized Apache Kafka topic names for the NET-Tutos ecosystem.
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Stream of all AI queries, responses, model usage, latency, and token metrics from Atus.
    /// </summary>
    public const string AtusAiTelemetry = "nettutos.atus.ai-telemetry";

    /// <summary>
    /// Real-time stream of all learner interactions (lesson views, quiz results, challenge attempts)
    /// feeding into the Adaptive Learning Engine.
    /// </summary>
    public const string UserLearningActivity = "nettutos.learning.user-activities";

    /// <summary>
    /// Real-time stream of code executions executed within the Roslyn sandbox runner.
    /// </summary>
    public const string CodeExecutionTelemetry = "nettutos.coderunner.executions";

    /// <summary>
    /// Immutable audit trail stream for critical actions (daily streak check-ins, certificate grants).
    /// </summary>
    public const string AuditTrail = "nettutos.audit.activity-log";
}
