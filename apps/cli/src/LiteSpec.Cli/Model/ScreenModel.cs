namespace LiteSpec.Model;

/// <summary>One generated operation: its name, method and path.</summary>
sealed record GeneratedOperation(string Name, string Method, string Path);

/// <summary>One screen call: the operation a screen called and the use case it reaches.</summary>
sealed record ScreenCall(string Operation, string UseCase);

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
    ///     The paired calls: each operation with the use case it reaches, as the
    ///     reader resolved it. Empty when the pairing is unknown, so a use case
    ///     listed only in <see cref="UseCases" /> keeps an empty operation.
    /// </summary>
    public IReadOnlyList<ScreenCall> Calls { get; init; } = [];

    /// <summary>
    ///     Every calls link this screen makes: one per paired call, plus one
    ///     with an empty operation per use case no paired call reaches.
    /// </summary>
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

/// <summary>One browser test and what it proves, reaches and asserts.</summary>
sealed record BrowserTestInfo(
    string Frontend,
    string Title,
    string File,
    int Line,
    IReadOnlyList<string> Proves,
    string? Journey,
    IReadOnlyList<string> Gotos,
    IReadOnlyList<string> Codes,
    IReadOnlyList<string> Texts,
    string Reader = Finding.TypeScriptReader);

/// <summary>What the screen read found: route screens and file comments.</summary>
sealed record ScreenRead(
    IReadOnlyList<ScreenInfo> Screens,
    IReadOnlyList<CodeComment> Comments)
{
    /// <summary>The browser tests every frontend reader found.</summary>
    public IReadOnlyList<BrowserTestInfo> BrowserTests { get; init; } = [];

    /// <summary>Findings the readers themselves produced, such as <c>reader-missing</c>.</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];

    /// <summary>Whether the TypeScript reader's handshake matched, so it is qualified.</summary>
    public bool TypeScriptQualified { get; init; }
}
