using System.Security.Cryptography;
using System.Text;
using NET_Tutos.Catalog.Domain.Enums;
using NET_Tutos.Catalog.Domain.Exceptions;

namespace NET_Tutos.Catalog.Domain.ValueObjects;

public sealed record ReadingDuration
{
    public int Minutes { get; }
    public string PaceCategory => Minutes switch
    {
        <= 7 => "Quick Read",
        <= 15 => "Standard Lesson",
        _ => "Deep-Dive Architecture"
    };

    private ReadingDuration(int minutes)
    {
        Minutes = minutes;
    }

    public static ReadingDuration Create(int minutes)
    {
        if (minutes < 1 || minutes > 240)
        {
            throw new InvalidReadingDurationException(minutes);
        }
        return new ReadingDuration(minutes);
    }
}

public sealed record ExamScore
{
    public const double PassingThresholdPercent = 80.0;

    public int CorrectAnswers { get; }
    public int TotalQuestions { get; }
    public double Percentage { get; }
    public bool IsPassed => Percentage >= PassingThresholdPercent;
    public ExamGradeTier GradeTier { get; }

    private ExamScore(int correctAnswers, int totalQuestions, double percentage, ExamGradeTier gradeTier)
    {
        CorrectAnswers = correctAnswers;
        TotalQuestions = totalQuestions;
        Percentage = percentage;
        GradeTier = gradeTier;
    }

    public static ExamScore Evaluate(int correctAnswers, int totalQuestions)
    {
        if (totalQuestions <= 0)
        {
            throw new InvalidExamScoreException("Total questions must be greater than zero.");
        }
        if (correctAnswers < 0 || correctAnswers > totalQuestions)
        {
            throw new InvalidExamScoreException($"Correct answers ({correctAnswers}) must be between 0 and total questions ({totalQuestions}).");
        }

        double pct = Math.Round((double)correctAnswers / totalQuestions * 100.0, 2);
        var tier = pct switch
        {
            >= 95.0 => ExamGradeTier.Distinction,
            >= 85.0 => ExamGradeTier.Merit,
            >= 80.0 => ExamGradeTier.Pass,
            _ => ExamGradeTier.Fail
        };

        return new ExamScore(correctAnswers, totalQuestions, pct, tier);
    }

    public int CalculateAwardedXp()
    {
        if (!IsPassed) return 0;
        return GradeTier switch
        {
            ExamGradeTier.Distinction => 300,
            ExamGradeTier.Merit => 250,
            ExamGradeTier.Pass => 200,
            _ => 0
        };
    }

    public string GenerateCertificateCode(string userId, string trackCode = "DOTNET10")
    {
        if (!IsPassed) return string.Empty;
        var seed = $"{userId}:{trackCode}:{CorrectAnswers}/{TotalQuestions}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)))[..8];
        return $"NT-CERT-2026-{hash}";
    }
}
