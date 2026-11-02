namespace NET_Tutos.Gamification.Application.Common.Exceptions;

public class GamificationValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public GamificationValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public GamificationValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
