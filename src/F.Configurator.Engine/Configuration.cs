namespace F.Configurator.Engine;

public sealed record Configuration
{
    public string CurrentStage { get; private set; } = "Model";

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        return new Configuration();
    }
}
