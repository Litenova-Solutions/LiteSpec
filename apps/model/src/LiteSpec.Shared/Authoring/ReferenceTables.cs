using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     The shared builders for the tables the reference pages hold: the finding code table, the
///     record folder table and the manifest pin table. The generator writes them and the freshness
///     gate compares them, so a checked-in table and its check cannot disagree.
/// </summary>
/// <follows>litespec/core/authoring#regenerate-the-provision-index</follows>
public static class ReferenceTables
{
    /// <summary>The generated finding code page, relative to the repository root.</summary>
    public const string FindingCodesPath = "docs/reference/finding-codes.md";

    /// <summary>The generated record folder page, relative to the repository root.</summary>
    public const string FoldersPath = "docs/reference/folders.md";

    /// <summary>The release manifest page whose pin table is generated, relative to the repository root.</summary>
    public const string ManifestPath = "docs/reference/manifest.md";

    /// <summary>The generated-region name of the manifest pin table.</summary>
    public const string PinsRegion = "manifest-pins";

    /// <summary>Builds <c>docs/reference/finding-codes.md</c> from the finding code table.</summary>
    /// <returns>The page text, with a final newline.</returns>
    public static string BuildFindingCodes()
    {
        var text = new StringBuilder();
        text.Append("# Finding Codes\n\n## Intent\n\n");
        text.Append("Use this generated page to read what a finding code means and how to resolve it. A reader who sees a code in `litespec model check` output, or in a `Checked by` line of a standards page, looks it up here. The table holds every code the tools emit, with its level, the record layer it governs, whether it ties that layer to the records, and the extension that adds it.\n\n");
        text.Append("## Reference\n\n");
        text.Append("| Code | Level | Layer | Trace | Extension | Meaning | Fix |\n|:---|:---|:---|:---|:---|:---|:---|\n");
        foreach (var code in FindingCodes.All)
        {
            text.Append(CultureInfo.InvariantCulture, $"| `{code.Code}` | {code.Level} | {LayerClaims.NameOf(code.Layer)} | {(code.Trace ? "yes" : "no")} | {(code.Extension is null ? "-" : code.Extension)} | {Cell(code.Meaning)} | {Cell(code.Fix)} |\n");
        }

        text.Append("\n## Notes\n\nThe `litespec-maintain provisions` command writes this page from the finding code table, and `litespec-maintain provisions --check` fails when it differs. Edit the table, not the page.\n");
        return text.ToString();
    }

    /// <summary>Builds <c>docs/reference/folders.md</c> from the record folder table.</summary>
    /// <returns>The page text, with a final newline.</returns>
    public static string BuildFolders()
    {
        var text = new StringBuilder();
        text.Append("# Record Folders\n\n## Intent\n\n");
        text.Append("Use this generated page to find the folder and file each record type lives in, relative to `.litespec/`. A record in any other folder is a `record-placement-invalid` finding. The Flat module column marks a home that applies only to a module that keeps its records flat.\n\n");
        text.Append("## Reference\n\n");
        text.Append("| Type | Folder | File | Flat module |\n|:---|:---|:---|:---|\n");
        foreach (var (type, folder, file, flat) in RecordFolders.Homes())
        {
            text.Append(CultureInfo.InvariantCulture, $"| `{type}` | `{folder}` | `{file}` | {(flat ? "yes" : "-")} |\n");
        }

        text.Append("\n## Notes\n\nThe `litespec-maintain provisions` command writes this page from the record folder table in the model, and `litespec-maintain provisions --check` fails when it differs. Edit the table, not the page.\n");
        return text.ToString();
    }

    /// <summary>Builds the generated region of the manifest pin table from the manifest.</summary>
    /// <param name="manifest">The parsed release manifest.</param>
    /// <returns>The region, fenced by its generated markers, with no final newline.</returns>
    public static string BuildPins(JsonObject manifest)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"<!-- generated:{PinsRegion} -->\n");
        text.Append("| Kind | Name | Version |\n|:---|:---|:---|\n");
        foreach (var (name, version) in Pairs(manifest["stack"] as JsonObject))
        {
            text.Append(CultureInfo.InvariantCulture, $"| toolchain | `{name}` | `{version}` |\n");
        }

        var packages = manifest["packages"] as JsonObject;
        foreach (var ecosystem in new[] { "nuget", "npm" })
        {
            foreach (var (name, version) in Pairs(packages?[ecosystem] as JsonObject))
            {
                text.Append(CultureInfo.InvariantCulture, $"| {ecosystem} | `{name}` | `{version}` |\n");
            }
        }

        text.Append(CultureInfo.InvariantCulture, $"<!-- /generated:{PinsRegion} -->");
        return text.ToString();
    }

    /// <summary>Builds the generated files of the reference tables, reading the manifest page they patch.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>Each generated file's path and its expected text. The manifest page is listed only when it exists.</returns>
    /// <exception cref="InvalidOperationException">The manifest cannot be read, or the manifest page holds no pin table region.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A manifest that cannot be read is reported as one error by the caller, so the generated tables never crash a validation run.")]
    public static (string Path, string Text)[] Outputs(string root)
    {
        var tables = new List<(string Path, string Text)>
        {
            (FindingCodesPath, BuildFindingCodes()),
            (FoldersPath, BuildFolders()),
        };
        var manifestFile = Path.Combine(root, ManifestPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(manifestFile))
        {
            return [.. tables];
        }

        var page = File.ReadAllText(manifestFile).Replace("\r\n", "\n", StringComparison.Ordinal);
        JsonObject manifest;
        try
        {
            manifest = File.Exists(Path.Combine(root, LiteSpecHome.ManifestFile)) ? LiteSpecHome.ReadManifest(root) : new JsonObject();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new InvalidOperationException($"{LiteSpecHome.ManifestFile} cannot be read: {exception.Message}", exception);
        }

        var patched = ApplyPins(page, BuildPins(manifest)) ?? throw new InvalidOperationException($"{ManifestPath} holds no generated:{PinsRegion} region.");
        tables.Add((ManifestPath, patched));
        return [.. tables];
    }

    /// <summary>Replaces the pin table region of a manifest page.</summary>
    /// <param name="page">The manifest page text.</param>
    /// <param name="region">The region from <see cref="BuildPins"/>.</param>
    /// <returns>The page with its region replaced, or null when the page holds no region.</returns>
    public static string? ApplyPins(string page, string region)
    {
        var open = $"<!-- generated:{PinsRegion} -->";
        var close = $"<!-- /generated:{PinsRegion} -->";
        var start = page.IndexOf(open, StringComparison.Ordinal);
        var end = page.IndexOf(close, StringComparison.Ordinal);
        if (start < 0 || end < start)
        {
            return null;
        }

        return page[..start] + region + page[(end + close.Length)..];
    }

    /// <summary>The name and value pairs of a pin object, in the manifest's order.</summary>
    /// <param name="pins">The pin object, or null.</param>
    /// <returns>Each key with its value as written.</returns>
    private static IEnumerable<(string Name, string Version)> Pairs(JsonObject? pins) =>
        pins is null ? [] : pins.Select(pin => (pin.Key, pin.Value?.ToString() ?? string.Empty));

    /// <summary>Escapes a table cell so a pipe in the text does not start a new column.</summary>
    /// <param name="value">The cell text.</param>
    /// <returns>The escaped cell text.</returns>
    private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
