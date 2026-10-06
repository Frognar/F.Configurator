namespace F.Configurator.Expressions;

public record Value
{
    public static Value Number(decimal number) => new();
}
