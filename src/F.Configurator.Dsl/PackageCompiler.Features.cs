using F.Configurator.Catalog;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

internal sealed partial class PackageCompiler
{
    private static readonly string[] CommonProperties = ["nazwa", "opis", "prezentacja", "obraz", "wymagana"];

    private static readonly string[] NumberProperties = [.. CommonProperties, "jednostka", "min", "maks", "krok"];

    private static readonly string[] TextProperties = [.. CommonProperties, "długość", "znaki", "wzorzec"];

    private static readonly Dictionary<string, Presentation> Presentations = new()
    {
        ["lista"] = Presentation.List,
        ["przyciski"] = Presentation.Buttons,
        ["kafelki"] = Presentation.Tiles,
        ["próbki"] = Presentation.Swatches,
        ["pole"] = Presentation.Field,
        ["suwak"] = Presentation.Slider,
    };

    // A feature with an error is left out of the catalog, so building it can trust its syntax.
    private CatalogBuilder Features(CatalogBuilder catalog, IEnumerable<FeatureSyntax> features)
    {
        var declared = new HashSet<string>();
        foreach (var feature in features)
        {
            if (!declared.Add(feature.Name))
            {
                Report(feature.Span, $"cecha „{feature.Name}” jest już zadeklarowana");
            }
            else if (feature.Type == "wielokrotny wybór")
            {
                Report(feature.Span, "„wielokrotny wybór” jeszcze nie działa w silniku");
            }
            else if (IsValid(feature))
            {
                catalog = Feature(catalog, feature);
            }
        }

        return catalog;
    }

    private bool IsValid(FeatureSyntax feature)
    {
        var count = _diagnostics.Count;
        foreach (var path in feature.OptionFiles.Where(path => !files.ContainsKey(path)))
        {
            Report(feature.Span, $"brak pliku „{path}”");
        }

        CheckProperties(feature);
        CheckOptions(feature);
        return _diagnostics.Count == count;
    }

    private void CheckProperties(FeatureSyntax feature)
    {
        var allowed = feature.Type switch
        {
            "liczba" => NumberProperties,
            "tekst" => TextProperties,
            _ => CommonProperties,
        };
        foreach (var property in feature.Properties)
        {
            if (!allowed.Contains(property.Name))
            {
                Report(property.Span, $"„{property.Name}” nie jest właściwością cechy typu {feature.Type} ({string.Join(", ", allowed)})");
            }
            else if (WrongProperty(property) is { } message)
            {
                Report(property.Values[0].Span, message);
            }
        }
    }

    private static string? WrongProperty(PropertySyntax property) => property.Name switch
    {
        "nazwa" or "opis" or "obraz" when property.Values is not [TextSyntax] => $"po „{property.Name}” oczekiwano tekstu w cudzysłowie",
        "prezentacja" when property.Values is not [NameSyntax name] || !Presentations.ContainsKey(name.Name) =>
            $"nieznana prezentacja „{(property.Values[0] as NameSyntax)?.Name}” ({string.Join(", ", Presentations.Keys)})",
        "wymagana" when property.Values is not [NameSyntax { Name: "TAK" or "NIE" }] => "po „wymagana” oczekiwano TAK albo NIE",
        "jednostka" when property.Values is not [NameSyntax] => "po „jednostka” oczekiwano nazwy jednostki, np. mm",
        "min" or "maks" or "krok" when NumberOf(property.Values) is null => $"po „{property.Name}” oczekiwano liczby",
        "długość" when property.Values is not [BinarySyntax { Operator: "..", Left: NumberSyntax, Right: NumberSyntax }] =>
            "po „długość” oczekiwano przedziału, np. 0..20",
        _ => null,
    };

    private CatalogBuilder Feature(CatalogBuilder catalog, FeatureSyntax feature)
    {
        var metadata = Metadata(feature);
        var optional = feature.Properties.Any(property => property is { Name: "wymagana", Values: [NameSyntax { Name: "NIE" }] });
        return feature.Type switch
        {
            "wybór" => catalog.Choice(feature.Name, f => Choice((optional ? f.Optional() : f).Metadata(metadata), feature)),
            "tak/nie" => catalog.Choice(
                feature.Name, f => (optional ? f.Optional() : f).Metadata(metadata).Option("TAK", "TAK").Option("NIE", "NIE")),
            "liczba" => catalog.Number(
                feature.Name, f => feature.Properties.Aggregate((optional ? f.Optional() : f).Metadata(metadata), Number)),
            _ => catalog.Text(
                feature.Name, f => feature.Properties.Aggregate((optional ? f.Optional() : f).Metadata(metadata), Text)),
        };
    }

    private static FeatureMetadata Metadata(FeatureSyntax feature)
    {
        ExpressionSyntax? Value(string name) => feature.Properties.FirstOrDefault(property => property.Name == name)?.Values[0];
        string? Text(string name) => (Value(name) as TextSyntax)?.Value;

        Presentation? presentation = Value("prezentacja") is NameSyntax name ? Presentations[name.Name] : null;
        return new FeatureMetadata(Text("nazwa"), Text("opis"), presentation, Text("obraz"));
    }

    private static NumberFeatureBuilder Number(NumberFeatureBuilder builder, PropertySyntax property) => property.Name switch
    {
        "jednostka" => builder.Unit(((NameSyntax)property.Values[0]).Name),
        "min" => builder.Min(NumberOf(property.Values)!.Value),
        "maks" => builder.Max(NumberOf(property.Values)!.Value),
        "krok" => builder.Step(NumberOf(property.Values)!.Value),
        _ => builder,
    };

    private TextFeatureBuilder Text(TextFeatureBuilder builder, PropertySyntax property)
    {
        switch (property)
        {
            case { Name: "znaki" or "wzorzec" }:
                Warn(property.Span, $"silnik jeszcze nie sprawdza „{property.Name}”, pomijam");
                return builder;
            case { Name: "długość", Values: [BinarySyntax { Left: NumberSyntax min, Right: NumberSyntax max }] }:
                return builder.Length((int)min.Value, (int)max.Value);
            default:
                return builder;
        }
    }
}
