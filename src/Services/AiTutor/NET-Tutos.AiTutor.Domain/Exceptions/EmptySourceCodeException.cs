namespace NET_Tutos.AiTutor.Domain.Exceptions;

public class EmptySourceCodeException : AiTutorDomainException
{
    public EmptySourceCodeException()
        : base("Source code cannot be null or whitespace for algorithmic analysis.")
    {
    }
}
