using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;

namespace LiteSpec.Model.Readers.Frontend;

/// <summary>
///     Reads one Blazor or Android frontend through the .NET screen reader,
///     which stays qualified.
/// </summary>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed class DotnetFrontendReader(FrontendConfig frontend) : IFrontendReader
{
    /// <summary>Reads this frontend's route entries and their screen marks.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="code">The code model the screens are checked against.</param>
    /// <returns>The screen read built from the .NET screen reader routes and problems.</returns>
    public ScreenRead Read(ConsumerConfig config, CodeModel code)
    {
        var screenRead = DotnetScreenReader.ReadWithProblems(frontend);
        var facts = new FrontendFacts(
            frontend.Name,
            Finding.RoslynReader,
            Qualified: true,
            Failure: null,
            Modules: [],
            Routes: screenRead.Routes,
            Tests: [],
            Diagnostics: []);

        var built = FrontendReadBuilder.Build(config, frontend, facts, code);

        return screenRead.Problems.Count == 0
            ? built
            : built with { Findings = [.. built.Findings, .. screenRead.Problems] };
    }
}
