namespace F.Configurator.Expressions;

public record Value(decimal N)
{
    public static Value Number(decimal number) => new(number);
}
