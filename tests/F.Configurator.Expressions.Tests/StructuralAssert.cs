namespace F.Configurator.Expressions.Tests;

// Records compare by content; equal objects must also hash alike, or dictionaries and sets break.
internal static class StructuralAssert
{
    public static void Equal<T>(T first, T second) where T : notnull
    {
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
