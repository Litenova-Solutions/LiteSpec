using System.Text;
using System.Text.Json;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Proof;








/// <summary>
///     The run commit each scenario's proof last passed at, kept in the committed
///     <c>.litespec/proof/proofs.json</c>. A proof is fresh when its run commit is
///     HEAD or only record paths changed since it.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed class ProofLedger
{
    /// <summary>The recorded proofs by scenario id, updated in place when a run passes.</summary>
    private readonly Dictionary<string, ProofEntry> entries;

    /// <summary>Initializes a new instance of the <see cref="ProofLedger" /> class with its recorded proofs.</summary>
    /// <param name="entries">The recorded proofs by scenario id.</param>
    /// <param name="problems">The problems to report, empty when the ledger reads.</param>
    private ProofLedger(Dictionary<string, ProofEntry> entries, IReadOnlyList<Finding> problems)
    {
        this.entries = entries;
        Problems = problems;
    }

    /// <summary>Gets a value indicating whether whether a build recorded a new proof since the ledger was read.</summary>
    public bool Changed { get; private set; }

    /// <summary>Gets the problem a ledger that does not parse reports, empty when the ledger reads.</summary>
    public IReadOnlyList<Finding> Problems { get; }

    /// <summary>Gets the recorded proofs, by scenario id.</summary>
    public IReadOnlyDictionary<string, ProofEntry> Entries => entries;

    /// <summary>The ledger file of a project: <c>proof/proofs.json</c> under its documentation root.</summary>
    /// <param name="config">The consumer configuration holding the documentation root.</param>
    /// <returns>The absolute path of the ledger file.</returns>
    public static string PathOf(ConsumerConfig config) =>
        Path.Combine(config.RecordRoots.Count > 0 ? config.RecordRoots[0] : Path.Combine(config.Root, ".litespec"), "proof", "proofs.json");

    /// <summary>A ledger with no recorded proof.</summary>
    /// <returns>A ledger holding no recorded proof and no problems.</returns>
    public static ProofLedger Empty() => new(new Dictionary<string, ProofEntry>(StringComparer.Ordinal), []);

    /// <summary>
    ///     Reads the ledger, or an empty one when the file is missing. A ledger
    ///     that does not parse reads empty and reports a model problem, so a
    ///     damaged ledger never counts as a ledger with no proof.
    /// </summary>
    /// <param name="config">The consumer configuration holding the documentation root.</param>
    /// <returns>The ledger read from disk, or an empty ledger when the file is missing or does not parse.</returns>
    public static ProofLedger Load(ConsumerConfig config)
    {
        var entries = new Dictionary<string, ProofEntry>(StringComparer.Ordinal);
        var path = PathOf(config);

        if (!File.Exists(path))
        {
            return new ProofLedger(entries, []);
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

                var commit = property.Value.TryGetProperty("commit", out var commitId)
                    ? commitId.GetString() ?? ""
                    : "";
                var outcome = property.Value.TryGetProperty("outcome", out var result)
                    ? result.GetString() ?? ""
                    : "";
                var run = property.Value.TryGetProperty("run", out var runId)
                    ? runId.GetString() ?? ""
                    : "";

                // A ledger written before outcomes carries the commit and the run without one; its proofs passed.
                entries[property.Name] = new ProofEntry(commit, outcome.Length > 0 ? outcome : "Passed", run);
            }
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new ProofLedger(
                new Dictionary<string, ProofEntry>(StringComparer.Ordinal),
                [new Finding("record-invalid", $"Proof ledger '{path}' does not parse, so no proof in it counts.", path, 1, null)]);
        }

        return new ProofLedger(entries, []);
    }

    /// <summary>
    ///     The identity of a test run: a hash over its result files' paths and contents, so
    ///     every new run differs from the last even on the same day with the same counts.
    /// </summary>
    /// <param name="config">The consumer configuration holding the project root.</param>
    /// <param name="run">The test run whose result files identify it.</param>
    /// <returns>The sixteen-hex-character identity of the run.</returns>
    public static string RunOf(ConsumerConfig config, TestRun run)
    {
        var hasher = new Sha256();

        foreach (var file in run.Files.OrderBy(file => file, StringComparer.Ordinal))
        {
            hasher.Append(Encoding.UTF8.GetBytes(file + "\n"));

            var full = Path.Combine(config.Root, file);

            if (File.Exists(full))
            {
                hasher.Append(File.ReadAllBytes(full));
            }
        }

        return Convert.ToHexString(hasher.Finish()).ToLowerInvariant()[..16];
    }

    /// <summary>
    ///     Whether the scenario's passing proof in this run is stale: a run passing the proof for the first time
    ///     records its commit and reads fresh. On every later build against the same run, the proof is stale
    ///     unless its run commit is HEAD or only record paths changed since it.
    /// </summary>
    /// <param name="scenario">The scenario id to test.</param>
    /// <param name="run">The identity of the passing run in this build.</param>
    /// <param name="commit">The commit the passing run was read at.</param>
    /// <param name="freshness">The proof freshness against HEAD, cached per run commit.</param>
    /// <returns>True when the recorded proof for the same run is stale.</returns>
    public bool Stale(string scenario, string run, string commit, ProofFreshness freshness)
    {
        if (entries.TryGetValue(scenario, out var entry) && entry.Run == run)
        {
            return freshness.StatusOf(entry.Commit).Status == "stale";
        }

        entries[scenario] = new ProofEntry(commit, "Passed", run);
        Changed = true;

        return false;
    }

    /// <summary>
    ///     The source commit a passing run is read at: the checked-out HEAD of the consumer. Empty when the
    ///     consumer is not a git checkout, so a proof then carries no commit.
    /// </summary>
    /// <param name="config">The consumer configuration holding the project root.</param>
    /// <returns>The full commit identifier, or empty when none can be read.</returns>
    public static string CommitOf(ConsumerConfig config)
    {
        var result = ProcessRunner.Git(config.Root, "rev-parse", "HEAD");

        return result.Status == 0 ? result.Stdout.Trim() : "";
    }

    /// <summary>Writes the ledger when a build recorded a new proof.</summary>
    /// <param name="config">The consumer configuration holding the documentation root.</param>
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
                writer.WriteString("commit", entry.Value.Commit);
                writer.WriteString("outcome", entry.Value.Outcome);
                writer.WriteString("run", entry.Value.Run);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        File.WriteAllText(path, Encoding.UTF8.GetString(stream.ToArray()) + "\n");
        Changed = false;
    }
}
