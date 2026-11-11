using LiteSpec.Model;

namespace LiteSpec.Model.Readers.Frontend;

/// <summary>
///     Reads one Svelte or React frontend through the shared TypeScript reader
///     run: the Node reader runs once for every web frontend, and this reader
///     takes its own frontend's facts from that run.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed class TypeScriptFrontendReader : IFrontendReader
{
    /// <summary>The frontend this reader takes its facts for.</summary>
    private readonly FrontendConfig frontend;

    /// <summary>The shared Node reader run every web frontend takes its facts from.</summary>
    private readonly Task<IReadOnlyList<FrontendFacts>> run;

    /// <summary>Initializes a new instance of the <see cref="TypeScriptFrontendReader"/> class for one web frontend.</summary>
    /// <param name="frontend">The frontend this reader takes its facts for.</param>
    /// <param name="run">The shared Node reader run every web frontend takes its facts from.</param>
    public TypeScriptFrontendReader(FrontendConfig frontend, Task<IReadOnlyList<FrontendFacts>> run)
    {
        this.frontend = frontend;
        this.run = run;
    }

    /// <summary>Reads this frontend's screens, module marks and browser tests.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="code">The code model the screens are checked against.</param>
    /// <returns>The screen read built from this frontend's facts.</returns>
    public ScreenRead Read(ConsumerConfig config, CodeModel code)
    {
        var facts = run.GetAwaiter().GetResult().FirstOrDefault(entry => entry.Name == frontend.Name)
            ?? FrontendFacts.Failed(
                frontend.Name, Finding.TypeScriptReader,
                $"the TypeScript reader returned no facts for frontend '{frontend.Name}'.");

        return FrontendReadBuilder.Build(config, frontend, facts, code);
    }
}
