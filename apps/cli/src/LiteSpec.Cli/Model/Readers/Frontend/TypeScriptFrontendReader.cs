using LiteSpec.Model;

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
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="code">The code model the screens are checked against.</param>
    /// <returns>The screen read built from this frontend's facts.</returns>
    public ScreenRead Read(ConsumerConfig config, CodeModel code)
    {
        var facts = run.Value.FirstOrDefault(entry => entry.Name == frontend.Name)
            ?? FrontendFacts.Failed(
                frontend.Name, Finding.TypeScriptReader,
                $"the TypeScript reader returned no facts for frontend '{frontend.Name}'.");

        return FrontendReadBuilder.Build(config, frontend, facts, code);
    }
}
