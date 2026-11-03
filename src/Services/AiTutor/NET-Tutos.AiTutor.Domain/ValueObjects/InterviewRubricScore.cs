using NET_Tutos.AiTutor.Domain.Common;
using NET_Tutos.AiTutor.Domain.Enums;
using NET_Tutos.AiTutor.Domain.Exceptions;

namespace NET_Tutos.AiTutor.Domain.ValueObjects;

public sealed class InterviewRubricScore : ValueObject
{
    public int TechnicalAccuracy { get; }
    public int ClarityStructure { get; }
    public int PracticalDepth { get; }
    public int WeightedOverall { get; }
    public InterviewReadinessTier ReadinessTier { get; }

    public InterviewRubricScore(int technicalAccuracy, int clarityStructure, int practicalDepth)
    {
        if (technicalAccuracy is < 0 or > 100)
            throw new InvalidRubricScoreException(technicalAccuracy);
        if (clarityStructure is < 0 or > 100)
            throw new InvalidRubricScoreException(clarityStructure);
        if (practicalDepth is < 0 or > 100)
            throw new InvalidRubricScoreException(practicalDepth);

        TechnicalAccuracy = technicalAccuracy;
        ClarityStructure = clarityStructure;
        PracticalDepth = practicalDepth;

        // Weighted rubric: 50% Technical, 25% Clarity, 25% Practical Depth
        WeightedOverall = (int)Math.Round((TechnicalAccuracy * 0.50) + (ClarityStructure * 0.25) + (PracticalDepth * 0.25));

        ReadinessTier = WeightedOverall switch
        {
            >= 85 => InterviewReadinessTier.SeniorReady,
            >= 70 => InterviewReadinessTier.MiddleReady,
            >= 50 => InterviewReadinessTier.JuniorReady,
            _ => InterviewReadinessTier.FoundationalStudyNeeded
        };
    }

    public string GetPerformanceRating(bool isEnglish)
    {
        return ReadinessTier switch
        {
            InterviewReadinessTier.SeniorReady => isEnglish ? "Senior .NET Specialist (Outstanding)" : "Trình độ Senior .NET (Xuất sắc)",
            InterviewReadinessTier.MiddleReady => isEnglish ? "Strong Middle Developer (Confident)" : "Trình độ Middle .NET (Khá vững)",
            InterviewReadinessTier.JuniorReady => isEnglish ? "Junior / Developing Knowledge" : "Trình độ Junior (Đang phát triển)",
            _ => isEnglish ? "Fundamental Gaps Detected" : "Cần củng cố kiến thức nền tảng"
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TechnicalAccuracy;
        yield return ClarityStructure;
        yield return PracticalDepth;
        yield return WeightedOverall;
    }
}
