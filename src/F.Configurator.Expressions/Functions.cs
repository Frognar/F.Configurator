namespace F.Configurator.Expressions;

public abstract record Extremum(EquatableList<Expression> Expressions) : Expression
{
    public sealed override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        var evaluated = Expressions
            .Select(expr => expr.Evaluate(values))
            .OfType<NumberValue>()
            .ToList();

        return evaluated.Count == Expressions.Count
            ? Value.Number(Pick(evaluated.Select(v => v.Amount)))
            : Value.Missing;
    }

    protected abstract decimal Pick(IEnumerable<decimal> amounts);
}

public sealed record Min(EquatableList<Expression> Expressions) : Extremum(Expressions)
{
    protected override decimal Pick(IEnumerable<decimal> amounts) => amounts.Min();
}

public sealed record Max(EquatableList<Expression> Expressions) : Extremum(Expressions)
{
    protected override decimal Pick(IEnumerable<decimal> amounts) => amounts.Max();
}

public sealed record Round(Expression Operand, Expression Step) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Operand.Evaluate(values), Step.Evaluate(values)) switch
        {
            (NumberValue number, NumberValue { Amount: > 0 } step) => SafeRound(number, step),
            _ => Value.Missing,
        };

    private Value SafeRound(NumberValue number, NumberValue step)
    {
        try
        {
            return number.RoundTo(step);
        }
        catch (OverflowException)
        {
            return Value.Missing;
        }
    }
}

public sealed record Length(Expression Operand) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Operand.Evaluate(values) switch
        {
            TextValue { Content: var str } => Value.Number(str.Length),
            _ => Value.Missing,
        };
}

public sealed record Pad(Expression Operand, Expression TotalWidth, Expression PaddingChar) : Expression
{
    private const decimal MaxWidth = 1000;

    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Operand.Evaluate(values).AsText(), TotalWidth.Evaluate(values), PaddingChar.Evaluate(values)) switch
        {
            (
                TextValue { Content: var text },
                NumberValue { Amount: var width and >= 0 and <= MaxWidth },
                TextValue { Content: [var c] }
                ) =>
                Value.Text(text.PadLeft(decimal.ToInt32(width), c)),
            _ => Value.Missing,
        };
}

file static class ValueExtensions
{
    extension(Value value)
    {
        public Value AsText() =>
            value switch
            {
                NumberValue number => number.AsText(),
                TextValue text => text,
                _ => Value.Missing,
            };
    }
}
