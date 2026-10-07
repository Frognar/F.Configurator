namespace F.Configurator.Expressions;

public sealed record Table(IReadOnlyList<string> KeyColumns, IReadOnlyList<TableRow> Rows);

public sealed record TableRow(IReadOnlyList<IReadOnlyList<Value>> Keys, IReadOnlyDictionary<string, Value> Values);
