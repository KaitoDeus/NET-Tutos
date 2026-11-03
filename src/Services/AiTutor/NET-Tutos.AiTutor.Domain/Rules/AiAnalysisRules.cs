using System.Text.RegularExpressions;
using NET_Tutos.AiTutor.Domain.Enums;

namespace NET_Tutos.AiTutor.Domain.Rules;

public static class AiAnalysisRules
{
    public static ComplexityTier DetectTimeComplexity(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return ComplexityTier.Constant;

        bool hasNestedLoop = Regex.IsMatch(sourceCode, @"(for|while|foreach)[^{]*\{[^{}]*(for|while|foreach)");
        if (hasNestedLoop)
            return ComplexityTier.Quadratic;

        bool hasSorting = sourceCode.Contains(".OrderBy") || sourceCode.Contains(".Sort(") || sourceCode.Contains("Array.Sort");
        if (hasSorting)
            return ComplexityTier.LogLinear;

        bool hasRecursion = Regex.IsMatch(sourceCode, @"\b([a-zA-Z0-9_]+)\s*\([^)]*\)[^{]*\{[^{}]*\b\1\s*\(");
        if (hasRecursion)
            return ComplexityTier.Exponential;

        int loopCount = Regex.Matches(sourceCode, @"\b(for|while|foreach)\b").Count;
        if (loopCount > 0)
            return ComplexityTier.Linear;

        return ComplexityTier.Constant;
    }

    public static string FormatBigONotation(ComplexityTier tier)
    {
        return tier switch
        {
            ComplexityTier.Constant => "O(1)",
            ComplexityTier.Logarithmic => "O(log N)",
            ComplexityTier.Linear => "O(N)",
            ComplexityTier.LogLinear => "O(N log N)",
            ComplexityTier.Quadratic => "O(N²)",
            ComplexityTier.Exponential => "O(2ⁿ) hoặc O(N)",
            _ => "O(N)"
        };
    }

    public static bool HasLinearSpaceAllocation(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return false;

        return sourceCode.Contains("Dictionary<") ||
               sourceCode.Contains("HashSet<") ||
               sourceCode.Contains("new int[") ||
               sourceCode.Contains("new string[") ||
               sourceCode.Contains("new List<");
    }
}
