namespace NET_Tutos.BuildingBlocks.Common.Sandbox;

/// <summary>
/// Shared security keywords, source payload limits, and execution timeout guardrails
/// used by both the CodeRunner microservice and the Web BFF fallback sandbox.
/// </summary>
public static class SandboxGuardrails
{
    public const int DefaultTimeoutMs = 4_000;
    public const int MinExecutionTimeoutMs = 200;
    public const int MaxExecutionTimeoutMs = 15_000;
    public const int MaxSourceCodeLength = 32_768;
    public const int MaxLineCount = 500;
    public const int MaxOutputCharacters = 16_384;

    public static readonly string[] ForbiddenKeywords =
    [
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
    ];

    public static int ClampTimeoutMs(int requestedTimeoutMs)
    {
        if (requestedTimeoutMs <= 0)
        {
            return DefaultTimeoutMs;
        }

        return Math.Clamp(requestedTimeoutMs, MinExecutionTimeoutMs, MaxExecutionTimeoutMs);
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

    public static bool TryValidatePayloadLimits(string code, out string? errorMessage)
    {
        if (code.Length > MaxSourceCodeLength)
        {
            errorMessage = $"Từ chối thực thi: Mã nguồn vượt quá giới hạn kích thước cho phép ({MaxSourceCodeLength} ký tự).";
            return false;
        }

        int lines = 1;
        foreach (char c in code)
        {
            if (c == '\n')
            {
                lines++;
                if (lines > MaxLineCount)
                {
                    errorMessage = $"Từ chối thực thi: Mã nguồn vượt quá giới hạn số dòng cho phép ({MaxLineCount} dòng).";
                    return false;
                }
            }
        }

        errorMessage = null;
        return true;
    }

    public static string ValidateSecurityAndLimits(string code)
    {
        if (!TryValidatePayloadLimits(code, out var limitError))
        {
            return limitError ?? string.Empty;
        }

        if (ContainsForbiddenKeyword(code, out var matchedKeyword))
        {
            return $"Từ chối thực thi: Mã nguồn chứa từ khóa bị cấm vì lý do an toàn bảo mật ('{matchedKeyword}').";
        }

        return string.Empty;
    }
}
