namespace F.Configurator.Expressions;

public record Expression(decimal N)
{
    public static Expression Number(decimal number) => new(number);
    public Value Evaluate() => new(N);
}
