namespace F.Configurator.Expressions;

public abstract record Expression
{
    public static Expression Number(decimal number) => new Number(number);
    public static Expression Add(Expression left, Expression right) => new Add(left, right);

    public abstract Value Evaluate();
}

public sealed record Number(decimal Amount) : Expression
{
    public override Value Evaluate() => new(Amount);
}

public sealed record Add(Expression Left, Expression Right) : Expression
{
    public override Value Evaluate() => new(Left.Evaluate().Amount + Right.Evaluate().Amount);
}
