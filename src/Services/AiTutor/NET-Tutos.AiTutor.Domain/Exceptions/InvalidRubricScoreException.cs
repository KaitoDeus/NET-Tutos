namespace NET_Tutos.AiTutor.Domain.Exceptions;

public class InvalidRubricScoreException : AiTutorDomainException
{
    public InvalidRubricScoreException(int score)
        : base($"Rubric score '{score}' is outside the valid domain range [0..100].")
    {
    }
}
