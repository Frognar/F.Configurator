using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed record Catalog(
    string Name,
    string? Version,
    Reaction? DefaultReaction,
    ImmutableDictionary<string, Context> Contexts,
    ImmutableList<Feature> Features,
    ImmutableList<Collection> Collections,
    ImmutableList<Rule> Rules,
    ImmutableDictionary<string, Table> Tables,
    ImmutableDictionary<string, AllowedCombinationsTable> AllowedCombinations);

public sealed class CatalogBuilder
{
    private readonly Catalog _catalog;

    private CatalogBuilder(Catalog catalog) => _catalog = catalog;

    public static CatalogBuilder Create(string name) =>
        new(new Catalog(name,
            null,
            null,
            ImmutableDictionary<string, Context>.Empty,
            ImmutableList<Feature>.Empty,
            ImmutableList<Collection>.Empty,
            ImmutableList<Rule>.Empty,
            ImmutableDictionary<string, Table>.Empty,
            ImmutableDictionary<string, AllowedCombinationsTable>.Empty));

    public CatalogBuilder Version(string version) => new(_catalog with { Version = version });

    public CatalogBuilder DefaultReaction(Reaction reaction) => new(_catalog with { DefaultReaction = reaction });

    public CatalogBuilder Context(string name, Func<ContextBuilder, ContextBuilder> setupContext) =>
        _catalog.Contexts.ContainsKey(name)
            ? throw Duplicate("Context", name)
            : new CatalogBuilder(_catalog with
            {
                Contexts = _catalog.Contexts.Add(name, setupContext(ContextBuilder.Create(name)).Build())
            });

    private CatalogBuilder AddFeature(Feature feature) =>
        _catalog.Features.Any(f => f.Name == feature.Name)
            ? throw Duplicate("Feature", feature.Name)
            : new CatalogBuilder(_catalog with { Features = _catalog.Features.Add(feature) });

    public CatalogBuilder Choice(string name, Func<ChoiceFeatureBuilder, ChoiceFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(ChoiceFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Number(string name, Func<NumberFeatureBuilder, NumberFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(NumberFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Boolean(string name) =>
        AddFeature(new BooleanFeature(name));

    public CatalogBuilder Text(string name) =>
        AddFeature(new TextFeature(name));

    public CatalogBuilder Computed(string name, Expression expression) =>
        AddFeature(new ComputedFeature(name, expression));

    public CatalogBuilder Collection(string name, Func<CollectionBuilder, CollectionBuilder> setupCollection) =>
        _catalog.Collections.Any(c => c.Name == name)
            ? throw Duplicate("Collection", name)
            : new CatalogBuilder(_catalog with
            {
                Collections = _catalog.Collections.Add(setupCollection(CollectionBuilder.Create(name)).Build())
            });

    public CatalogBuilder Rule(string name, Func<RuleBuilder, RuleBuilder> setupRule) =>
        new(_catalog with
        {
            Rules = _catalog.Rules.Add(setupRule(RuleBuilder.Create(name)).Build())
        });

    public CatalogBuilder Table(string name, Table table) =>
        TableExists(name)
            ? throw Duplicate("Table", name)
            : new CatalogBuilder(_catalog with { Tables = _catalog.Tables.Add(name, table) });

    public CatalogBuilder AllowedCombinations(string name,
        Func<AllowedCombinationsTableBuilder, AllowedCombinationsTableBuilder> setupTable) =>
        TableExists(name)
            ? throw Duplicate("Table", name)
            : new CatalogBuilder(_catalog with
            {
                AllowedCombinations =
                _catalog.AllowedCombinations.Add(name, setupTable(AllowedCombinationsTableBuilder.Create()).Build())
            });

    private bool TableExists(string name) =>
        _catalog.Tables.ContainsKey(name) || _catalog.AllowedCombinations.ContainsKey(name);

    private static ArgumentException Duplicate(string kind, string name) =>
        new($"{kind} with name '{name}' already exists.");

    public Catalog Build() => _catalog;
}

public sealed record Context(string Name, ImmutableList<AttributeDeclaration> Attributes);

public sealed record AttributeDeclaration(string Name, AttributeType Type, Value? Value);

public enum AttributeType
{
    Text,
    Boolean
}

public sealed class ContextBuilder
{
    private readonly Context _context;
    private ContextBuilder(Context context) => _context = context;

    public static ContextBuilder Create(string name) =>
        new(new Context(name, ImmutableList<AttributeDeclaration>.Empty));

    public ContextBuilder Attribute(string name, AttributeType type) =>
        new(_context with { Attributes = _context.Attributes.Add(new AttributeDeclaration(name, type, null)) });

    public Context Build() => _context;
}
