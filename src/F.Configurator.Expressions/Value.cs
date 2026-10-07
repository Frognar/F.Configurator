namespace F.Configurator.Expressions;

public abstract record Value
{
    public static Value Missing { get; } = new MissingValue();
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Boolean(bool boolean) => new BooleanValue(boolean);
    public static Value Text(string text) => new TextValue(text);
}

public sealed record NumberValue(decimal Value) : Value;

public sealed record BooleanValue(bool Value) : Value
{
    public static BooleanValue True { get; } = new(true);
    public static BooleanValue False { get; } = new(false);
}

public sealed record RangeValue(decimal Min, decimal Max) : Value;
public sealed record TextValue(string Value) : Value;
public sealed record MissingValue : Value;
