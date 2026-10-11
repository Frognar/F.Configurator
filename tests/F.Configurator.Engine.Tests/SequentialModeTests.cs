namespace F.Configurator.Engine.Tests;

// Analysis 3.2 and grammar 4.5: in sequential mode (`tryb kolejny`) the configurator asks stage by
// stage, in the order of the collection. A configuration is an immutable state: setting a value
// returns a new state.
public class SequentialModeTests
{
    private static Configuration Start(string collectionName = "Basic")
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Choice("Kierunek", feature => feature.Option("L", "Lewe").Option("P", "Prawe"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Collection("Basic", collection => collection
                .Stage("Model", "Model", "Kierunek")
                .Stage("Wymiary", "SzerokoscMM"))
            .Build();

        return Configuration.Start(catalog, collectionName);
    }

    [Fact]
    public void Configuration_starts_at_the_first_stage()
    {
        var configuration = Start();

        Assert.Equal("Model", configuration.CurrentStage);
    }

    // Later stages stay hidden until the user reaches them.
    [Fact]
    public void Only_features_of_the_first_stage_are_visible_at_start()
    {
        var configuration = Start();

        Assert.Equal(["Model", "Kierunek"], configuration.VisibleFeatures);
    }

    [Fact]
    public void Features_have_no_values_at_start()
    {
        var configuration = Start();

        Assert.Equal(Value.Missing, configuration.ValueOf("Model"));
    }

    [Fact]
    public void Starting_an_unknown_collection_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Start("Brilliant"));
    }

    [Fact]
    public void Setting_a_value_returns_a_state_with_that_value()
    {
        var configuration = Start().Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Option("PORTA"), configuration.ValueOf("Model"));
    }

    [Fact]
    public void Setting_a_value_leaves_the_previous_state_unchanged()
    {
        var start = Start();

        start.Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Missing, start.ValueOf("Model"));
    }

    [Fact]
    public void Stage_with_a_feature_left_stays_current()
    {
        var configuration = Start().Set("Model", Value.Option("PORTA"));

        Assert.Equal("Model", configuration.CurrentStage);
        Assert.Equal(["Model", "Kierunek"], configuration.VisibleFeatures);
    }

    // Every feature of the stage has a value, so the next stage opens and its features become visible.
    [Fact]
    public void Completed_stage_opens_the_next_one()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("Kierunek", Value.Option("L"));

        Assert.Equal("Wymiary", configuration.CurrentStage);
        Assert.Equal(["Model", "Kierunek", "SzerokoscMM"], configuration.VisibleFeatures);
    }

    // The user may go back and change an earlier answer.
    [Fact]
    public void Value_of_an_earlier_stage_can_be_changed()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("Kierunek", Value.Option("L"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("VERTE"), configuration.ValueOf("Model"));
    }

    // In sequential mode a feature can be set only once its stage is reached.
    [Fact]
    public void Setting_a_feature_of_a_later_stage_is_rejected()
    {
        var configuration = Start();

        Assert.Throws<InvalidOperationException>(() => configuration.Set("SzerokoscMM", Value.Number(900m)));
    }

    [Fact]
    public void Setting_an_unknown_feature_is_rejected()
    {
        var configuration = Start();

        Assert.Throws<ArgumentException>(() => configuration.Set("Kolor", Value.Option("BIALY")));
    }

    // The UI offers only the options of a choice feature; anything else is a programmer error.
    [Fact]
    public void Option_the_feature_does_not_have_is_rejected()
    {
        var configuration = Start();

        Assert.Throws<ArgumentException>(() => configuration.Set("Model", Value.Option("ANATOLIA")));
    }

    [Fact]
    public void Value_of_the_wrong_type_is_rejected()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("Kierunek", Value.Option("L"));

        Assert.Throws<ArgumentException>(() => configuration.Set("SzerokoscMM", Value.Text("900")));
    }

    [Fact]
    public void Number_feature_keeps_a_number()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("Kierunek", Value.Option("L"))
            .Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal(Value.Number(900m), configuration.ValueOf("SzerokoscMM"));
    }
}
