using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.PlanWaves;

/// <summary>
///     The <c>litespec plan-waves</c> command: prints the conflict-free parallel
///     slices of the consumer, one per aggregate use-case set, each with its
///     exclusive path globs, and the waves in dependency order. Shared Kernel,
///     Platform and Contracts paths print once as frozen.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class PlanWavesCommand
{
    /// <summary>The usage text for the plan-waves command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec plan-waves [--root <consumer>] [--format json]
          litespec plan-waves --help

        Prints the conflict-free parallel slices of the consumer, one per aggregate
        use-case set, each with its exclusive path globs: its record folder, its
        Application, WebApi and Infrastructure folders, and its frontend module
        folders. The waves hold the slices in dependency order, so the slices of
        one wave run beside each other. Shared Kernel, Platform and Contracts paths
        print once as frozen: every wave leaves them alone.

          --root      The consumer repository to read. Defaults to the working directory.
          --format    json for a JSON object, or text. Defaults to text.
          --help      Print this text and exit.

        Exit codes: 0 the slices are printed, 1 the records cannot be read, 2 the options are invalid or the project file is missing.
        """;

    /// <summary>Runs the plan-waves command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 printed, 1 unreadable records, 2 usage error or unreadable consumer.</returns>
    public static int Run(string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            [],
            ["--root", "--format"],
            maxPositionals: 0);

        if (parsed.Help && parsed.Error is null && parsed.FormatError is null)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (parsed.FormatError is { } formatError)
        {
            return CommandFailure.UsageError(formatError, Usage, json: false);
        }

        var json = parsed.Json;
        var config = ConsumerRoot.Open(parsed.Value("--root"), json, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var model = ModelBuilder.Build(config);

        // A printed slice must never be quietly short: when a record file
        // does not parse, plan-waves prints nothing and names each invalid file instead.
        var invalid = model.Problems
            .Where(finding => finding.Code == "record-invalid" && IsParseFailure(finding))
            .Select(finding => PathText.Relative(config.Root, finding.Path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (invalid.Count > 0)
        {
            if (json)
            {
                return CommandFailure.Fail(
                    $"The records cannot be read: {string.Join("; ", invalid)}",
                    json: true,
                    code: 1);
            }

            foreach (var file in invalid)
            {
                CliHost.Error.WriteLine($"{file}: does not parse.");
            }

            return 1;
        }

        var plan = PlanWavesPlan.Build(model);

        if (json)
        {
            CliHost.Out.WriteLine(CliJson.Serialize(JsonOf(plan)));
        }
        else
        {
            Print(plan);
        }

        return 0;
    }

    /// <summary>Whether the finding reports a file the reader could not read, rather than a record the file holds.</summary>
    /// <param name="finding">The record-invalid finding.</param>
    /// <returns>True when the file does not parse.</returns>
    static bool IsParseFailure(Finding finding) =>
        finding.Message.Contains("could not be read", StringComparison.Ordinal)
            || finding.Message.StartsWith("The glossary is not YAML", StringComparison.Ordinal);

    /// <summary>Prints the slices, waves and frozen paths as text.</summary>
    /// <param name="plan">The planned slices, waves and frozen paths.</param>
    static void Print(PlanWavesPlan.Plan plan)
    {
        if (plan.Slices.Count == 0)
        {
            CliHost.Out.WriteLine("No slices: the project holds no aggregates with use cases.");
            return;
        }

        CliHost.Out.WriteLine($"Slices ({plan.Slices.Count}):");

        foreach (var slice in plan.Slices)
        {
            CliHost.Out.WriteLine($"- {slice.Id}");
            CliHost.Out.WriteLine($"  records: {string.Join(", ", slice.Records)}");
            CliHost.Out.WriteLine("  paths:");

            foreach (var path in slice.Paths)
            {
                CliHost.Out.WriteLine($"  - {path}");
            }
        }

        CliHost.Out.WriteLine($"Waves ({plan.Waves.Count}):");

        for (var wave = 0; wave < plan.Waves.Count; wave++)
        {
            CliHost.Out.WriteLine($"- wave {wave + 1}: {string.Join(", ", plan.Waves[wave])}");
        }

        if (plan.Frozen.Count > 0)
        {
            CliHost.Out.WriteLine("Frozen:");

            foreach (var path in plan.Frozen)
            {
                CliHost.Out.WriteLine($"- {path}");
            }
        }
    }

    /// <summary>The JSON object of a plan: its slices, waves and frozen paths.</summary>
    /// <param name="plan">The planned slices, waves and frozen paths.</param>
    /// <returns>The JSON object the command prints.</returns>
    static JsonObject JsonOf(PlanWavesPlan.Plan plan)
    {
        var slices = new JsonArray([.. plan.Slices.Select(slice => (JsonNode?)new JsonObject
        {
            ["id"] = slice.Id,
            ["module"] = slice.Module,
            ["records"] = CliJson.Strings(slice.Records),
            ["paths"] = CliJson.Strings(slice.Paths),
        })]);

        var waves = new JsonArray([.. plan.Waves.Select(wave => (JsonNode?)CliJson.Strings(wave))]);

        return new JsonObject
        {
            ["slices"] = slices,
            ["waves"] = waves,
            ["frozen"] = CliJson.Strings(plan.Frozen),
        };
    }
}
