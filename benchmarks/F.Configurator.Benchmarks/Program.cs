using BenchmarkDotNet.Running;

// Run by hand, outside `dotnet test` and CI:
// dotnet run -c Release --project benchmarks/F.Configurator.Benchmarks -- --filter '*'
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
