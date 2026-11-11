using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace LiteSpec.Standards;

/// <summary>
///     The parsed files of the LiteSpec release, such as its schemas, read once per process.
///     A release file does not change while a command runs, so every reader shares one parse;
///     a file written since it was read, by size or time, is read again. Many readers asking
///     at once wait for the one parse instead of each parsing the file.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class ReleaseText
{
    /// <summary>The parsed files by full path, each with the size and time it was read at.</summary>
    static readonly ConcurrentDictionary<string, Lazy<Entry>> Files = new(StringComparer.Ordinal);

    /// <summary>
    ///     Reads a release file as <see cref="RecordText.Read"/> does, from the shared parse.
    ///     The node is shared: a reader must not change it.
    /// </summary>
    /// <param name="path">The release file to read.</param>
    /// <returns>The parsed file.</returns>
    public static JsonNode? Read(string path)
    {
        var full = Path.GetFullPath(path);
        var info = new FileInfo(full);
        var stamp = (info.Exists ? info.Length : -1, info.Exists ? info.LastWriteTimeUtc : DateTime.MinValue);

        while (true)
        {
            var lazy = Files.GetOrAdd(full, key => new Lazy<Entry>(() => new Entry(stamp, RecordText.Read(key))));
            var entry = lazy.Value;

            if (entry.Stamp == stamp)
            {
                return entry.Node;
            }

            Files.TryRemove(new KeyValuePair<string, Lazy<Entry>>(full, lazy));
        }
    }

    /// <summary>One parsed file and the size and write time it was read at.</summary>
    /// <param name="Stamp">The file size and last write time when it was read.</param>
    /// <param name="Node">The parsed file.</param>
    sealed record Entry((long Length, DateTime Written) Stamp, JsonNode? Node);
}
