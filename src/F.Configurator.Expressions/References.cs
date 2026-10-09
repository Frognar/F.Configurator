namespace F.Configurator.Expressions;

public sealed record Reference(string Name) : Expression
{
    public override Value Evaluate(IReadOnlyDictionary<string, Value> values) =>
        values.TryGetValue(Name, out var value) ? value : Value.Missing;
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
}
