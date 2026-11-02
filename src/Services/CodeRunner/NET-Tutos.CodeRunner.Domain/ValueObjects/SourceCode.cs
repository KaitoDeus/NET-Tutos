using NET_Tutos.CodeRunner.Domain.Common;
using NET_Tutos.CodeRunner.Domain.Exceptions;
using NET_Tutos.CodeRunner.Domain.Rules;

namespace NET_Tutos.CodeRunner.Domain.ValueObjects;

public class SourceCode : ValueObject
{
    public string Code { get; }
    public int LineCount { get; }

    public SourceCode(string? code)
    {
        Code = code ?? string.Empty;
        LineCount = string.IsNullOrWhiteSpace(Code)
            ? 0
            : Code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;
    }

    public static SourceCode Create(string? code) => new(code);

    public void ValidateConstraints()
    {
        if (ExecutionConstraints.ContainsForbiddenKeyword(Code, out var forbidden))
        {
            throw new ForbiddenSyntaxException(forbidden ?? "Blocked Namespace / Dangerous API");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;

    public static implicit operator string(SourceCode source) => source.Code;
    public static explicit operator SourceCode(string code) => new(code);
}
