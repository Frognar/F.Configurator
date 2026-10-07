namespace F.Configurator.Expressions;

public abstract record Expression
{
    public static Expression Number(decimal number) => new Number(number);
    public static Expression Add(Expression left, Expression right) => new Add(left, right);
    public static Expression Reference(string name) => new Reference(name);

    public abstract Value Evaluate();
    public abstract Value Evaluate(IReadOnlyDictionary<string, Value> values);
}

public sealed record Number(decimal Amount) : Expression
{
    public override Value Evaluate() => new(Amount);
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => new(Amount);
}

public sealed record Add(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate() => new(Left.Evaluate().Amount + Right.Evaluate().Amount);
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => new(Left.Evaluate(values).Amount + Right.Evaluate(values).Amount);
}

public sealed record Reference(string Name) : Expression
{
    public override Value Evaluate() => throw new NotImplementedException();
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        return values.TryGetValue(Name, out var value) ? value : Value.Missing;
    }
}
