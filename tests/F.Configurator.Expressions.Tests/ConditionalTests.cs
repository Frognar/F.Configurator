namespace F.Configurator.Expressions.Tests;

// Grammar 6.1 level 1: `jeśli … to … inaczej …`.
public class ConditionalTests
{
    private static readonly Expression FrameAllowance = If(
        GreaterThan(Reference("SzerokoscMM"), Number(1000m)),
        Number(70m),
        Number(0m));

    [Fact]
    public void True_condition_evaluates_to_the_then_branch()
    {
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(1100m) };

        var value = FrameAllowance.Evaluate(values);

        Assert.Equal(Value.Number(70m), value);
    }

    [Fact]
    public void False_condition_evaluates_to_the_otherwise_branch()
    {
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = FrameAllowance.Evaluate(values);

        Assert.Equal(Value.Number(0m), value);
    }

    // Grammar 6.4: a condition with a missing value is false, so `inaczej` applies.
    [Fact]
    public void Missing_condition_evaluates_to_the_otherwise_branch()
    {
        var value = FrameAllowance.Evaluate(NoValues);

        Assert.Equal(Value.Number(0m), value);
    }
}
