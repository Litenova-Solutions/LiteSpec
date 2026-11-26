using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The record files under the consumer's record roots, listed once per rules run.
///     Each file is read and parsed at most once per run, and the marked records of
///     each target are kept, so every target shares the records it names.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal sealed class ProvisionRecordIndex
{
    /// <summary>The record files in read order.</summary>
    private readonly IReadOnlyList<string> files;

    /// <summary>The files read and parsed so far.</summary>
    private readonly HashSet<string> scanned = new(StringComparer.Ordinal);

    /// <summary>The fields of the records read so far, by record identifier.</summary>
    private readonly Dictionary<string, ProvisionWhen.RecordFields> byId = new(StringComparer.Ordinal);

    /// <summary>The marked records loaded so far, by target.</summary>
    private readonly Dictionary<string, IReadOnlyList<ProvisionWhen.RecordFields>> byTarget = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="ProvisionRecordIndex" /> class over the record files.</summary>
    /// <param name="files">The record files in read order.</param>
    private ProvisionRecordIndex(IReadOnlyList<string> files) => this.files = files;

    /// <summary>Lists the record files under the consumer's record roots, once per rules run.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The index over the record files.</returns>
    public static ProvisionRecordIndex Load(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var root in config.RecordRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] found;

            try
            {
                found = Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            files.AddRange(found.OrderBy(name => name, StringComparer.Ordinal));
        }

        return new ProvisionRecordIndex(files);
    }

    /// <summary>Loads the marked records' fields, reading and parsing each record file once per run.</summary>
    /// <param name="target">The rules target with its marked identifiers.</param>
    /// <returns>The fields of each marked record found, in marked order.</returns>
    public IReadOnlyList<ProvisionWhen.RecordFields> LoadMarked(PathContext target)
    {
        if (byTarget.TryGetValue(target.Display, out var cached))
        {
            return cached;
        }

        var wanted = target.Marked
            .Where(marked => PathContext.ArtifactOf(marked).Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var missing = wanted.Where(id => !byId.ContainsKey(id)).ToHashSet(StringComparer.Ordinal);

        foreach (var file in files)
        {
            if (missing.Count == 0)
            {
                break;
            }

            if (!scanned.Add(file))
            {
                continue;
            }

            if (ProvisionWhen.TryReadFields(ReadText(file), out var id, out var record))
            {
                byId.TryAdd(id, record);
                missing.Remove(id);
            }
        }

        var loaded = wanted.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        byTarget[target.Display] = loaded;
        return loaded;
    }

    /// <summary>Reads one file's text, or null when the file cannot be read.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <returns>The file text, or null.</returns>
    static string? ReadText(string file)
    {
        try
        {
            return File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
