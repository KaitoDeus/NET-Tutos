namespace NET_Tutos.AiTutor.Domain.Exceptions;

public class AiTutorDomainException : Exception
{
    public AiTutorDomainException(string message) : base(message) { }
    public AiTutorDomainException(string message, Exception innerException) : base(message, innerException) { }
}
