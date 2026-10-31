using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>One scenario's recorded proof: its hashes at the last passing run, and that run.</summary>
/// <param name="Scenario">The scenario semantic hash at the last passing run.</param>
/// <param name="Test">The proving test files' hash at the last passing run.</param>
/// <param name="Run">The run identity that recorded the hashes.</param>
sealed record ProofEntry(string Scenario, string Test, string Run);

/// <summary>
///     The hashes each scenario had when the builder last read a passing run of
///     its proof, kept in the committed <c>docs/proof/proofs.json</c>. A proof is
///     stale when the scenario's hash or its tests' hash has moved since.
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

    /// <summary>The ledger file of a project: <c>proof/proofs.json</c> under its documentation root.</summary>
    public static string PathOf(ConsumerConfig config) =>
        Path.Combine(config.RecordRoots.FirstOrDefault() ?? Path.Combine(config.Root, "docs"), "proof", "proofs.json");

    /// <summary>A ledger with no recorded proof.</summary>
    public static ProofLedger Empty() => new(new Dictionary<string, ProofEntry>(StringComparer.Ordinal));

    /// <summary>Reads the ledger, or an empty one when the file is missing or unreadable.</summary>
    public static ProofLedger Load(ConsumerConfig config)
    {
        var entries = new Dictionary<string, ProofEntry>(StringComparer.Ordinal);
        var path = PathOf(config);

        if (!File.Exists(path))
        {
            return new ProofLedger(entries);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var scenario = property.Value.TryGetProperty("scenario", out var scenarioHash)
                    ? scenarioHash.GetString() ?? ""
                    : "";
                var test = property.Value.TryGetProperty("test", out var testHash)
                    ? testHash.GetString() ?? ""
                    : "";
                var run = property.Value.TryGetProperty("run", out var runId)
                    ? runId.GetString() ?? ""
                    : "";

                entries[property.Name] = new ProofEntry(scenario, test, run);
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
    ///     The hash of the proving test files: the contents of every test file holding a
    ///     confirmed proof of the scenario, in path order, so a test change stales the
    ///     proof until a newer passing run is read. A missing file hashes as empty.
    /// </summary>
    public static string TestHashOf(ProjectModel model, string scenario)
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var link in model.Proves)
        {
            if (link.Claim != scenario || !link.Confirmed)
            {
                continue;
            }

            foreach (var test in model.Tests)
            {
                if (test.Name == link.Scenario)
                {
                    files.Add(test.File);
                }
            }

            foreach (var browser in model.BrowserTests)
            {
                if (browser.Title == link.Scenario)
                {
                    files.Add(browser.File);
                }
            }
        }

        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var file in files)
        {
            var full = Path.IsPathRooted(file) ? file : Path.Combine(model.Config.Root, file);

            hasher.AppendData(Encoding.UTF8.GetBytes(full + "\n"));

            if (File.Exists(full))
            {
                hasher.AppendData(File.ReadAllBytes(full));
            }
        }

        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant()[..16];
    }

    /// <summary>
    ///     Whether the scenario's passing proof in this run is stale: the first time a run
    ///     passes the proof, both hashes are recorded and the proof is current. On every
    ///     later build against the same run, the proof is stale when either hash moved.
    ///     A newer passing run records the current hashes instead.
    /// </summary>
    public bool Stale(string scenario, string scenarioHash, string testHash, string run)
    {
        if (entries.TryGetValue(scenario, out var entry) && entry.Run == run)
        {
            return entry.Scenario != scenarioHash || entry.Test != testHash;
        }

        entries[scenario] = new ProofEntry(scenarioHash, testHash, run);
        Changed = true;

        return false;
    }

    /// <summary>Writes the ledger when a build recorded a new proof.</summary>
    public void Save(ConsumerConfig config)
    {
        if (!Changed)
        {
            return;
        }

        var path = PathOf(config);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var entry in entries.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(entry.Key);
                writer.WriteString("scenario", entry.Value.Scenario);
                writer.WriteString("test", entry.Value.Test);
                writer.WriteString("run", entry.Value.Run);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        File.WriteAllText(path, Encoding.UTF8.GetString(stream.ToArray()) + "\n");
        Changed = false;
    }
}
