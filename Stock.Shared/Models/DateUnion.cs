namespace Stock.Shared.Models;

public struct DateUnion
{
    public DateTimeOffset? DateTime;
    public DateEnum? Enum;

    public static implicit operator DateUnion(DateTimeOffset DateTime) => new DateUnion { DateTime = DateTime };
    public static implicit operator DateUnion(DateEnum Enum) => new DateUnion { Enum = Enum };
}

public enum DateEnum { The00011130 };