using System.Globalization;

namespace F.Configurator.Expressions;

public abstract record Value
{
    public static MissingValue Missing { get; } = new();
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Boolean(bool isTrue) => isTrue ? BooleanValue.True : BooleanValue.False;
    public static Value Text(string content) => new TextValue(content);
    public static Value Option(string id) => new OptionValue(id);
    public static Value Range(decimal min, decimal max) => new RangeValue(min, max);
    public static Value List(params IEnumerable<Value> values) => new ListValue([.. values]);
}

public sealed record NumberValue(decimal Amount) : Value
{
    internal NumberValue RoundTo(NumberValue step) =>
        new(Math.Round(Amount / step.Amount, MidpointRounding.AwayFromZero) * step.Amount);

    public TextValue AsText() =>
        new(Amount.ToString("0.############################", CultureInfo.InvariantCulture));
}

public sealed record BooleanValue(bool IsTrue) : Value
{
    public static BooleanValue True { get; } = new(true);
    public static BooleanValue False { get; } = new(false);
}

public sealed record RangeValue(decimal Min, decimal Max) : Value;

public sealed record TextValue(string Content) : Value;

public sealed record OptionValue(string Id) : Value;

public sealed record MissingValue : Value;

public sealed record ListValue(EquatableList<Value> Values) : Value;
