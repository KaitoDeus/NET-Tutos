namespace NET_Tutos.Models.Entities;

/// <summary>
/// Persisted multi-source RAG document metadata and 768-dimensional dense neural embedding vector.
/// Supports both PostgreSQL pgvector (HNSW vector(768) column) and JSON fallback on SQLite/InMemory.
/// </summary>
public class KnowledgeEmbedding
{
    public int Id { get; set; }
    public string DocumentKey { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Curriculum";
    public string TitleVi { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SummaryVi { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string UrlVi { get; set; } = string.Empty;
    public string UrlEn { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "nettutos-neural-hash-768";
    public int Dimensions { get; set; } = 768;
    public string VectorJson { get; set; } = "[]";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
