namespace F.Configurator.Expressions;

public record Value(decimal Amount)
{
    public static Value Number(decimal number) => new(number);
}
