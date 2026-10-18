using System.Globalization;
using F.Configurator.Catalog;
using F.Configurator.Dsl.Syntax;
using F.Configurator.Expressions;

namespace F.Configurator.Dsl;

internal sealed partial class PackageCompiler
{
    private static readonly string[] OptionColumns = ["id", "symbol", "nazwa", "obraz", "erp"];

    private const NumberStyles NumberStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private void CheckOptions(FeatureSyntax feature)
    {
        var ids = new HashSet<string>();
        foreach (var option in feature.Options.Where(option => !ids.Add(option.Id)))
        {
            Report(option.Span, $"opcja „{option.Id}” jest już w cesze „{feature.Name}”");
        }

        foreach (var path in feature.OptionFiles.Where(files.ContainsKey))
        {
            CheckFile(feature, OptionsFile.Read(path, files[path]), ids);
        }

        foreach (var attribute in feature.Attributes)
        {
            if (attribute.Default is { } value && WrongValue(attribute, value) is { } message)
            {
                Report(value.Span, message);
            }
        }

        foreach (var property in feature.Options.SelectMany(option => option.Properties))
        {
            CheckOptionProperty(feature, property);
        }

        if (feature.Type != "wybór")
        {
            foreach (var attribute in feature.Attributes)
            {
                Report(attribute.Span, "atrybuty mają tylko cechy typu wybór");
            }

            foreach (var option in feature.Options)
            {
                Report(
                    option.Span,
                    feature.Type == "tak/nie" ? "cecha tak/nie ma zawsze opcje TAK i NIE" : "opcje mają tylko cechy typu wybór");
            }
        }
    }

    private void CheckOptionProperty(FeatureSyntax feature, PropertySyntax property)
    {
        var value = property.Values[0];
        if (property.Name is "symbol" or "obraz")
        {
            if (property.Values is not [TextSyntax])
            {
                Report(value.Span, $"po „{property.Name}” oczekiwano tekstu w cudzysłowie");
            }
        }
        else if (AttributeOf(feature, property.Name) is not { } attribute)
        {
            Report(property.Span, $"„{property.Name}” nie jest atrybutem cechy „{feature.Name}”");
        }
        else if (property.Values.Count > 1)
        {
            Report(property.Values[1].Span, $"atrybut „{attribute.Name}” ma jedną wartość");
        }
        else if (WrongValue(attribute, value) is { } message)
        {
            Report(value.Span, message);
        }
    }

    private void CheckFile(FeatureSyntax feature, OptionsFile file, HashSet<string> ids)
    {
        if (file.Header is not { } header)
        {
            Report(new SourceSpan(file.Path, 1, 1, 0), "plik opcji jest pusty");
            return;
        }

        if (header.Cells.All(cell => cell.Text != "id"))
        {
            Report(header.Span, "w pliku opcji brak kolumny „id”");
            return;
        }

        var columns = new HashSet<string>();
        foreach (var column in header.Cells)
        {
            if (!columns.Add(column.Text))
            {
                Report(column.Span, $"kolumna „{column.Text}” jest już w pliku");
            }
            else if (!OptionColumns.Contains(column.Text) && AttributeOf(feature, column.Text) is null)
            {
                Report(column.Span, $"kolumna „{column.Text}” nie jest atrybutem cechy „{feature.Name}”");
            }
        }

        foreach (var row in file.Rows)
        {
            if (row.Cells.Count != header.Cells.Count)
            {
                Report(row.Span, $"wiersz ma {row.Cells.Count} kolumny, a nagłówek {header.Cells.Count}");
                continue;
            }

            foreach (var (column, cell) in header.Cells.Zip(row.Cells))
            {
                var message = column.Text switch
                {
                    "id" when cell.Text.Length == 0 => "opcja bez id",
                    "id" when !ids.Add(cell.Text) => $"opcja „{cell.Text}” jest już w cesze „{feature.Name}”",
                    _ when cell.Text.Length > 0 && AttributeOf(feature, column.Text) is { } attribute => WrongCell(attribute, cell.Text),
                    _ => null,
                };
                if (message is not null)
                {
                    Report(cell.Span, message);
                }
            }
        }
    }

    // Options written in place come first: the syntax keeps them apart from the files.
    private ChoiceFeatureBuilder Choice(ChoiceFeatureBuilder choice, FeatureSyntax feature)
    {
        choice = feature.Attributes.Aggregate(choice, (builder, attribute) => builder.Attribute(
            attribute.Name, TypeOf(attribute), attribute.Default is { } value ? AttributeValue(attribute, value) : null));
        choice = feature.Options.Aggregate(choice, (builder, option) => builder.Option(
            option.Id, option.Label ?? option.Id, optionBuilder => Option(feature, option, optionBuilder)));
        foreach (var file in feature.OptionFiles.Select(path => OptionsFile.Read(path, files[path])))
        {
            var columns = file.Header!.Cells.Select(cell => cell.Text).ToList();
            foreach (var row in file.Rows)
            {
                var cells = columns.Zip(row.Cells, (column, cell) => (column, cell.Text)).ToDictionary();
                choice = choice.Option(
                    cells["id"],
                    cells.GetValueOrDefault("nazwa") is { Length: > 0 } label ? label : cells["id"],
                    optionBuilder => FileOption(feature, optionBuilder, cells));
            }
        }

        return choice;
    }

    private static FeatureOptionBuilder Option(FeatureSyntax feature, OptionSyntax option, FeatureOptionBuilder builder)
    {
        builder = option.Properties.Aggregate(builder, (optionBuilder, property) => property.Name switch
        {
            "symbol" => optionBuilder.Symbol(((TextSyntax)property.Values[0]).Value),
            "obraz" => optionBuilder.Image(((TextSyntax)property.Values[0]).Value),
            _ => optionBuilder.Attribute(property.Name, AttributeValue(AttributeOf(feature, property.Name)!, property.Values[0])),
        });
        return Defaults(feature, builder, attribute => option.Properties.Any(property => property.Name == attribute.Name));
    }

    // An empty cell is no value, so the attribute's default fills it.
    private static FeatureOptionBuilder FileOption(FeatureSyntax feature, FeatureOptionBuilder builder, Dictionary<string, string> cells)
    {
        foreach (var (column, cell) in cells.Where(cell => cell.Value.Length > 0))
        {
            builder = column switch
            {
                "id" or "nazwa" => builder,
                "symbol" => builder.Symbol(cell),
                "obraz" => builder.Image(cell),
                "erp" => builder.ExternalCode(cell),
                _ => builder.Attribute(column, CellValue(AttributeOf(feature, column)!, cell)),
            };
        }

        return Defaults(feature, builder, attribute => cells.GetValueOrDefault(attribute.Name) is { Length: > 0 });
    }

    private static FeatureOptionBuilder Defaults(
        FeatureSyntax feature, FeatureOptionBuilder builder, Func<AttributeSyntax, bool> hasValue) =>
        feature.Attributes
            .Where(attribute => attribute.Default is not null && !hasValue(attribute))
            .Aggregate(builder, (optionBuilder, attribute) => optionBuilder.Attribute(attribute.Name, AttributeValue(attribute, attribute.Default!)));

    private static AttributeSyntax? AttributeOf(FeatureSyntax feature, string name) =>
        feature.Attributes.FirstOrDefault(attribute => attribute.Name == name);

    private static AttributeType TypeOf(AttributeSyntax attribute) => attribute.Type switch
    {
        "liczba" => AttributeType.Number,
        "tak/nie" => AttributeType.Boolean,
        _ => AttributeType.Text,
    };

    // Decision E5 (2026-10-14): a tak/nie value is the option TAK or NIE, as for a tak/nie feature.
    private static string? WrongValue(AttributeSyntax attribute, ExpressionSyntax value) => attribute.Type switch
    {
        "liczba" when NumberOf([value]) is null => $"atrybut „{attribute.Name}” jest liczbą, a „{TextOf(value)}” nie",
        "tak/nie" when value is not NameSyntax { Name: "TAK" or "NIE" } => $"atrybut „{attribute.Name}” przyjmuje TAK albo NIE",
        "tekst" when TextOf(value) is null => $"atrybut „{attribute.Name}” przyjmuje nazwę, tekst albo liczbę",
        _ => null,
    };

    private static string? WrongCell(AttributeSyntax attribute, string cell) => attribute.Type switch
    {
        "liczba" when NumberOf(cell) is null => $"atrybut „{attribute.Name}” jest liczbą, a „{cell}” nie",
        "tak/nie" when cell is not ("TAK" or "NIE") => $"atrybut „{attribute.Name}” przyjmuje TAK albo NIE",
        _ => null,
    };

    private static Value AttributeValue(AttributeSyntax attribute, ExpressionSyntax value) => attribute.Type switch
    {
        "liczba" => Value.Number(NumberOf([value])!.Value),
        "tak/nie" => Value.Option(((NameSyntax)value).Name),
        _ => Value.Text(TextOf(value)!),
    };

    private static Value CellValue(AttributeSyntax attribute, string cell) => attribute.Type switch
    {
        "liczba" => Value.Number(NumberOf(cell)!.Value),
        "tak/nie" => Value.Option(cell),
        _ => Value.Text(cell),
    };

    private static decimal? NumberOf(IReadOnlyList<ExpressionSyntax> values) => values switch
    {
        [NumberSyntax number] => number.Value,
        [UnarySyntax { Operator: "-", Operand: NumberSyntax number }] => -number.Value,
        _ => null,
    };

    // A file holds numbers with a decimal point, so `40,5` is not 405 by grouping.
    private static decimal? NumberOf(string cell) =>
        decimal.TryParse(cell, NumberStyle, CultureInfo.InvariantCulture, out var number) ? number : null;

    private static string? TextOf(ExpressionSyntax value) => value switch
    {
        TextSyntax text => text.Value,
        NameSyntax name => name.Name,
        _ => NumberOf([value])?.ToString(CultureInfo.InvariantCulture),
    };
}
