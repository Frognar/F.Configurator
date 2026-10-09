namespace F.Configurator.Expressions;

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
