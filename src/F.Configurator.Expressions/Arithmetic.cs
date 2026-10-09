namespace F.Configurator.Expressions;

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
