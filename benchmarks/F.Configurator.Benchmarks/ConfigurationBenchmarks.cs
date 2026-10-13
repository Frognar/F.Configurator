using BenchmarkDotNet.Attributes;
using F.Configurator.Engine;
using F.Configurator.Results;

namespace F.Configurator.Benchmarks;

// Analysis 3.4: a step of the wizard (setting one value and settling the rules) should take below 10 ms.
[MemoryDiagnoser]
public class ConfigurationBenchmarks
{
    private ProductCatalog _catalog = null!;
    private Configuration _started = null!;
    private Configuration _filled = null!;
    private Value _firstChoice = null!;
    private Value _middleChoice = null!;

    [GlobalSetup]
    public void Setup()
    {
        _catalog = SyntheticCatalog.Build();
        _started = Configuration.Start(_catalog, SyntheticCatalog.Collection);
        _filled = Fill(_catalog, _started);
        _firstChoice = AnotherOption(_started, SyntheticCatalog.ChoiceName(0));
        _middleChoice = AnotherOption(_filled, SyntheticCatalog.ChoiceName(15));
    }

    [Benchmark]
    public Configuration Start() => Configuration.Start(_catalog, SyntheticCatalog.Collection);

    [Benchmark]
    public Configuration SetFirstChoice() => _started.Set(SyntheticCatalog.ChoiceName(0), _firstChoice);

    [Benchmark]
    public Configuration SetChoiceOfAFilledConfiguration() =>
        _filled.Set(SyntheticCatalog.ChoiceName(15), _middleChoice);

    [Benchmark]
    public Configuration SetNumberOfAFilledConfiguration() =>
        _filled.Set(SyntheticCatalog.NumberName(2), Value.Number(1234m));

    [Benchmark]
    public StepResult ResultOfAFilledConfiguration() => _filled.Result;

    // The user goes through every stage and picks the last option they may, so every rule has something to read.
    private static Configuration Fill(ProductCatalog catalog, Configuration configuration)
    {
        foreach (var feature in catalog.Features)
        {
            configuration = configuration.Set(feature.Name, feature switch
            {
                ChoiceFeature => Value.Option(configuration.AvailableOptions(feature.Name)[^1]),
                NumberFeature => Value.Number(1500m),
                _ => Value.Text("ABCDEF"),
            });
        }

        return configuration;
    }

    private static Value AnotherOption(Configuration configuration, string feature) =>
        Value.Option(configuration.AvailableOptions(feature).First(o => configuration.ValueOf(feature) != Value.Option(o)));
}
