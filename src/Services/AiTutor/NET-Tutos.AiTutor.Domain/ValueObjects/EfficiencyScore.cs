using NET_Tutos.AiTutor.Domain.Common;

namespace NET_Tutos.AiTutor.Domain.ValueObjects;

public sealed class EfficiencyScore : ValueObject
{
    public const int MinScore = 1;
    public const int MaxScore = 100;

    public int Value { get; }
    public bool IsOptimal => Value >= 85;

    public EfficiencyScore(int rawScore)
    {
        Value = Math.Clamp(rawScore, MinScore, MaxScore);
    }

    public static implicit operator int(EfficiencyScore score) => score.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
