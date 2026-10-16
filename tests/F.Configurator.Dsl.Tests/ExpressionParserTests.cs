using static F.Configurator.Dsl.Tests.Parsing;

namespace F.Configurator.Dsl.Tests;

// Grammar 6: the parser builds a syntax tree of an expression. Names stay names: whether `PL` is an option, `Model` a
// feature or `Kontrahent` a context is decided later, when the whole package is known.
public class ExpressionParserTests
{
    [Theory]
    [InlineData("1044", "1044")]
    [InlineData("0.5", "0.5")]
    [InlineData("007", "7")]
    public void Number_is_a_number(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Fact]
    public void Text_is_a_text()
    {
        Assert.Equal("\"26MM SOFT\"", Tree("\"26MM SOFT\""));
    }

    [Theory]
    [InlineData("SzerokoscMM")]
    [InlineData("PL")]
    [InlineData("TAK")]
    public void Name_is_a_name(string source)
    {
        Assert.Equal(source, Tree(source));
    }

    [Fact]
    public void Dot_reads_a_member()
    {
        Assert.Equal("(. Model Seria)", Tree("Model.Seria"));
    }

    [Fact]
    public void Brackets_after_a_name_read_a_table_row()
    {
        Assert.Equal(
            "(. ([] Wymiary Norma Szerokosc) SzerokoscMM)",
            Tree("Wymiary[Norma, Szerokosc].SzerokoscMM"));
    }

    [Fact]
    public void Multiplication_binds_tighter_than_addition()
    {
        Assert.Equal("(+ A (* B 2))", Tree("A + B * 2"));
    }

    [Fact]
    public void Parentheses_group_first()
    {
        Assert.Equal("(* (+ A B) 2)", Tree("(A + B) * 2"));
    }

    [Theory]
    [InlineData("A - B - C", "(- (- A B) C)")]
    [InlineData("A / B * C", "(* (/ A B) C)")]
    [InlineData("A + B - C", "(- (+ A B) C)")]
    public void Arithmetic_groups_from_the_left(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Theory]
    [InlineData("-5", "(- 5)")]
    [InlineData("-A * B", "(* (- A) B)")]
    [InlineData("A - -5", "(- A (- 5))")]
    public void Minus_before_a_value_negates_it(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Theory]
    [InlineData("=")]
    [InlineData("≠")]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public void Comparison_compares_two_sides(string @operator)
    {
        Assert.Equal($"({@operator} A (+ B 1))", Tree($"A {@operator} B + 1"));
    }

    [Fact]
    public void Comparison_written_another_way_is_the_same_comparison()
    {
        Assert.Equal("(≠ A B)", Tree("A != B"));
    }

    [Fact]
    public void List_is_its_items_in_brackets()
    {
        Assert.Equal("(w Norma [CZ SK])", Tree("Norma w [CZ, SK]"));
    }

    [Fact]
    public void List_may_hold_texts_and_numbers()
    {
        Assert.Equal("(w Szerokosc [\"100\" 110])", Tree("Szerokosc w [\"100\", 110]"));
    }

    [Fact]
    public void Not_in_is_one_operator()
    {
        Assert.Equal("(nie w FelcSkrzydla [\"26MM\"])", Tree("FelcSkrzydla nie w [\"26MM\"]"));
    }

    [Fact]
    public void Range_is_two_values_around_two_dots()
    {
        Assert.Equal("(w Odchylka (.. (- 5) (- 1)))", Tree("Odchylka w -5..-1"));
    }

    [Theory]
    [InlineData("A i B lub C", "(lub (i A B) C)")]
    [InlineData("A lub B i C", "(lub A (i B C))")]
    [InlineData("A lub B lub C", "(lub (lub A B) C)")]
    public void And_binds_tighter_than_or(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Theory]
    [InlineData("nie A = X", "(nie (= A X))")]
    [InlineData("nie A i B", "(i (nie A) B)")]
    [InlineData("nie nie A", "(nie (nie A))")]
    [InlineData("nie (A i B)", "(nie (i A B))")]
    public void Not_binds_looser_than_a_comparison_and_tighter_than_and(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Fact]
    public void Comparisons_join_with_and_or_or()
    {
        Assert.Equal("(lub (i (= A X) (≠ B Y)) (< C 5))", Tree("A = X i B ≠ Y lub C < 5"));
    }

    [Fact]
    public void Condition_gives_one_of_two_values()
    {
        Assert.Equal("(jeśli (= A X) 10 0)", Tree("jeśli A = X to 10 inaczej 0"));
    }

    [Fact]
    public void Condition_may_follow_another_condition()
    {
        Assert.Equal(
            "(jeśli (= A X) \"T\" (jeśli (= B Y) \"S\" \"N\"))",
            Tree("jeśli A = X to \"T\" inaczej jeśli B = Y to \"S\" inaczej \"N\""));
    }

    [Fact]
    public void Condition_in_parentheses_is_a_value()
    {
        Assert.Equal(
            "(+ \"SPŁ\" (jeśli (wybrana Zamek) (+ \"Z\" Zamek) \"\"))",
            Tree("\"SPŁ\" + (jeśli wybrana(Zamek) to \"Z\" + Zamek inaczej \"\")"));
    }

    [Theory]
    [InlineData("domyślna(SzerokoscMM)", "(domyślna SzerokoscMM)")]
    [InlineData("domyslna(SzerokoscMM)", "(domyślna SzerokoscMM)")]
    [InlineData("wybrana(Kolor.Krawedz)", "(wybrana (. Kolor Krawedz))")]
    [InlineData("dopełnij(Szerokosc, 4)", "(dopełnij Szerokosc 4)")]
    [InlineData("dopełnij(Szerokosc, 4, \"0\")", "(dopełnij Szerokosc 4 \"0\")")]
    [InlineData("zaokrąglij(A / 3)", "(zaokrąglij (/ A 3))")]
    [InlineData("zaokrąglij(A, 5)", "(zaokrąglij A 5)")]
    [InlineData("min(A, B, C)", "(min A B C)")]
    [InlineData("maks(A, 2050)", "(maks A 2050)")]
    [InlineData("długość(Grawer)", "(długość Grawer)")]
    public void Function_takes_its_arguments_in_parentheses(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Fact]
    public void Function_result_is_a_value_like_any_other()
    {
        Assert.Equal("(- (domyślna SzerokoscMM) 100)", Tree("domyślna(SzerokoscMM) - 100"));
    }

    [Fact]
    public void Not_before_a_function()
    {
        Assert.Equal("(nie (wybrana A))", Tree("nie wybrana(A)"));
    }

    // The lexer joins lines that go on; the parser sees one line.
    [Fact]
    public void Expression_may_go_on_in_the_next_lines()
    {
        Assert.Equal(
            "(+ (+ \"SPŁ\" Model) Typ)",
            Tree("\"SPŁ\" + Model\n    + Typ"));
    }

    [Fact]
    public void Expression_reads_like_a_door_rule()
    {
        Assert.Equal(
            "(jeśli (lub (≠ SzerokoscMM (domyślna SzerokoscMM)) (≠ WysokoscMM (domyślna WysokoscMM))) \"T\" \"N\")",
            Tree("jeśli SzerokoscMM ≠ domyślna(SzerokoscMM)\n    lub WysokoscMM ≠ domyślna(WysokoscMM) to \"T\" inaczej \"N\""));
    }
}
