using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     The provision comparison behind <c>litespec-maintain changelog</c>: it reads the
///     provision index and the manifest at a git ref and in the working tree, lists the
///     added, removed, and renamed provision IDs with the added and removed extensions,
///     and writes those lists into the marked section of <c>CHANGELOG.md</c>.
/// </summary>
/// <follows>litespec/core/authoring#generate-the-provision-diff-for-each-release</follows>
static partial class ProvisionDiff
{
    /// <summary>The changelog that carries the marked provision-change section, relative to the repository root.</summary>
    public const string ChangelogPath = "CHANGELOG.md";

    /// <summary>The generated-region name of the provision-change section.</summary>
    public const string RegionName = "provision-diff";

    /// <summary>The marked provision-change section, wherever it sits in the changelog.</summary>
    static readonly Regex RegionPattern = new(
        "<!-- generated:" + Regex.Escape(RegionName) + " -->.*?<!-- /generated:" + Regex.Escape(RegionName) + " -->",
        RegexOptions.Singleline);

    /// <summary>One provision identity: the three fields the comparison reads from the provision data.</summary>
    /// <param name="Id">The provision identifier.</param>
    /// <param name="Page">The owning page, relative to the repository root.</param>
    /// <param name="Statement">The assertive line: the Statement, or the Default of a convention or pattern.</param>
    public sealed record Row(string Id, string Page, string Statement);

    /// <summary>One renamed provision: the retired identifier and the identifier that replaces it.</summary>
    /// <param name="From">The removed identifier.</param>
    /// <param name="To">The added identifier on the same page with the same statement.</param>
    public sealed record Rename(string From, string To);

    /// <summary>The comparison of the provision index and the extensions at a git ref with the current ones.</summary>
    /// <param name="Added">The added provision identifiers, in ordinal order.</param>
    /// <param name="Removed">The removed provision identifiers, in ordinal order.</param>
    /// <param name="Renamed">The renames, in ordinal order of the removed identifier.</param>
    /// <param name="ExtensionsAdded">The added extension names, in ordinal order.</param>
    /// <param name="ExtensionsRemoved">The removed extension names, in ordinal order.</param>
    public sealed record Result(
        IReadOnlyList<string> Added,
        IReadOnlyList<string> Removed,
        IReadOnlyList<Rename> Renamed,
        IReadOnlyList<string> ExtensionsAdded,
        IReadOnlyList<string> ExtensionsRemoved);

    /// <summary>
    ///     Reads the provision rows at a git ref: the generated index when the ref holds one,
    ///     otherwise the index rebuilt from the standards pages at that ref, so a release that
    ///     predates the generated index still compares.
    /// </summary>
    /// <param name="root">The repository root the command runs in.</param>
    /// <param name="fromRef">The git tag or commit to read from.</param>
    /// <returns>The provision identities at the ref, in index order.</returns>
    public static List<Row> RowsAtRef(string root, string fromRef)
    {
        try
        {
            return ReadRows(ShowAtRef(root, fromRef, Provisions.JsonPath), $"'{fromRef}:{Provisions.JsonPath}'");
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains(" not in ", StringComparison.Ordinal)
            || exception.Message.Contains("does not exist in", StringComparison.Ordinal))
        {
            var folder = Path.Combine(Path.GetTempPath(), "litespec-changelog-" + Guid.NewGuid().ToString("N"));
            var archive = folder + ".zip";

            try
            {
                RunGit(root, $"Cannot read the pages at '{fromRef}'", "archive", "--format=zip", "-o", archive, fromRef, "docs");
                System.IO.Compression.ZipFile.ExtractToDirectory(archive, folder);

                return Provisions.Collect(folder)
                    .Select(row => new Row(row.Id, row.Page, row.Statement))
                    .ToList();
            }
            finally
            {
                if (File.Exists(archive))
                {
                    File.Delete(archive);
                }

                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }
        }
    }

    /// <summary>Runs one git command in the repository and fails with its first error line.</summary>
    /// <param name="root">The repository root the command runs in.</param>
    /// <param name="failure">The message that opens the failure.</param>
    /// <param name="arguments">The git arguments.</param>
    static void RunGit(string root, string failure, params string[] arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root,
        };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        Process process;

        try
        {
            process = Process.Start(start)!;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"Cannot run git: {exception.Message}", exception);
        }

        using (process)
        {
            var stderr = process.StandardError.ReadToEndAsync();
            process.StandardOutput.ReadToEnd();

            if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidOperationException($"{failure}: git did not finish in two minutes.");
            }

            if (process.ExitCode != 0)
            {
                var detail = stderr.Result.Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0)
                    ?? $"git exited {process.ExitCode}.";
                throw new InvalidOperationException($"{failure}: {detail}");
            }
        }
    }

    /// <summary>Reads the file at a git ref with <c>git show</c>.</summary>
    /// <param name="root">The repository root the command runs in.</param>
    /// <param name="fromRef">The git tag or commit to read from.</param>
    /// <param name="relative">The file path under the root, with forward-slash segments.</param>
    /// <returns>The file text at the ref.</returns>
    public static string ShowAtRef(string root, string fromRef, string relative)
    {
        var start = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add("show");
        start.ArgumentList.Add($"{fromRef}:{relative}");

        Process process;

        try
        {
            process = Process.Start(start)!;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"Cannot run git: {exception.Message}", exception);
        }

        using (process)
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(TimeSpan.FromMinutes(1)))
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidOperationException($"Cannot read '{relative}' at '{fromRef}': git did not finish in one minute.");
            }

            if (process.ExitCode != 0)
            {
                var detail = stderr.Result.Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0)
                    ?? $"git show exited {process.ExitCode}.";
                throw new InvalidOperationException($"Cannot read '{relative}' at '{fromRef}': {detail}");
            }

            return stdout.Result;
        }
    }

    /// <summary>Reads the provision identities from one provision data text.</summary>
    /// <param name="json">The provision data text.</param>
    /// <param name="source">The source name used in failure messages.</param>
    /// <returns>The provision identities, in file order.</returns>
    public static List<Row> ReadRows(string json, string source)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("provisions", out var items) || items.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"{source} holds invalid provision data: it carries no provisions array.");
            }

            var rows = new List<Row>();

            foreach (var item in items.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"{source} holds invalid provision data: a provision entry is not an object.");
                }

                var id = Text(item, "id");

                // A row without an identifier cannot be compared, so it stays out of the
                // comparison instead of pairing by an empty name.
                if (id.Length == 0)
                {
                    continue;
                }

                rows.Add(new Row(id, Text(item, "page"), Text(item, "statement")));
            }

            return rows;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"{source} holds invalid provision data: {exception.Message}", exception);
        }
    }

    /// <summary>
    ///     Reads the extension names from provision rows: an extension is a page under an
    ///     <c>ext/</c> folder, other than its index. The provision index holds the same shape at
    ///     every release, so the old and the new extensions come from one kind of source.
    /// </summary>
    /// <param name="rows">The provision rows.</param>
    /// <returns>The extension names, in ordinal order.</returns>
    public static List<string> ExtensionsOf(IEnumerable<Row> rows) =>
        rows.Select(row => ExtensionPage().Match(row.Page))
            .Where(match => match.Success && match.Groups[1].Value != "README")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    /// <summary>The page path of an extension, capturing its name.</summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^docs/(?:.+/)?ext/([A-Za-z0-9-]+)\.md$", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionPage();

    /// <summary>Compares the old provision identities and extensions with the current ones.</summary>
    /// <param name="oldRows">The provision identities at the git ref.</param>
    /// <param name="newRows">The current provision identities.</param>
    /// <param name="oldExtensions">The extension names at the git ref.</param>
    /// <param name="newExtensions">The current extension names.</param>
    /// <returns>The added, removed, and renamed identifiers with the extension changes.</returns>
    public static Result Compare(
        IReadOnlyList<Row> oldRows,
        IReadOnlyList<Row> newRows,
        IReadOnlyCollection<string> oldExtensions,
        IReadOnlyCollection<string> newExtensions)
    {
        var oldById = new Dictionary<string, Row>(StringComparer.Ordinal);
        var newById = new Dictionary<string, Row>(StringComparer.Ordinal);

        foreach (var row in oldRows)
        {
            oldById[row.Id] = row;
        }

        foreach (var row in newRows)
        {
            newById[row.Id] = row;
        }

        var removed = oldById.Keys.Where(id => !newById.ContainsKey(id)).ToList();
        var added = newById.Keys.Where(id => !oldById.ContainsKey(id)).ToList();

        // A rename is a removed identifier and an added identifier that share one page
        // and one statement: the assertion stayed and only its name changed. Keys with
        // several identifiers pair in identifier order, so the pairing is stable.
        var removedByKey = removed.GroupBy(id => KeyOf(oldById[id])).ToDictionary(group => group.Key, group => group.OrderBy(id => id, StringComparer.Ordinal).ToList());
        var addedByKey = added.GroupBy(id => KeyOf(newById[id])).ToDictionary(group => group.Key, group => group.OrderBy(id => id, StringComparer.Ordinal).ToList());
        var renames = new List<Rename>();

        foreach (var key in removedByKey.Keys.OrderBy(key => key, KeyComparer))
        {
            if (!addedByKey.TryGetValue(key, out var targets))
            {
                continue;
            }

            var sources = removedByKey[key];
            var pairs = Math.Min(sources.Count, targets.Count);

            for (var index = 0; index < pairs; index++)
            {
                renames.Add(new Rename(sources[index], targets[index]));
            }
        }

        var renamedFrom = renames.Select(rename => rename.From).ToHashSet(StringComparer.Ordinal);
        var renamedTo = renames.Select(rename => rename.To).ToHashSet(StringComparer.Ordinal);
        renames.Sort((left, right) =>
            string.CompareOrdinal(left.From, right.From) is var order && order != 0 ? order : string.CompareOrdinal(left.To, right.To));

        return new Result(
            added.Where(id => !renamedTo.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToList(),
            removed.Where(id => !renamedFrom.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToList(),
            renames,
            newExtensions.Where(name => !oldExtensions.Contains(name)).OrderBy(name => name, StringComparer.Ordinal).ToList(),
            oldExtensions.Where(name => !newExtensions.Contains(name)).OrderBy(name => name, StringComparer.Ordinal).ToList());
    }

    /// <summary>Renders the marked provision-change section for the comparison, without a trailing newline.</summary>
    /// <param name="result">The comparison to render.</param>
    /// <param name="fromRef">The git tag or commit the comparison starts from.</param>
    /// <returns>The marked section text with <c>\n</c> line ends.</returns>
    public static string Render(Result result, string fromRef)
    {
        var lines = new List<string>
        {
            $"<!-- generated:{RegionName} -->",
            $"### Provision Changes Since {fromRef}",
            "",
        };
        Group(lines, "Added", result.Added.Select(id => $"`{id}`").ToList());
        Group(lines, "Removed", result.Removed.Select(id => $"`{id}`").ToList());
        Group(lines, "Renamed", result.Renamed.Select(rename => $"`{rename.From}` to `{rename.To}`").ToList());
        Group(lines, "Extensions added", result.ExtensionsAdded.Select(name => $"`{name}`").ToList());
        Group(lines, "Extensions removed", result.ExtensionsRemoved.Select(name => $"`{name}`").ToList());
        lines.Add($"<!-- /generated:{RegionName} -->");
        return string.Join("\n", lines);
    }

    /// <summary>Writes the marked section into the changelog text, with <c>\n</c> line ends and one trailing newline.</summary>
    /// <param name="changelog">The current changelog text.</param>
    /// <param name="block">The marked section text.</param>
    /// <returns>The changelog text with the section replaced in place, or added at the end of the topmost release section.</returns>
    public static string Apply(string changelog, string block)
    {
        var normalized = changelog.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!normalized.EndsWith('\n'))
        {
            normalized += "\n";
        }

        var match = RegionPattern.Match(normalized);

        if (match.Success)
        {
            return normalized[..match.Index] + block + normalized[(match.Index + match.Length)..];
        }

        var lines = normalized.TrimEnd('\n').Split('\n');
        var insertAt = lines.Length;
        var seenFirst = false;

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith("## ", StringComparison.Ordinal))
            {
                if (seenFirst)
                {
                    insertAt = index;
                    break;
                }

                seenFirst = true;
            }
        }

        var head = string.Join("\n", lines[..insertAt]).TrimEnd();
        var tail = string.Join("\n", lines[insertAt..]);
        var result = (head.Length > 0 ? head + "\n\n" : "") + block;

        if (tail.Length > 0)
        {
            result += "\n\n" + tail;
        }

        return result + "\n";
    }

    /// <summary>The rename key of one provision identity: its page with its statement.</summary>
    /// <param name="row">The provision identity.</param>
    /// <returns>The page with the statement.</returns>
    static (string Page, string Statement) KeyOf(Row row) => (row.Page, row.Statement);

    /// <summary>Orders rename keys by page, then by statement.</summary>
    static readonly Comparer<(string Page, string Statement)> KeyComparer = Comparer<(string Page, string Statement)>.Create((left, right) =>
        string.CompareOrdinal(left.Page, right.Page) is var order && order != 0 ? order : string.CompareOrdinal(left.Statement, right.Statement));

    /// <summary>Appends one rendered group: its counted heading with its bullets, or <c>None.</c> when empty.</summary>
    /// <param name="lines">The rendered lines.</param>
    /// <param name="title">The group title.</param>
    /// <param name="items">The rendered bullets.</param>
    static void Group(List<string> lines, string title, List<string> items)
    {
        lines.Add($"{title} ({items.Count}):");
        lines.Add("");

        if (items.Count == 0)
        {
            lines.Add("None.");
        }
        else
        {
            foreach (var item in items)
            {
                lines.Add($"- {item}");
            }
        }

        lines.Add("");
    }

    /// <summary>Reads one string property of a JSON object, or empty when it is missing or not a string.</summary>
    /// <param name="value">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : "";
}
