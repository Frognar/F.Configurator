namespace F.Configurator.Expressions;

public abstract record Expression
{
    public abstract Value Evaluate(IReadOnlyDictionary<string, Value> values);

    public static Expression Number(decimal number) => new Number(number);
    public static Expression Reference(string name) => new Reference(name);
    public static Expression Range(decimal min, decimal max) => new Range(min, max);

    public static Expression Add(Expression left, Expression right) => new Add(left, right);
    public static Expression Subtract(Expression left, Expression right) => new Subtract(left, right);
    public static Expression Multiply(Expression left, Expression right) => new Multiply(left, right);
    public static Expression Divide(Expression left, Expression right) => new Divide(left, right);

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
}

public sealed record Number(decimal Amount) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => new NumberValue(Amount);
}

public sealed record Reference(string Name) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        values.TryGetValue(Name, out var value) ? value : Value.Missing;
}

public sealed record Range(decimal Min, decimal Max) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => new RangeValue(Min, Max);
}

public abstract record BinaryArithmetic(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        return (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue left, NumberValue right) => Apply(left.Amount, right.Amount),
            _ => Value.Missing
        };
    }

    protected abstract Value Apply(decimal left, decimal right);
}

public sealed record Add(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) => new NumberValue(left + right);
}

public sealed record Subtract(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) => new NumberValue(left - right);
}

public sealed record Multiply(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) => new NumberValue(left * right);
}

public sealed record Divide(Expression Left, Expression Right) : BinaryArithmetic(Left, Right)
{
    protected override Value Apply(decimal left, decimal right) =>
        right == decimal.Zero ? Value.Missing : new NumberValue(left / right);
}

public sealed record Equal(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (MissingValue, _) or (_, MissingValue) => BooleanValue.False,
            ({ } l, { } r) => new BooleanValue(l == r)
        };
}

public sealed record NotEqual(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (MissingValue, _) or (_, MissingValue) => BooleanValue.True,
            ({ } l, { } r) => new BooleanValue(l != r)
        };
}

public abstract record BinaryComparison(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        return (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue left, NumberValue right) => Compare(left.Amount, right.Amount),
            _ => BooleanValue.False
        };
    }

    protected abstract BooleanValue Compare(decimal left, decimal right);
}

public sealed record LessThan(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override BooleanValue Compare(decimal left, decimal right) => new(left < right);
}

public sealed record LessThanOrEqual(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override BooleanValue Compare(decimal left, decimal right) => new(left <= right);
}

public sealed record GreaterThan(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override BooleanValue Compare(decimal left, decimal right) => new(left > right);
}

public sealed record GreaterThanOrEqual(Expression Left, Expression Right) : BinaryComparison(Left, Right)
{
    protected override BooleanValue Compare(decimal left, decimal right) => new(left >= right);
}

public sealed record In(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue n, RangeValue r) => new BooleanValue(n.Amount >= r.Min && n.Amount <= r.Max),
            _ => BooleanValue.False,
        };
}

public sealed record NotIn(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        (Left.Evaluate(values), Right.Evaluate(values)) switch
        {
            (NumberValue n, RangeValue r) => new BooleanValue(n.Amount < r.Min || n.Amount > r.Max),
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
            BooleanValue { Value: true } => BooleanValue.False,
            BooleanValue { Value: false } => BooleanValue.True,
            _ => BooleanValue.False,
        };
}
