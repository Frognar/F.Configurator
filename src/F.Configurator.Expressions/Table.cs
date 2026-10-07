namespace F.Configurator.Expressions;

public sealed record Table(string[] KeyColumns, TableRow[] Rows);

public sealed record TableRow(Value[] Keys, IReadOnlyDictionary<string, Value> Values);
