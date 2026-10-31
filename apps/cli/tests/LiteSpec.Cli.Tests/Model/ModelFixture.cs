using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>
///     Copies a model fixture to a fresh temporary folder and applies a broken overlay.
/// </summary>
static class ModelFixture
{
    /// <summary>Copies <c>Fixtures/Model/&lt;fixture&gt;</c> to a fresh temp folder, then overlays <c>broken/&lt;overlay&gt;</c> when one is named.</summary>
    /// <param name="fixtureName">The fixture to copy, <c>small</c> or <c>large</c>.</param>
    /// <param name="overlayName">The broken overlay to apply, or <c>null</c> for a clean fixture.</param>
    /// <returns>The temp folder holding the prepared fixture; the caller deletes it.</returns>
    public static TempFolder Prepare(string fixtureName, string? overlayName = null)
    {
        var temp = Cli.Temp("model-cases");
        try
        {
            CopyContents(Cli.Fixture($"Model/{fixtureName}"), temp.Path);
            if (overlayName is not null)
            {
                ApplyOverlay(overlayName, temp.Path);
            }

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void CopyContents(string source, string target)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    // Copies the overlay files over the fixture, skipping DELETES.txt itself,
    // then deletes the paths DELETES.txt lists, one per line.
    static void ApplyOverlay(string overlayName, string target)
    {
        var overlay = Cli.Fixture($"Model/broken/{overlayName}");
        foreach (var file in Directory.GetFiles(overlay, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(overlay, file);
            if (relative == "DELETES.txt")
            {
                continue;
            }

            var destination = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }

        var deletes = Path.Combine(overlay, "DELETES.txt");
        if (!File.Exists(deletes))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(deletes))
        {
            var relative = line.Trim();
            if (relative.Length == 0)
            {
                continue;
            }

            File.Delete(Path.Combine(target, relative.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    /// <summary>
    ///     Removes the <c>transitions</c> field from the frontmatter of one lending aggregate page, so a
    ///     test can start from an aggregate whose lifecycle is not yet written. The page's approval reopens,
    ///     so a test that runs the check stamps the aggregate again.
    /// </summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="folder">The aggregate's folder under <c>docs/behavior/modules/lending</c>, such as <c>loans</c>.</param>
    public static void RemoveTransitions(string root, string folder)
    {
        var page = Path.Combine(root, "docs", "behavior", "modules", "lending", folder, "README.md");
        var text = File.ReadAllText(page).Replace("\r\n", "\n", StringComparison.Ordinal);
        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        var frontmatter = System.Text.Json.Nodes.JsonNode.Parse(text[4..end])!.AsObject();

        frontmatter.Remove("transitions");

        var json = frontmatter.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, NewLine = "\n" });
        File.WriteAllText(page, "---\n" + json + text[end..]);
    }
}
