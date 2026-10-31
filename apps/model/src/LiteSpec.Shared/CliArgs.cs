namespace LiteSpec;

/// <summary>
///     The parsed arguments of one command. An option takes its value as
///     <c>--name value</c> or <c>--name=value</c>; a flag stands alone. Anything
///     the command does not declare is a usage error, as are a repeated option
///     and an empty value for <c>--root</c>, <c>--model</c>, <c>--out</c> or
///     <c>--editor</c>. A usage error is reported before <c>--help</c> is
///     honoured. The repeatable options are <c>--batch</c> and
///     <c>--batches</c>: <c>model approve</c> collects every batch from the
///     raw arguments, so a repeated batch option is not an error.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public sealed class CliArgs
{
    /// <summary>Options whose empty value is a usage error.</summary>
    static readonly HashSet<string> NonEmptyOptions = new(StringComparer.Ordinal) { "--root", "--model", "--out", "--editor" };

    /// <summary>Options a command collects repeatedly from the raw arguments.</summary>
    static readonly HashSet<string> RepeatableOptions = new(StringComparer.Ordinal) { "--batch", "--batches" };

    /// <summary>The option values by option name.</summary>
    readonly Dictionary<string, string> values = new(StringComparer.Ordinal);
    /// <summary>The flags that were passed.</summary>
    readonly HashSet<string> flags = new(StringComparer.Ordinal);

    /// <summary>Gets the arguments that are not options, in order.</summary>
    public List<string> Positionals { get; } = [];

    /// <summary>Gets the usage error, or null when the arguments parsed.</summary>
    public string? Error { get; private set; }

    /// <summary>Gets a value indicating whether true when <c>--help</c> or <c>-h</c> was passed.</summary>
    public bool Help { get; private set; }

    /// <summary>
    ///     Parses <paramref name="args"/> against the declared flags and value options.
    ///     Names include their dashes, such as <c>--check</c> or <c>--root</c>.
    /// </summary>
    /// <param name="args">The raw arguments.</param>
    /// <param name="flagNames">The declared flag names, with their dashes.</param>
    /// <param name="optionNames">The declared value option names, with their dashes.</param>
    /// <param name="maxPositionals">The most positional arguments the command takes.</param>
    /// <returns>The parsed arguments, with any usage error stated.</returns>
    public static CliArgs Parse(string[] args, IEnumerable<string> flagNames, IEnumerable<string> optionNames, int maxPositionals = 0)
    {
        var result = new CliArgs();
        var knownFlags = flagNames.ToHashSet(StringComparer.Ordinal);
        var knownOptions = optionNames.ToHashSet(StringComparer.Ordinal);
        var seenOptions = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (arg is "--help" or "-h")
            {
                result.Help = true;
                continue;
            }

            if (!arg.StartsWith('-') || arg == "-")
            {
                if (result.Positionals.Count >= maxPositionals)
                {
                    result.Error ??= $"Unexpected argument '{arg}'.";
                }

                result.Positionals.Add(arg);
                continue;
            }

            var equals = arg.IndexOf('=');
            var name = equals > 0 ? arg[..equals] : arg;

            if (knownFlags.Contains(name) && equals < 0)
            {
                result.flags.Add(name);
            }
            else if (knownOptions.Contains(name))
            {
                if (!seenOptions.Add(name) && !RepeatableOptions.Contains(name))
                {
                    result.Error ??= $"Option {name} is repeated.";
                }

                if (equals > 0)
                {
                    var value = arg[(equals + 1)..];

                    if (value.Length == 0 && NonEmptyOptions.Contains(name))
                    {
                        result.Error ??= $"Option {name} needs a value.";
                    }

                    result.values[name] = value;
                }
                else if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    var value = args[++index];

                    if (value.Length == 0 && NonEmptyOptions.Contains(name))
                    {
                        result.Error ??= $"Option {name} needs a value.";
                    }

                    result.values[name] = value;
                }
                else
                {
                    result.Error ??= $"Option {name} needs a value.";
                }
            }
            else
            {
                result.Error ??= $"Unknown option '{arg}'.";
            }
        }

        if (result.Error is not null)
        {
            result.Help = false;
        }

        return result;
    }

    /// <summary>True when the flag was passed.</summary>
    /// <param name="flag">The flag name, with its dashes.</param>
    /// <returns>True when the caller passed the flag on the command line; otherwise false.</returns>
    public bool Has(string flag) => flags.Contains(flag);

    /// <summary>The option's value, or null when it was not passed.</summary>
    /// <param name="option">The option name, with its dashes.</param>
    /// <returns>The option value, or null when it was not passed.</returns>
    public string? Value(string option) => values.GetValueOrDefault(option);

    /// <summary>Gets a value indicating whether true when <c>--format json</c> or <c>--format=json</c> was passed.</summary>
    public bool Json => Value("--format") == "json";

    /// <summary>Gets a usage error for a <c>--format</c> value other than <c>json</c> or <c>text</c>.</summary>
    public string? FormatError => Value("--format") is null or "json" or "text" ? null : $"Unknown format '{Value("--format")}'; use json or text.";

    /// <summary>The <c>--root</c> value as a full path, or the working directory.</summary>
    /// <param name="fallback">The root to use when no root option was passed.</param>
    /// <returns>The root as a full path.</returns>
    public string Root(string? fallback = null) =>
        Path.GetFullPath(Value("--root") ?? fallback ?? Directory.GetCurrentDirectory());
}
