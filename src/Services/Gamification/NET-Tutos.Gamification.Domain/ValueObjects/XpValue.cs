using NET_Tutos.Gamification.Domain.Common;
using NET_Tutos.Gamification.Domain.Exceptions;

namespace NET_Tutos.Gamification.Domain.ValueObjects;

public class XpValue : ValueObject
{
    public int Value { get; }

    public XpValue(int value)
    {
        if (value < 0)
            throw new NegativeXpValueException(value);

        Value = value;
    }

    public static XpValue Zero => new(0);

    public static XpValue Create(int value) => new(value);

    public static XpValue operator +(XpValue left, XpValue right) => new(left.Value + right.Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value} XP";

    public static implicit operator int(XpValue xp) => xp.Value;
    public static explicit operator XpValue(int value) => new(value);
}
