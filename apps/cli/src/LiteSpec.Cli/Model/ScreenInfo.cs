namespace LiteSpec.Model;

/// <summary>One read route: its address, file, operations, use cases and paired calls.</summary>
sealed record ScreenInfo(
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
