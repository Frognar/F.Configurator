namespace F.Configurator.Expressions;

public record Expression(decimal N)
{
    public static Expression Number(decimal number) => new(number);
    public static Expression Add(Expression left, Expression right) => new(left.N + right.N);
    public Value Evaluate() => new(N);
}
