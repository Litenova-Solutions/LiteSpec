using System.IO.Compression;

namespace LiteSpec.Cli.Init;

/// <summary>
///     Reads the runnable starter of one preset from the release the tool carries: the folder
///     <c>templates/bootstrap/&lt;preset&gt;/</c> in a repository checkout, or the archive
///     <c>templates/bootstrap/&lt;preset&gt;.zip</c> the tool package carries in its place, because a
///     package path NuGet accepts holds at most 128 characters and the starter's own paths run longer.
/// </summary>
/// <follows>litespec/core/scope#select-a-size-a-shape-and-stacks</follows>
internal static class StarterFiles
{
    /// <summary>One starter file: its path inside the starter with forward slashes, and its bytes.</summary>
    /// <param name="Relative">The path inside the starter, with forward slashes.</param>
    /// <param name="Bytes">The file content.</param>
    internal sealed record StarterFile(string Relative, byte[] Bytes);

    /// <summary>Reads every file of the preset's starter, in ordinal path order.</summary>
    /// <param name="home">The release root the tool carries.</param>
    /// <param name="preset">The preset whose starter is read.</param>
    /// <param name="error">The reason the starter could not be read, or null when it was read.</param>
    /// <returns>The starter files, or null when the release holds no readable starter.</returns>
    internal static IReadOnlyList<StarterFile>? Read(string home, string preset, out string? error)
    {
        var folder = Path.Combine(home, "templates", "bootstrap", preset);
        var archive = folder + ".zip";
        error = null;

        try
        {
            if (Directory.Exists(folder))
            {
                return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                    .Select(file => new StarterFile(Path.GetRelativePath(folder, file).Replace('\\', '/'), File.ReadAllBytes(file)))
                    .OrderBy(file => file.Relative, StringComparer.Ordinal)
                    .ToList();
            }

            if (File.Exists(archive))
            {
                using var zip = ZipFile.OpenRead(archive);
                var files = new List<StarterFile>();

                foreach (var entry in zip.Entries.Where(entry => entry.Name.Length > 0))
                {
                    using var stream = entry.Open();
                    using var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    files.Add(new StarterFile(entry.FullName.Replace('\\', '/'), buffer.ToArray()));
                }

                return files.OrderBy(file => file.Relative, StringComparer.Ordinal).ToList();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            error = $"Failed to read templates/bootstrap/{preset}: {exception.Message}";
            return null;
        }

        error = $"The LiteSpec release holds no readable templates/bootstrap/{preset}/.";
        return null;
    }
}
