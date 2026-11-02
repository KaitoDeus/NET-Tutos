namespace NET_Tutos.Gamification.Domain.Exceptions;

public class GamificationDomainException : Exception
{
    public GamificationDomainException(string message) : base(message)
    {
    }

    public GamificationDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
