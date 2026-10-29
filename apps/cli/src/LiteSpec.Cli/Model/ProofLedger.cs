using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>One scenario's recorded proof: its semantic hash and the run that first passed it.</summary>
sealed record ProofEntry(string Hash, string Run);

/// <summary>
///     The semantic hash each scenario had when the builder first read a passing run of
///     its proof, kept in <c>.model/proofs.json</c>. A later build against the same run
///     finds the proof stale when the scenario's hash has moved since.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class ProofLedger
{
    private readonly Dictionary<string, ProofEntry> entries;

    private ProofLedger(Dictionary<string, ProofEntry> entries) => this.entries = entries;

    /// <summary>Whether a build recorded a new proof since the ledger was read.</summary>
    public bool Changed { get; private set; }

    /// <summary>The recorded proofs, by scenario id.</summary>
    public IReadOnlyDictionary<string, ProofEntry> Entries => entries;

    /// <summary>The ledger file of a project: <c>.model/proofs.json</c> under its root.</summary>
    public static string PathOf(string root) => Path.Combine(root, ".model", "proofs.json");

    /// <summary>A ledger with no recorded proof.</summary>
    public static ProofLedger Empty() => new(new Dictionary<string, ProofEntry>(StringComparer.Ordinal));

    /// <summary>Reads the ledger, or an empty one when the file is missing or unreadable.</summary>
    public static ProofLedger Load(string root)
    {
        var entries = new Dictionary<string, ProofEntry>(StringComparer.Ordinal);
        var path = PathOf(root);

        if (!File.Exists(path))
        {
            return new ProofLedger(entries);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.TryGetProperty("hash", out var hash)
                    && property.Value.TryGetProperty("run", out var run)
                    && hash.GetString() is { } hashText
                    && run.GetString() is { } runText)
                {
                    entries[property.Name] = new ProofEntry(hashText, runText);
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or IOException or InvalidOperationException)
        {
            entries.Clear();
        }

        return new ProofLedger(entries);
    }

    /// <summary>
    ///     The identity of a test run: a hash over its result files' paths and contents, so
    ///     every new run differs from the last even on the same day with the same counts.
    /// </summary>
    public static string RunOf(ConsumerConfig config, TestRun run)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var file in run.Files.OrderBy(file => file, StringComparer.Ordinal))
        {
            hasher.AppendData(Encoding.UTF8.GetBytes(file + "\n"));

            var full = Path.Combine(config.Root, file);

            if (File.Exists(full))
            {
                hasher.AppendData(File.ReadAllBytes(full));
            }
        }

        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant()[..16];
    }

    /// <summary>
    ///     Whether the scenario's passing proof in this run is stale. The first time a run
    ///     passes the proof, the scenario's hash is recorded and the proof is current.
    /// </summary>
    public bool Stale(string scenario, string hash, string run)
    {
        if (entries.TryGetValue(scenario, out var entry) && entry.Run == run)
        {
            return entry.Hash != hash;
        }

        entries[scenario] = new ProofEntry(hash, run);
        Changed = true;

        return false;
    }

    /// <summary>Writes the ledger when a build recorded a new proof.</summary>
    public void Save(string root)
    {
        if (!Changed)
        {
            return;
        }

        var path = PathOf(root);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var entry in entries.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(entry.Key);
                writer.WriteString("hash", entry.Value.Hash);
                writer.WriteString("run", entry.Value.Run);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        File.WriteAllText(path, Encoding.UTF8.GetString(stream.ToArray()) + "\n");
        Changed = false;
    }
}
