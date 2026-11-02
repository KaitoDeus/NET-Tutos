namespace NET_Tutos.CodeRunner.Domain.Rules;

public static class ExecutionConstraints
{
    public const int DefaultTimeoutMs = 4000;
    public const int MaxTimeoutMs = 15000;
    public const int MinTimeoutMs = 500;

    public static readonly string[] ForbiddenKeywords =
    {
        "System.Diagnostics.Process",
        "Process.Start",
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileStream",
        "System.Environment.Exit",
        "System.Net.Sockets",
        "System.Reflection.Emit",
        "Assembly.Load"
    };

    public static bool ContainsForbiddenKeyword(string code, out string? matchedKeyword)
    {
        foreach (var keyword in ForbiddenKeywords)
        {
            if (code.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                matchedKeyword = keyword;
                return true;
            }
        }

        matchedKeyword = null;
        return false;
    }
}
