namespace F.Configurator.Expressions;

public abstract record Expression
{
    public abstract Value Evaluate(IReadOnlyDictionary<string, Value> values);

    public static Expression Number(decimal number) => new Number(number);
    public static Expression Text(string text) => new Text(text);
    public static Expression Option(string id) => new Option(id);
    public static Expression Range(decimal lower, decimal upper) => new Range(lower, upper);
    public static Expression List(Expression expression, params IEnumerable<Expression> expressions) =>
        new ListExpression([expression, .. expressions]);

    public static Expression Reference(string name) => new Reference(name);
    public static Expression OptionAttribute(Expression selected, EquatableDictionary<string, Value> valuesByOption) =>
        new OptionAttribute(selected, valuesByOption);

    public static Expression TableLookup(Table table, IEnumerable<Expression> keys, string column) =>
        new TableLookup(table, [.. keys], column);

    public static Expression Add(Expression left, Expression right) => new Add(left, right);
    public static Expression Subtract(Expression left, Expression right) => new Subtract(left, right);
    public static Expression Multiply(Expression left, Expression right) => new Multiply(left, right);
    public static Expression Divide(Expression left, Expression right) => new Divide(left, right);
    public static Expression Negate(Expression operand) => new Negate(operand);

    public static Expression Equal(Expression left, Expression right) => new Equal(left, right);
    public static Expression NotEqual(Expression left, Expression right) => new NotEqual(left, right);
    public static Expression LessThan(Expression left, Expression right) => new LessThan(left, right);
    public static Expression LessThanOrEqual(Expression left, Expression right) => new LessThanOrEqual(left, right);
    public static Expression GreaterThan(Expression left, Expression right) => new GreaterThan(left, right);
    public static Expression GreaterThanOrEqual(Expression left, Expression right) =>
        new GreaterThanOrEqual(left, right);

    public static Expression In(Expression left, Expression right) => new In(left, right);
    public static Expression NotIn(Expression left, Expression right) => new NotIn(left, right);

    public static Expression And(Expression left, Expression right) => new And(left, right);
    public static Expression Or(Expression left, Expression right) => new Or(left, right);
    public static Expression Not(Expression operand) => new Not(operand);
    public static Expression If(Expression condition, Expression then, Expression otherwise)
        => new If(condition, then, otherwise);


    public static Expression Min(Expression first, params IEnumerable<Expression> rest) => new Min([first, .. rest]);
    public static Expression Max(Expression first, params IEnumerable<Expression> rest) => new Max([first, .. rest]);
    public static Expression Round(Expression operand) => new Round(operand, Number(1));
    public static Expression Round(Expression operand, Expression step) => new Round(operand, step);
    public static Expression Length(Expression operand) => new Length(operand);
    public static Expression Pad(Expression operand, Expression totalWidth) =>
        new Pad(operand, totalWidth, Text("0"));

    public static Expression Pad(Expression operand, Expression totalWidth, Expression paddingChar) =>
        new Pad(operand, totalWidth, paddingChar);
}
