using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The scoping options of <c>litespec model check</c>: the <c>--only-paths</c> paths a
///     parallel worker owns and the <c>--module</c> name the backend read is limited to.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class ModelOptions
{
    /// <summary>The <c>--only-paths</c> flag: every following path belongs to it until the next option.</summary>
    const string OnlyPathsFlag = "--only-paths";

    /// <summary>
    ///     Splits the <c>--only-paths</c> paths out of the check arguments, leaving the rest
    ///     for the option parser. The flag takes any number of paths and repeats, so
    ///     <c>--only-paths a b --only-paths c</c> names three paths.
    /// </summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The named paths with the remaining arguments and the usage error, or null when the flag was well formed.</returns>
    public static (IReadOnlyList<string> Paths, string[] Remaining, string? Error) SplitOnlyPaths(string[] args)
    {
        var paths = new List<string>();
        var rest = new List<string>(args.Length);
        string? error = null;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (arg == OnlyPathsFlag)
            {
                var taken = 0;

                while (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    paths.Add(args[++index]);
                    taken++;
                }

                if (taken == 0)
                {
                    error ??= "Option --only-paths needs a value.";
                }

                continue;
            }

            if (arg.StartsWith(OnlyPathsFlag + "=", StringComparison.Ordinal))
            {
                var value = arg[(OnlyPathsFlag.Length + 1)..];

                if (value.Length == 0)
                {
                    error ??= "Option --only-paths needs a value.";
                }
                else
                {
                    paths.Add(value);
                }

                continue;
            }

            rest.Add(arg);
        }

        return (paths, [.. rest], error);
    }

    /// <summary>
    ///     Roots one named path at the consumer: an absolute path stays, a relative path
    ///     resolves under the root, with forward slashes read as folder separators.
    /// </summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="path">The named path, relative or absolute.</param>
    /// <returns>The absolute normalized path.</returns>
    public static string Rooted(ConsumerConfig config, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(config.Root, path.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    ///     Whether the finding belongs to the named paths: its file is one of them or sits
    ///     under one of them, or its subject record's file does. A finding on no record and
    ///     no named file belongs to no path, so a scoped check omits it.
    /// </summary>
    /// <param name="model">The built project model resolving the finding subject.</param>
    /// <param name="config">The consumer configuration the named paths resolve against.</param>
    /// <param name="finding">The finding to test.</param>
    /// <param name="roots">The named paths, rooted at the consumer.</param>
    /// <returns>True when the finding sits on the named paths or their records.</returns>
    public static bool InOnlyPaths(ProjectModel model, ConsumerConfig config, Finding finding, IReadOnlyList<string> roots)
    {
        if (UnderAny(finding.Path, roots))
        {
            return true;
        }

        if (finding.Subject is not null
            && model.ById.TryGetValue(Identifiers.BaseId(finding.Subject), out var record)
            && record.File.Length > 0
            && UnderAny(record.File, roots))
        {
            return true;
        }

        return false;
    }

    /// <summary>Whether the file is one of the rooted paths or sits under one of them.</summary>
    /// <param name="file">The finding or record file path.</param>
    /// <param name="roots">The named paths, rooted at the consumer.</param>
    /// <returns>True when the file equals a named path or sits under one.</returns>
    static bool UnderAny(string file, IReadOnlyList<string> roots)
    {
        if (file.Length == 0)
        {
            return false;
        }

        var full = Path.GetFullPath(file);

        foreach (var root in roots)
        {
            var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

            if (full.Equals(root, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Whether a module folder and a module name spell one module: case, hyphens and
    ///     underscores ignored, so <c>Library.Lending</c> keeps <c>lending</c>.
    /// </summary>
    /// <param name="folder">The module folder name as written on disk.</param>
    /// <param name="name">The module name the check keeps.</param>
    /// <returns>True when both names agree with case, hyphens and underscores ignored; otherwise false.</returns>
    public static bool ModuleMatches(string folder, string name)
    {
        var left = folder.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var right = name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Whether the backend file belongs to the module: the module layout names the
    ///     module folder first. A file under the Kernel, the Platform, a host or the
    ///     composition folder belongs to no module, so a module read skips it.
    /// </summary>
    /// <param name="config">The consumer configuration holding each module root.</param>
    /// <param name="file">The full path of the backend file.</param>
    /// <param name="module">The module name the read keeps.</param>
    /// <returns>True when the file sits under the module's projects.</returns>
    public static bool BelongsToModule(ConsumerConfig config, string file, string module) =>
        SourceRoots.ModulePath(config, file) is { Length: > 0 } segments && ModuleMatches(segments[0], module);
}
