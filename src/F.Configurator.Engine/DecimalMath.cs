namespace F.Configurator.Engine;

public class DecimalMath
{
    // Least common multiple of positive decimals: scale both to integers, use the integer LCM, scale back.
    public static decimal LeastCommonMultiple(decimal a, decimal b)
    {
        var scale = new decimal(1, 0, 0, false, Math.Max(a.Scale, b.Scale));
        var (x, y) = ((long)(a / scale), (long)(b / scale));
        return checked(x / GreatestCommonDivisor(x, y) * y) * scale;
    }

    private static long GreatestCommonDivisor(long a, long b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }
}
