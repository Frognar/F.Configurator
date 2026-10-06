namespace F.Configurator.Expressions;

public record Expression
{
    public static Expression Number(decimal number) => new();
    public Value Evaluate() => new();
}
