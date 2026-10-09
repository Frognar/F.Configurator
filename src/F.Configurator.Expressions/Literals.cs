namespace F.Configurator.Expressions;

public sealed record Number(decimal Amount) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Number(Amount);
}

public sealed record Text(string Content) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Text(Content);
}

public sealed record Option(string Id) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Option(Id);
}

public sealed record Range(decimal Lower, decimal Upper) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Range(Lower, Upper);
}

public sealed record ListExpression(EquatableList<Expression> Expressions) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Value.List(Expressions.Select(e => e.Evaluate(values)));
}
