using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Verify.Adversarial;

/// <summary>
///     The <c>litespec verify --adversarial</c> command: replays every scenario probe through
///     three hostile matrices and reports each weakness the replay exposes.
/// </summary>
/// <follows>litespec/core/backend-application#enforce-target-authorization-in-the-pipeline</follows>
/// <follows>litespec/core/concurrency#store-idempotency-state-atomically</follows>
/// <follows>litespec/core/backend-application#separate-input-validation-from-invariants</follows>
internal static class AdversarialCommand
{
    /// <summary>The usage text for the adversarial command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec verify --adversarial [--root <consumer>] [--base-url <url>] [--use-case <id>] [--help]

        Replays every scenario probe through three hostile matrices and reports each weakness:
        an access replay with no session, five parallel requests of an idempotent command, and
        wrong-type and oversized input variants of every object body.

          --root        The consumer repository to read. Defaults to the working directory.
          --base-url    The API host address. Overrides verify.baseUrl of the project file.
          --use-case    Run only the probes of this use case, such as use-case/spending.submit-expense.
          --help        Print this text and exit.

        Exit codes: 0 every replay held, 1 a replay exposed a weakness or the host could not be reached, 2 the options are invalid, the project file is missing, or a probe names no cast person.
        """;

    /// <summary>Runs the adversarial command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 every replay held, 1 a replay exposed a weakness or was unreachable, 2 a usage error.</returns>
    public static int Run(string[] args)
    {
        if (args is ["--help"] or ["-h"] or ["help"])
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        var parsed = CliArgs.Parse(
            args,
            ["--adversarial"],
            ["--root", "--base-url", "--use-case"],
            maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (parsed.Value("--use-case") is { Length: 0 })
        {
            return CommandFailure.UsageError("Option --use-case needs a value.", Usage, json: false);
        }

        if (parsed.Value("--base-url") is { Length: 0 })
        {
            return CommandFailure.UsageError("Option --base-url needs a value.", Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var model = ModelBuilder.Build(config);

        var invalid = model.Problems
            .Where(finding => finding.Code == "record-invalid" && IsParseFailure(finding))
            .Select(finding => PathText.Relative(config.Root, finding.Path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (invalid.Count > 0)
        {
            foreach (var file in invalid)
            {
                CliHost.Error.WriteLine($"{file}: does not parse.");
            }

            CliHost.Out.WriteLine("0 probes checked.");
            return 2;
        }

        var probes = Probes.ProbeCollector.Collect(model, parsed.Value("--use-case"), out var collectError);

        if (collectError is not null)
        {
            return CommandFailure.UsageError(collectError, Usage, json: false);
        }

        var unknownAs = probes
            .Where(probe => probe.As is { Length: > 0 } && !IsCastPerson(model, probe.As))
            .Select(probe => probe.As!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (unknownAs.Count > 0)
        {
            return CommandFailure.UsageError(
                $"Probe names unknown cast person '{unknownAs[0]}': reference-unknown. Use a person id from the cast, such as person/ava, or omit as for anonymous.",
                Usage,
                json: false);
        }

        var baseUrl = parsed.Value("--base-url") ?? config.Verify.BaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return CommandFailure.UsageError(
                "No base address: pass --base-url or set verify.baseUrl in .litespec/project.yaml.",
                Usage,
                json: false);
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            return CommandFailure.UsageError(
                $"Base address '{baseUrl}' is no absolute HTTP address.",
                Usage,
                json: false);
        }

        var findings = AdversarialRunner.Run(config, baseUrl, probes, RaceOwners(model));
        var report = AdversarialReport.Write(config, baseUrl, probes, findings);

        foreach (var finding in findings)
        {
            CliHost.Out.WriteLine($"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [{finding.Tier}] [{finding.Code}] {finding.Message}");
        }

        CliHost.Out.WriteLine(findings.Count == 0
            ? $"All {probes.Count} probes held against the adversarial matrices."
            : $"{findings.Count} adversarial findings across {probes.Count} probes.");

        CliHost.Out.WriteLine($"Wrote {PathText.Relative(config.Root, report)}.");

        return findings.Count > 0 ? 1 : 0;
    }

    /// <summary>The use-case ids the race matrix replays: commands whose idempotency mode is key or state.</summary>
    /// <param name="model">The built project model holding the use case records.</param>
    /// <returns>The use-case ids that carry key or state idempotency.</returns>
    private static HashSet<string> RaceOwners(ProjectModel model)
    {
        var owners = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Derived || !string.Equals(record.Type, RecordTypes.UseCase, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(record.Field(RecordFields.Kind), "command", StringComparison.Ordinal))
            {
                continue;
            }

            if (record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
                || idempotency.ValueKind != JsonValueKind.Object
                || !idempotency.TryGetProperty(RecordFields.Mode, out var mode)
                || mode.ValueKind != JsonValueKind.String
                || mode.GetString() is not ("key" or "state"))
            {
                continue;
            }

            owners.Add(record.Id);
        }

        return owners;
    }

    /// <summary>Whether the model holds the cast person id.</summary>
    /// <param name="model">The built project model holding the cast records.</param>
    /// <param name="person">The probe as id, such as person/ava.</param>
    /// <returns>True when a non-derived person record carries the id.</returns>
    private static bool IsCastPerson(ProjectModel model, string person) =>
        model.Records.Any(record =>
            !record.Derived
            && string.Equals(record.Type, RecordTypes.Person, StringComparison.Ordinal)
            && string.Equals(record.Id, person, StringComparison.Ordinal));

    /// <summary>Whether the finding reports a file the reader could not read, rather than a record the file holds.</summary>
    /// <param name="finding">The record-invalid finding.</param>
    /// <returns>True when the file does not parse.</returns>
    private static bool IsParseFailure(Finding finding) =>
        finding.Message.Contains("could not be read", StringComparison.Ordinal)
        || finding.Message.StartsWith("The glossary is not YAML", StringComparison.Ordinal);
}
