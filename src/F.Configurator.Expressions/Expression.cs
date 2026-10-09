namespace F.Configurator.Expressions;

public abstract record Expression
{
    public abstract Value Evaluate(IReadOnlyDictionary<string, Value> values);

    public static Expression Number(decimal number) => new Number(number);
    public static Expression Reference(string name) => new Reference(name);
    public static Expression Range(decimal lower, decimal upper) => new Range(lower, upper);
    public static Expression Text(string text) => new Text(text);
    public static Expression Option(string id) => new Option(id);

    public static Expression OptionAttribute(Expression selected, EquatableDictionary<string, Value> valuesByOption) =>
        new OptionAttribute(selected, valuesByOption);

    public static Expression Add(Expression left, Expression right) => new Add(left, right);
    public static Expression Subtract(Expression left, Expression right) => new Subtract(left, right);
    public static Expression Multiply(Expression left, Expression right) => new Multiply(left, right);
    public static Expression Divide(Expression left, Expression right) => new Divide(left, right);
    public static Expression Negate(Expression operand) => new Negate(operand);
    public static Expression Min(Expression first, params IEnumerable<Expression> rest) => new Min([first, .. rest]);
    public static Expression Max(Expression first, params IEnumerable<Expression> rest) => new Max([first, .. rest]);

    public static Expression Equal(Expression left, Expression right) => new Equal(left, right);
    public static Expression NotEqual(Expression left, Expression right) => new NotEqual(left, right);

    public static Expression LessThan(Expression left, Expression right) => new LessThan(left, right);
    public static Expression LessThanOrEqual(Expression left, Expression right) => new LessThanOrEqual(left, right);
    public static Expression GreaterThan(Expression left, Expression right) => new GreaterThan(left, right);

    public static Expression GreaterThanOrEqual(Expression left, Expression right) =>
        new GreaterThanOrEqual(left, right);

    public static Expression In(Expression left, Expression right) => new In(left, right);
    public static Expression NotIn(Expression left, Expression right) => new NotIn(left, right);

    public static Expression And(Expression left, Expression right) => new And(left, right);
    public static Expression Or(Expression left, Expression right) => new Or(left, right);
    public static Expression Not(Expression operand) => new Not(operand);

    public static Expression If(Expression condition, Expression then, Expression otherwise)
        => new If(condition, then, otherwise);

    public static Expression Length(Expression operand) => new Length(operand);
    public static Expression Pad(Expression operand, Expression totalWidth) =>
        new Pad(operand, totalWidth, Text("0"));

    public static Expression Pad(Expression operand, Expression totalWidth, Expression paddingChar) =>
        new Pad(operand, totalWidth, paddingChar);

    public static Expression Round(Expression operand) => new Round(operand, Number(1));
    public static Expression Round(Expression operand, Expression step) => new Round(operand, step);

    public static Expression TableLookup(Table table, IEnumerable<Expression> keys, string column) =>
        new TableLookup(table, [.. keys], column);

    public static Expression List(Expression expression, params IEnumerable<Expression> expressions) =>
        new ListExpression([expression, .. expressions]);
}

public sealed record Number(decimal Amount) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Number(Amount);
}

public sealed record Reference(string Name) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        values.TryGetValue(Name, out var value) ? value : Value.Missing;
}

public sealed record Range(decimal Lower, decimal Upper) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Range(Lower, Upper);
}

public sealed record Text(string Content) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Text(Content);
}

public sealed record Option(string Id) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Option(Id);
}

public sealed record OptionAttribute(
    Expression Selected,
    EquatableDictionary<string, Value> ValuesByOption) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Selected.Evaluate(values) switch
        {
            OptionValue option => ValuesByOption.TryGetValue(option.Id, out var value) ? value : Value.Missing,
            _ => Value.Missing,
        };
}

public abstract record BinaryArithmetic(Expression Left, Expression Right) : Expression
{
    public sealed override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Combine(Left.Evaluate(values), Right.Evaluate(values));

    protected virtual Value Combine(Value left, Value right) =>
        (left, right) switch
        {
            (NumberValue l, NumberValue r) => SafeApply(l.Amount, r.Amount),
            _ => Value.Missing,
        };

    private Value SafeApply(decimal left, decimal right)
    {
        try
        {
            return Apply(left, right);
        }
        catch (OverflowException)
        {
            return Value.Missing;
        }
    }

    protected abstract Value Apply(decimal left, decimal right);
}

public sealed record Add(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Combine(Value left, Value right) =>
        (left, right) switch
        {
            (TextValue l, TextValue r) => Value.Text(l.Content + r.Content),
            _ => base.Combine(left, right),
        };

    protected override Value Apply(decimal left, decimal right) => Value.Number(left + right);
}

public sealed record Subtract(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) => Value.Number(left - right);
}

public sealed record Multiply(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) => Value.Number(left * right);
}

public sealed record Divide(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) =>
        right == decimal.Zero ? Value.Missing : Value.Number(left / right);
}

public sealed record Negate(Expression Operand) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Operand.Evaluate(values) switch
        {
            NumberValue { Amount: var amount } => Value.Number(-amount),
            _ => Value.Missing,
        };
}

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

public sealed record Equal(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (MissingValue, _) or (_, MissingValue) => BooleanValue.False,
            ({ } l, { } r) => Value.Boolean(l == r),
        };
}

public sealed record NotEqual(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        new Not(new Equal(Left, Right)).Evaluate(values);
}

public abstract record BinaryComparison(Expression Left, Expression Right) : Expression
{
    public sealed override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        return (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue left, NumberValue right) => Compare(left.Amount, right.Amount),
            _ => BooleanValue.False,
        };
    }

    protected abstract Value Compare(decimal left, decimal right);
}

public sealed record LessThan(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override Value Compare(decimal left, decimal right) => Value.Boolean(left < right);
}

public sealed record LessThanOrEqual(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override Value Compare(decimal left, decimal right) => Value.Boolean(left <= right);
}

public sealed record GreaterThan(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override Value Compare(decimal left, decimal right) => Value.Boolean(left > right);
}

public sealed record GreaterThanOrEqual(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override Value Compare(decimal left, decimal right) => Value.Boolean(left >= right);
}

public sealed record In(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (MissingValue, _) => BooleanValue.False,
            (NumberValue n, RangeValue r) => Value.Boolean(n.Amount >= r.Min && n.Amount <= r.Max),
            ({ } item, ListValue list) => Value.Boolean(list.Values.Contains(item)),
            _ => BooleanValue.False,
        };
}

public sealed record NotIn(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        new Not(new In(Left, Right)).Evaluate(values);
}

public sealed record And(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (BooleanValue { IsTrue: true }, BooleanValue { IsTrue: true }) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Or(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (BooleanValue { IsTrue: true }, _) or (_, BooleanValue { IsTrue: true }) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Not(Expression Operand) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Operand.Evaluate(values) switch
        {
            BooleanValue { IsTrue: true } => BooleanValue.False,
            _ => BooleanValue.True,
        };
}

public sealed record If(Expression Condition, Expression Then, Expression Otherwise) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Condition.Evaluate(values) switch
        {
            BooleanValue { IsTrue: true } => Then.Evaluate(values),
            _ => Otherwise.Evaluate(values),
        };
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

public sealed record TableLookup(Table Table, EquatableList<Expression> Keys, string Column) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        var evaluatedKeys = Keys.Select(k => k.Evaluate(values));
        return Table.Lookup([.. evaluatedKeys], Column);
    }
}

public sealed record ListExpression(EquatableList<Expression> Expressions) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Value.List(Expressions.Select(e => e.Evaluate(values)));
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
