namespace F.Configurator.Expressions;

public abstract record Expression
{
    public abstract Value Evaluate(IReadOnlyDictionary<string, Value> values);

    public static Expression Number(decimal number) => new Number(number);
    public static Expression Reference(string name) => new Reference(name);
    public static Expression Range(decimal min, decimal max) => new Range(min, max);
    public static Expression Text(string name) => new Text(name);

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
    public static Expression GreaterThanOrEqual(Expression left, Expression right) => new GreaterThanOrEqual(left, right);

    public static Expression In(Expression left, Expression right) => new In(left, right);
    public static Expression NotIn(Expression left, Expression right) => new NotIn(left, right);

    public static Expression And(Expression left, Expression right) => new And(left, right);
    public static Expression Or(Expression left, Expression right) => new Or(left, right);
    public static Expression Not(Expression operand) => new Not(operand);

    public static Expression Length(Expression operand) => new Length(operand);

    public static Expression If(Expression condition, Expression then, Expression otherwise)
        => new If(condition, then, otherwise);
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

public sealed record Range(decimal MinValue, decimal MaxValue) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Range(MinValue, MaxValue);
}

public sealed record Text(string TextValue) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Text(TextValue);
}

public abstract record BinaryArithmetic(Expression Left, Expression Right) : Expression
{
    public sealed override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Combine(Left.Evaluate(values), Right.Evaluate(values));

    protected virtual Value Combine(Value left, Value right) =>
        (left, right) switch
        {
            (NumberValue l, NumberValue r) => Apply(l.Value, r.Value),
            _ => Value.Missing,
        };

    protected abstract Value Apply(decimal left, decimal right);
}

public sealed record Add(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Combine(Value left, Value right) =>
        (left, right) switch
        {
            (TextValue l, TextValue r) => Value.Text(l.Value + r.Value),
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
            NumberValue { Value: var amount } => Value.Number(-amount),
            _ => Value.Missing,
        };
}

public abstract record Extremum(IReadOnlyList<Expression> Expressions) : Expression
{
    public sealed override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        var evaluated = Expressions
            .Select(expr => expr.Evaluate(values))
            .OfType<NumberValue>()
            .ToList();

        return evaluated.Count == Expressions.Count ? Pick(evaluated) ?? Value.Missing : Value.Missing;
    }

    protected abstract Value? Pick(IReadOnlyList<NumberValue> evaluated);
}

public sealed record Min(IReadOnlyList<Expression> Expressions) : Extremum(Expressions)
{
    protected override Value? Pick(IReadOnlyList<NumberValue> evaluated) => evaluated.MinBy(v => v.Value);
}

public sealed record Max(IReadOnlyList<Expression> Expressions) : Extremum(Expressions)
{
    protected override Value? Pick(IReadOnlyList<NumberValue> evaluated) => evaluated.MaxBy(v => v.Value);
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
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (MissingValue, _) or (_, MissingValue) => BooleanValue.True,
            ({ } l, { } r) => Value.Boolean(l != r),
        };
}

public abstract record BinaryComparison(Expression Left, Expression Right) : Expression
{
    public sealed override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        return (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue left, NumberValue right) => Compare(left.Value, right.Value),
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
            (NumberValue n, RangeValue r) => Value.Boolean(n.Value >= r.Min && n.Value <= r.Max),
            _ => BooleanValue.False,
        };
}

public sealed record NotIn(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue n, RangeValue r) => Value.Boolean(n.Value < r.Min || n.Value > r.Max),
            (MissingValue, _) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record And(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (BooleanValue { Value: true }, BooleanValue { Value: true }) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Or(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (BooleanValue { Value: true }, _) or (_, BooleanValue { Value: true }) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Not(Expression Operand) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Operand.Evaluate(values) switch
        {
            BooleanValue { Value: false } or MissingValue => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Length(Expression Operand) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Operand.Evaluate(values) switch
        {
            TextValue { Value: var str } => Value.Number(str.Length),
            _ => Value.Missing,
        };
}

public sealed record If(Expression Condition, Expression Then, Expression Otherwise) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Condition.Evaluate(values) switch
        {
            BooleanValue { Value: true } => Then.Evaluate(values),
            _ => Otherwise.Evaluate(values),
        };
}
