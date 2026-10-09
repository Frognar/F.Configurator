using System.Globalization;

namespace F.Configurator.Expressions;

public abstract record Value
{
    public static Value Missing { get; } = new MissingValue();
    public static Value Number(decimal number) => new NumberValue(number);
    public static Value Boolean(bool isTrue) => new BooleanValue(isTrue);
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
    public static Value True { get; } = new BooleanValue(true);
    public static Value False { get; } = new BooleanValue(false);
}

public sealed record RangeValue(decimal Min, decimal Max) : Value;

public sealed record TextValue(string Content) : Value;

public sealed record OptionValue(string Id) : Value;

public sealed record MissingValue : Value;

public sealed record ListValue(EquatableList<Value> Values) : Value;
