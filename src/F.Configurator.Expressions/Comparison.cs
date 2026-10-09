namespace F.Configurator.Expressions;

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
