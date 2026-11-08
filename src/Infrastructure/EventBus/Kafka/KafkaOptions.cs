namespace NET_Tutos.BuildingBlocks.EventBus.Kafka;

/// <summary>
/// Configuration options for Apache Kafka cluster connectivity.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Whether Kafka streaming is enabled. If false, NullKafkaProducer operates without errors.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Comma-separated list of Kafka broker host:port addresses (e.g. "kafka:9092" or "localhost:29092").
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:29092";

    /// <summary>
    /// Client ID passed in Kafka protocol requests.
    /// </summary>
    public string ClientId { get; set; } = "net-tutos-app";

    /// <summary>
    /// Timeout in milliseconds for metadata and connection attempts. Default 3000ms.
    /// </summary>
    public int TimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Maximum time in milliseconds to wait for broker acknowledgements. Default 5000ms.
    /// </summary>
    public int MessageTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Compression type for produced batches (None, Gzip, Snappy, Lz4, Zstd). Default Snappy.
    /// </summary>
    public string CompressionType { get; set; } = "Snappy";

    /// <summary>
    /// Acks policy (0 = None, 1 = Leader, -1 = All). Default 1 for low latency telemetry.
    /// </summary>
    public int Acks { get; set; } = 1;
}
