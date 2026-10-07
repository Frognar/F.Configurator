namespace F.Configurator.Expressions;

public abstract record Value
{
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Missing => new MissingValue();
}

public sealed record NumberValue(decimal Amount) : Value;

public sealed record MissingValue : Value;
