namespace F.Configurator.Expressions;

public sealed record And(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(
        IReadOnlyDictionary<string, Value> values,
        IReadOnlyDictionary<string, Value> context) =>
        (Left.Evaluate(values, context), Right.Evaluate(values, context)) switch
        {
            (BooleanValue { IsTrue: true }, BooleanValue { IsTrue: true }) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Or(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate(
        IReadOnlyDictionary<string, Value> values,
        IReadOnlyDictionary<string, Value> context) =>
        (Left.Evaluate(values, context), Right.Evaluate(values, context)) switch
        {
            (BooleanValue { IsTrue: true }, _) or (_, BooleanValue { IsTrue: true }) => BooleanValue.True,
            _ => BooleanValue.False,
        };
}

public sealed record Not(Expression Operand) : Expression
{
    public override Value Evaluate(
        IReadOnlyDictionary<string, Value> values,
        IReadOnlyDictionary<string, Value> context) =>
        Operand.Evaluate(values, context) switch
        {
            BooleanValue { IsTrue: true } => BooleanValue.False,
            _ => BooleanValue.True,
        };
}

public sealed record If(Expression Condition, Expression Then, Expression Otherwise) : Expression
{
    public override Value Evaluate(
        IReadOnlyDictionary<string, Value> values,
        IReadOnlyDictionary<string, Value> context) =>
        Condition.Evaluate(values, context) switch
        {
            BooleanValue { IsTrue: true } => Then.Evaluate(values, context),
            _ => Otherwise.Evaluate(values, context),
        };
}
