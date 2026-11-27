using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     The <c>litespec verify</c> command: runs every scenario probe against the
///     running product and reports each failed expectation.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal static class VerifyCommand
{
    /// <summary>The usage text for the verify command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec verify [--root <consumer>] [--base-url <url>] [--use-case <id>] [--help]

        Runs every scenario probe against the running product: signs each cast person
        in through verify.signIn, resolves {name} from verify.values, sends the request
        to --base-url or verify.baseUrl, and compares the status, the refusal and the fields.

          --root        The consumer repository to read. Defaults to the working directory.
          --base-url    The API host address. Overrides verify.baseUrl of the project file.
          --use-case    Run only the probes of this use case, such as use-case/spending.submit-expense.
          --help        Print this text and exit.

        Exit codes: 0 no probe failed, 1 a probe failed or the host could not be reached, 2 the options are invalid, the project file is missing, or a probe names no cast person.
        """;

    /// <summary>Runs the verify command.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code: 0 passed, 1 a probe failed or was unreachable, 2 a usage error.</returns>
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

        var probes = ProbeCollector.Collect(model, parsed.Value("--use-case"), out var collectError);

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

        var findings = RunProbes(config, probes, baseUrl);
        var report = ProbeReport.Write(config, baseUrl, probes, findings);

        foreach (var finding in findings)
        {
            CliHost.Out.WriteLine($"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [S] [{finding.Code}] {finding.Message}");
        }

        CliHost.Out.WriteLine(findings.Count == 0
            ? $"All {probes.Count} probes passed."
            : $"{findings.Count} findings across {probes.Count} probes.");

        CliHost.Out.WriteLine($"Wrote {PathText.Relative(config.Root, report)}.");

        return findings.Count > 0 ? 1 : 0;
    }

    /// <summary>Runs every probe, signing each cast person in once and reusing the session.</summary>
    /// <param name="config">The consumer configuration holding the verify block.</param>
    /// <param name="probes">The probes in run order.</param>
    /// <param name="baseUrl">The API host address.</param>
    /// <returns>The findings, in probe order.</returns>
    private static List<Finding> RunProbes(ConsumerConfig config, IReadOnlyList<ProbeCase> probes, string baseUrl)
    {
        var findings = new List<Finding>();
        var sessions = new Dictionary<string, ProbeSession>(StringComparer.Ordinal);
        var unreachable = new HashSet<string>(StringComparer.Ordinal);

        ProbeSession SessionFor(string? person)
        {
            var key = person ?? string.Empty;

            if (sessions.TryGetValue(key, out var known))
            {
                return known;
            }

            try
            {
                var session = ProbeSession.SignIn(person, baseUrl, config.Verify.SignIn, config.Verify.Values);
                sessions[key] = session;
                return session;
            }
            catch (ProbeUnreachableException exception)
            {
                unreachable.Add(key);

                foreach (var probe in probes.Where(candidate => (candidate.As ?? string.Empty) == key))
                {
                    findings.Add(new Finding(
                        "probe-unreachable",
                        $"Probe '{probe.ScenarioId}' could not sign '{person ?? "anonymous"}' in: {ShortReason(exception.Message)}",
                        probe.File,
                        probe.Line,
                        probe.ScenarioId));
                }

                throw;
            }
        }

        foreach (var probe in probes)
        {
            var key = probe.As ?? string.Empty;

            if (unreachable.Contains(key))
            {
                continue;
            }

            // A sign-in failure reports every probe of that person once, so a
            // probe whose session already failed stays silent here.
            if (findings.Any(finding => string.Equals(finding.Subject, probe.ScenarioId, StringComparison.Ordinal)))
            {
                continue;
            }

            ProbeSession session;

            try
            {
                session = SessionFor(probe.As);
            }
            catch (ProbeUnreachableException)
            {
                continue;
            }

            var shortName = probe.As is { Length: > 0 } person
                ? (person.StartsWith("person/", StringComparison.Ordinal) ? person["person/".Length..] : person)
                : null;
            var request = ResolvedProbeRequest.From(probe, config.Verify.Values, shortName);

            int status;
            string body;

            try
            {
                (status, body) = session.Send(request);
            }
            catch (ProbeUnreachableException exception)
            {
                findings.Add(new Finding(
                    "probe-unreachable",
                    $"Probe '{probe.ScenarioId}' could not reach the host: {ShortReason(exception.Message)}",
                    probe.File,
                    probe.Line,
                    probe.ScenarioId));
                continue;
            }

            foreach (var failure in ProbeEvaluation.Evaluate(probe, status, body))
            {
                findings.Add(new Finding("probe-failed", failure, probe.File, probe.Line, probe.ScenarioId));
            }
        }

        foreach (var session in sessions.Values)
        {
            session.Dispose();
        }

        return findings;
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

    /// <summary>The reason as one trimmed sentence without the trailing stop.</summary>
    /// <param name="message">The full failure message.</param>
    /// <returns>The trimmed message.</returns>
    private static string ShortReason(string message)
    {
        var trimmed = message.Trim();

        return trimmed.EndsWith('.') ? trimmed[..^1] : trimmed;
    }
}
