namespace F.Configurator.Dsl;

public sealed record ParseResult<T>(T? Syntax, IReadOnlyList<Diagnostic> Diagnostics)
    where T : class;
