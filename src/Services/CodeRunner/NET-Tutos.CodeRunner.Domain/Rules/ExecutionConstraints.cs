namespace NET_Tutos.CodeRunner.Domain.Rules;

public static class ExecutionConstraints
{
    public const int DefaultTimeoutMs = 4000;
    public const int MaxTimeoutMs = 15000;
    public const int MinTimeoutMs = 500;
    public const int MaxSourceCodeLength = 32_768;
    public const int MaxLineCount = 500;
    public const int MaxOutputCharacters = 16_384;

    public static readonly string[] ForbiddenKeywords =
    {
        "System.Diagnostics.Process",
        "Process.Start",
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileStream",
        "FileInfo",
        "DirectoryInfo",
        "StreamReader",
        "StreamWriter",
        "System.Environment.Exit",
        "Environment.GetEnvironmentVariable",
        "Environment.GetEnvironmentVariables",
        "Environment.SetEnvironmentVariable",
        "System.Net.Sockets",
        "System.Net.Http",
        "System.Net.WebClient",
        "System.Reflection.Emit",
        "Assembly.Load",
        "Activator.CreateInstance",
        "AppDomain.CurrentDomain",
        "DllImport",
        "LibraryImport",
        "System.Runtime.InteropServices.Marshal"
    };

    public static int ClampTimeoutMs(int requestedTimeoutMs)
    {
        if (requestedTimeoutMs <= 0)
        {
            return DefaultTimeoutMs;
        }

        return Math.Clamp(requestedTimeoutMs, 200, MaxTimeoutMs);
    }

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
