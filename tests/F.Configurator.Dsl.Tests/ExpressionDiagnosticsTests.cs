using static F.Configurator.Dsl.Tests.Parsing;

namespace F.Configurator.Dsl.Tests;

// Grammar 7: an expression that cannot be read gives a message in Polish that points at the place, and no tree.
public class ExpressionDiagnosticsTests
{
    private static Diagnostic Error(SourceSpan span, string message) => new(Severity.Error, span, message, null);

    [Fact]
    public void Empty_source_is_not_an_expression()
    {
        var result = ParseExpression("");

        Assert.Null(result.Syntax);
        Assert.Equal([Error(At(1, 1, 0), "oczekiwano wyrażenia")], result.Diagnostics);
    }

    [Fact]
    public void Operator_without_a_right_side_is_an_error_at_the_end_of_the_line()
    {
        var result = ParseExpression("A +");

        Assert.Null(result.Syntax);
        Assert.Equal([Error(At(1, 4, 0), "oczekiwano wyrażenia")], result.Diagnostics);
    }

    [Fact]
    public void Keyword_that_is_not_a_value_is_an_error()
    {
        Assert.Equal([Error(At(1, 5, 5), "oczekiwano wyrażenia")], ParseExpression("A = nazwa").Diagnostics);
    }

    [Fact]
    public void Missing_closing_parenthesis_is_an_error()
    {
        Assert.Equal([Error(At(1, 7, 0), "brak „)”")], ParseExpression("(A + B").Diagnostics);
    }

    [Fact]
    public void Missing_closing_bracket_of_a_list_is_an_error()
    {
        Assert.Equal([Error(At(1, 12, 0), "brak „]”")], ParseExpression("A w [CZ, SK").Diagnostics);
    }

    [Fact]
    public void Missing_closing_parenthesis_of_a_function_is_an_error()
    {
        Assert.Equal([Error(At(1, 10, 0), "brak „)”")], ParseExpression("wybrana(A").Diagnostics);
    }

    [Fact]
    public void Function_without_parentheses_is_an_error()
    {
        Assert.Equal([Error(At(1, 9, 1), "po „wybrana” oczekiwano „(”")], ParseExpression("wybrana A").Diagnostics);
    }

    [Fact]
    public void Dot_without_a_name_is_an_error()
    {
        Assert.Equal([Error(At(1, 7, 0), "po „.” oczekiwano nazwy")], ParseExpression("Model.").Diagnostics);
    }

    [Fact]
    public void Value_after_a_whole_expression_is_an_error()
    {
        var result = ParseExpression("A = X Y");

        Assert.Null(result.Syntax);
        Assert.Equal([Error(At(1, 7, 1), "nieoczekiwane „Y” po wyrażeniu")], result.Diagnostics);
    }

    [Fact]
    public void Condition_without_then_is_an_error()
    {
        Assert.Equal([Error(At(1, 9, 7), "brak „to”")], ParseExpression("jeśli A inaczej 1").Diagnostics);
    }

    [Fact]
    public void Condition_without_otherwise_is_an_error()
    {
        Assert.Equal([Error(At(1, 13, 0), "brak „inaczej”")], ParseExpression("jeśli A to 1").Diagnostics);
    }

    // `A < B < C` reads as `(A < B) < C`, which compares a yes/no with a number; the author meant `A < B i B < C`.
    [Fact]
    public void Comparisons_in_a_row_are_an_error()
    {
        Assert.Equal(
            [Error(At(1, 7, 1), "porównań nie można łączyć, użyj „i”")],
            ParseExpression("A < B < C").Diagnostics);
    }

    // Grammar 6.1: a condition is the weakest operator, so as a part of a sum it needs parentheses.
    [Fact]
    public void Condition_inside_a_sum_needs_parentheses()
    {
        Assert.Equal([Error(At(1, 7, 5), "oczekiwano wyrażenia")], ParseExpression("\"S\" + jeśli A to 1 inaczej 0").Diagnostics);
    }

    [Fact]
    public void Problems_of_the_lexer_come_first()
    {
        var result = ParseExpression("A @ B");

        Assert.Null(result.Syntax);
        Assert.Equal(
            [
                Error(At(1, 3, 1), "nieznany znak „@”"),
                Error(At(1, 5, 1), "nieoczekiwane „B” po wyrażeniu"),
            ],
            result.Diagnostics);
    }
}
