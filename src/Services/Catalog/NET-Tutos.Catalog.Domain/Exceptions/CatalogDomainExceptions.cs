namespace NET_Tutos.Catalog.Domain.Exceptions;

public class CatalogDomainException : Exception
{
    public string Code { get; }

    public CatalogDomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public sealed class InvalidReadingDurationException : CatalogDomainException
{
    public InvalidReadingDurationException(int minutes)
        : base("CATALOG_INVALID_DURATION", $"Estimated reading duration must be between 1 and 240 minutes (received: {minutes}).")
    {
    }
}

public sealed class InvalidExamScoreException : CatalogDomainException
{
    public InvalidExamScoreException(string reason)
        : base("CATALOG_INVALID_EXAM_SCORE", reason)
    {
    }
}
