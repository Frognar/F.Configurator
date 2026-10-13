namespace F.Configurator.Expressions;

public sealed record Reference(string Name) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        values.TryGetValue(Name, out var value) ? value : Value.Missing;
}

public sealed record DefaultOf(string Feature) : Expression
{
    public static string KeyOf(string feature) => $"default({feature})";

    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        values.GetValueOrDefault(KeyOf(Feature), Value.Missing);
}

public sealed record ContextValue(string Context, string? Attribute) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) => Value.Missing;

    public override Value Evaluate(
        IReadOnlyDictionary<string, Value> values,
        IReadOnlyDictionary<string, Value> context) =>
        context.TryGetValue(Attribute is null ? Context : $"{Context}.{Attribute}", out var value)
            ? value
            : Value.Missing;
}

public sealed record OptionAttribute(
    Expression Selected,
    EquatableDictionary<string, Value> ValuesByOption) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        Selected.Evaluate(values) switch
        {
            OptionValue option => ValuesByOption.TryGetValue(option.Id, out var value) ? value : Value.Missing,
            _ => Value.Missing,
        };
}

public sealed record TableLookup(Table Table, EquatableList<Expression> Keys, string Column) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values)
    {
        var evaluatedKeys = Keys.Select(k => k.Evaluate(values));
        return Table.Lookup([.. evaluatedKeys], Column);
    }

    public override Value Evaluate(
        IReadOnlyDictionary<string, Value> values,
        IReadOnlyDictionary<string, Value> context) =>
        Table.Lookup([.. Keys.Select(k => k.Evaluate(values, context))], Column);
}
