using LiteSpec.Cli.Model.Code;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Screens;

/// <summary>One read route: its address, file, operations, reached use cases and paired calls.</summary>
sealed record CodeScreen(
    string Frontend,
    string Route,
    string File,
    IReadOnlyList<GeneratedOperation> Operations,
    IReadOnlyList<string> UseCases,
    string? ScreenId = null,
    string Reader = Finding.TypeScriptReader,
    int Line = 1)
{
    /// <summary>
    ///     Gets the paired calls: each operation with the use case it reaches, as the
    ///     reader resolved it. Empty when the pairing is unknown, so a use case
    ///     listed only in <see cref="UseCases" /> keeps an empty operation.
    /// </summary>
    public IReadOnlyList<ScreenCall> Calls { get; init; } = [];

    /// <summary>
    ///     Gets the reached use cases only the route's layouts call: they count the
    ///     use case reached and satisfy an offer, but never require the screen to
    ///     offer them. Empty when the reader records no layout origin, so every
    ///     reached use case counts as the route's own.
    /// </summary>
    public IReadOnlyList<string> LayoutOnlyUseCases { get; init; } = [];

    /// <summary>Every use case the route's own files reach: the reached use cases without the ones only a layout reaches.</summary>
    /// <returns>The reached use cases without the layout-only ones, in reached order.</returns>
    public IReadOnlyList<string> OwnUseCases() =>
        [.. UseCases.Where(useCase => !LayoutOnlyUseCases.Contains(useCase, StringComparer.Ordinal))];

    /// <summary>
    ///     Every calls link this screen makes: one per paired call, plus one
    ///     with an empty operation per use case no paired call reaches.
    /// </summary>
    /// <returns>The deduplicated paired calls, followed by one empty-operation call per unpaired use case.</returns>
    public IReadOnlyList<ScreenCall> PairedCalls()
    {
        var emitted = new HashSet<(string Operation, string UseCase)>();
        var pairedUseCases = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ScreenCall>();

        foreach (var call in Calls)
        {
            pairedUseCases.Add(call.UseCase);

            if (emitted.Add((call.Operation, call.UseCase)))
            {
                result.Add(call);
            }
        }

        foreach (var useCase in UseCases)
        {
            if (pairedUseCases.Contains(useCase))
            {
                continue;
            }

            if (emitted.Add(("", useCase)))
            {
                result.Add(new ScreenCall("", useCase));
            }
        }

        return result;
    }
}
