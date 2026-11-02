namespace NET_Tutos.CodeRunner.Domain.Exceptions;

public class ForbiddenSyntaxException : CodeRunnerDomainException
{
    public string ForbiddenKeyword { get; }

    public ForbiddenSyntaxException(string forbiddenKeyword)
        : base($"Execution rejected: Security violation detected. Usage of '{forbiddenKeyword}' is forbidden in the sandbox.")
    {
        ForbiddenKeyword = forbiddenKeyword;
    }
}
