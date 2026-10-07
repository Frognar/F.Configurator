namespace F.Configurator.Expressions;

public abstract record Value
{
    public static Value Missing { get; } = new MissingValue();
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Boolean(bool value) => new BooleanValue(value);
}

public sealed record NumberValue(decimal Amount) : Value;

public sealed record BooleanValue(bool Value) : Value
{
    public static BooleanValue True { get; } = new(true);
    public static BooleanValue False { get; } = new(false);
}

public sealed record RangeValue(decimal Min, decimal Max) : Value;
public sealed record MissingValue : Value;
