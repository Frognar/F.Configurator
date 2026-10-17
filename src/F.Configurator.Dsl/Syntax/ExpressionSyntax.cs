namespace F.Configurator.Dsl.Syntax;

public abstract record ExpressionSyntax(SourceSpan Span);

public sealed record NumberSyntax(decimal Value, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record TextSyntax(string Value, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record NameSyntax(string Name, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record MemberSyntax(ExpressionSyntax Target, string Member, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record LookupSyntax(string Table, IReadOnlyList<ExpressionSyntax> Keys, SourceSpan Span)
    : ExpressionSyntax(Span);

public sealed record CallSyntax(string Function, IReadOnlyList<ExpressionSyntax> Arguments, SourceSpan Span)
    : ExpressionSyntax(Span);

public sealed record ListSyntax(IReadOnlyList<ExpressionSyntax> Items, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record UnarySyntax(string Operator, ExpressionSyntax Operand, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record BinarySyntax(string Operator, ExpressionSyntax Left, ExpressionSyntax Right, SourceSpan Span)
    : ExpressionSyntax(Span);

public sealed record ConditionalSyntax(
    ExpressionSyntax Condition,
    ExpressionSyntax Then,
    ExpressionSyntax Otherwise,
    SourceSpan Span) : ExpressionSyntax(Span);
