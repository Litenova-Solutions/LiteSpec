using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;

namespace LiteSpec.Model.Readers.Frontend;

/// <summary>
///     Reads one Svelte or React frontend through the shared TypeScript reader
///     run: the Node reader runs once for every web frontend, and this reader
///     takes its own frontend's facts from that run.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed class TypeScriptFrontendReader(
    FrontendConfig frontend,
    Lazy<IReadOnlyList<FrontendFacts>> run) : IFrontendReader
{
    /// <summary>Reads this frontend's screens, module marks and browser tests.</summary>
    public ScreenRead Read(ConsumerConfig config, CodeModel code)
    {
        var facts = run.Value.FirstOrDefault(entry => entry.Name == frontend.Name)
            ?? FrontendFacts.Failed(
                frontend.Name, Finding.TypeScriptReader,
                $"the TypeScript reader returned no facts for frontend '{frontend.Name}'.");

        return FrontendReadBuilder.Build(config, frontend, facts, code);
    }
}

/// <summary>
///     Reads one Blazor or Android frontend through the .NET screen reader,
///     which stays qualified.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed class DotnetFrontendReader(FrontendConfig frontend) : IFrontendReader
{
    /// <summary>Reads this frontend's route entries and their screen marks.</summary>
    public ScreenRead Read(ConsumerConfig config, CodeModel code)
    {
        var facts = new FrontendFacts(
            frontend.Name,
            Finding.RoslynReader,
            Qualified: true,
            Failure: null,
            Modules: [],
            Routes: DotnetScreenReader.Read(frontend),
            Tests: [],
            Diagnostics: []);

        return FrontendReadBuilder.Build(config, frontend, facts, code);
    }
}
