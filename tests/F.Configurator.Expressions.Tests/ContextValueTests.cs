namespace F.Configurator.Expressions.Tests;

// Grammar 4.6 and decision 2026-10-13: the context given by the platform (the contractor with its attributes, the
// language) is read by a node of its own, from a map of its own, so a context value and a feature never mix.
public class ContextValueTests
{
    private static readonly IReadOnlyDictionary<string, Value> Context = new Dictionary<string, Value>
    {
        ["Kontrahent"] = Value.Text("13-WEBICO"),
        ["Kontrahent.Asortyment"] = Value.Text("PREMIUM"),
        ["Kontrahent.Rynek"] = Value.Text("PL"),
        ["Kontrahent.Rabat"] = Value.Number(10m),
        ["Kontrahent.Hurt"] = Value.Boolean(true),
        ["Język"] = Value.Text("pl"),
    };

    [Fact]
    public void Context_value_reads_an_attribute_of_the_context()
    {
        var value = ContextValue("Kontrahent", "Asortyment").Evaluate(TestValues.NoValues, Context);

        Assert.Equal(Value.Text("PREMIUM"), value);
    }

    // Grammar 4.6: `Kontrahent` (the contractor's id) and `Język` are built in.
    [Fact]
    public void Context_value_reads_a_value_of_the_context_itself()
    {
        Assert.Equal(Value.Text("13-WEBICO"), ContextValue("Kontrahent").Evaluate(TestValues.NoValues, Context));
        Assert.Equal(Value.Text("pl"), ContextValue("Język").Evaluate(TestValues.NoValues, Context));
    }

    [Fact]
    public void Context_value_the_platform_did_not_give_is_missing()
    {
        var value = ContextValue("Kontrahent", "Asortyment").Evaluate(TestValues.NoValues, TestValues.NoValues);

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Context_value_is_not_read_from_the_features()
    {
        var values = new Dictionary<string, Value> { ["Kontrahent.Asortyment"] = Value.Text("PREMIUM") };

        var value = ContextValue("Kontrahent", "Asortyment").Evaluate(values, TestValues.NoValues);

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Feature_is_not_read_from_the_context()
    {
        var value = Reference("Kontrahent.Asortyment").Evaluate(TestValues.NoValues, Context);

        Assert.Equal(Value.Missing, value);
    }

    // An expression evaluated without a context sees an empty one.
    [Fact]
    public void Expression_evaluated_without_a_context_has_no_context_values()
    {
        Assert.Equal(Value.Missing, ContextValue("Kontrahent", "Asortyment").Evaluate(TestValues.NoValues));
    }

    private static readonly Table Discounts = TableBuilder.Create(["Asortyment"], ["Procent"])
        .Row([Value.Text("PREMIUM")], [Value.Number(15m)])
        .Build();

    private static readonly EquatableDictionary<string, Value> Groups = new Dictionary<string, Value>
    {
        ["H"] = Value.Text("Hurt"),
        ["D"] = Value.Text("Detal"),
    }.ToEquatableDictionary();

    // Each node passes the context on to the nodes inside it.
    public static TheoryData<Expression, Value> ContextInside => new()
    {
        { Equal(ContextValue("Kontrahent", "Asortyment"), Text("PREMIUM")), Value.Boolean(true) },
        { NotEqual(ContextValue("Kontrahent", "Asortyment"), Text("PREMIUM")), Value.Boolean(false) },
        { GreaterThan(ContextValue("Kontrahent", "Rabat"), Number(5m)), Value.Boolean(true) },
        { In(ContextValue("Kontrahent", "Rynek"), List(Text("PL"), Text("CZ"))), Value.Boolean(true) },
        { In(Text("PL"), List(ContextValue("Kontrahent", "Rynek"))), Value.Boolean(true) },
        { NotIn(ContextValue("Kontrahent", "Rynek"), List(Text("PL"), Text("CZ"))), Value.Boolean(false) },
        { And(ContextValue("Kontrahent", "Hurt"), Equal(Number(1m), Number(1m))), Value.Boolean(true) },
        { Or(ContextValue("Kontrahent", "Hurt"), Equal(Number(1m), Number(2m))), Value.Boolean(true) },
        { Not(ContextValue("Kontrahent", "Hurt")), Value.Boolean(false) },
        { Add(ContextValue("Kontrahent", "Rabat"), Number(5m)), Value.Number(15m) },
        { Negate(ContextValue("Kontrahent", "Rabat")), Value.Number(-10m) },
        { If(ContextValue("Kontrahent", "Hurt"), Text("H"), Text("D")), Value.Text("H") },
        { Min(ContextValue("Kontrahent", "Rabat"), Number(20m)), Value.Number(10m) },
        { Round(ContextValue("Kontrahent", "Rabat"), Number(4m)), Value.Number(12m) },
        { Length(ContextValue("Kontrahent", "Asortyment")), Value.Number(7m) },
        { Pad(ContextValue("Kontrahent", "Rabat"), Number(4m)), Value.Text("0010") },
        { TableLookup(Discounts, [ContextValue("Kontrahent", "Asortyment")], "Procent"), Value.Number(15m) },
        { OptionAttribute(If(ContextValue("Kontrahent", "Hurt"), Option("H"), Option("D")), Groups), Value.Text("Hurt") },
    };

    [Theory]
    [MemberData(nameof(ContextInside))]
    public void Context_reaches_every_node(Expression expression, Value expected)
    {
        Assert.Equal(expected, expression.Evaluate(TestValues.NoValues, Context));
    }
}
