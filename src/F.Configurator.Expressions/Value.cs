namespace F.Configurator.Expressions;

public abstract record Value
{
    public static Value Missing { get; } = new MissingValue();
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Boolean(bool boolean) => new BooleanValue(boolean);
    public static Value Text(string text) => new TextValue(text);
    public static Value Range(decimal min, decimal max) => new RangeValue(min, max);
}

public sealed record NumberValue(decimal Value) : Value;

public sealed record BooleanValue(bool Value) : Value
{
    public static Value True { get; } = new BooleanValue(true);
    public static Value False { get; } = new BooleanValue(false);
}

public sealed record RangeValue(decimal Min, decimal Max) : Value;
public sealed record TextValue(string Value) : Value;
public sealed record MissingValue : Value;
