namespace F.Configurator.Expressions;

public abstract record Value
{
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Missing => new NumberValue(1);
}

public sealed record NumberValue(decimal Amount) : Value;
