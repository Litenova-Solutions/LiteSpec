using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Verify.Probes;
using LiteSpec.Model;

namespace LiteSpec.Cli.Verify.Adversarial;

/// <summary>
///     Runs the three adversarial matrices over the collected probes: the access replay with no
///     session, the five parallel requests of an idempotent command, and the wrong-type and
///     oversized input variants of every object body. Each cast person signs in once.
/// </summary>
/// <follows>litespec/core/backend-application#enforce-target-authorization-in-the-pipeline</follows>
/// <follows>litespec/core/concurrency#store-idempotency-state-atomically</follows>
/// <follows>litespec/core/backend-application#separate-input-validation-from-invariants</follows>
internal static class AdversarialRunner
{
    /// <summary>How many parallel requests the race matrix sends for one probe.</summary>
    private const int RaceRepeatCount = 5;

    /// <summary>Runs the three matrices over the probes.</summary>
    /// <param name="config">The consumer configuration holding the verify block.</param>
    /// <param name="baseUrl">The API host address the replays run against.</param>
    /// <param name="probes">The probes in run order.</param>
    /// <param name="raceOwners">The use-case ids whose command carries key or state idempotency.</param>
    /// <returns>The findings, in probe order with the access, race, then input finding of each probe.</returns>
    public static List<Finding> Run(
        ConsumerConfig config,
        string baseUrl,
        IReadOnlyList<ProbeCase> probes,
        IReadOnlySet<string> raceOwners)
    {
        var sessions = new Dictionary<string, ProbeSession>(StringComparer.Ordinal);
        var failed = new HashSet<string>(StringComparer.Ordinal);
        var findings = new List<Finding>();

        try
        {
            foreach (var probe in probes)
            {
                var key = probe.As ?? string.Empty;

                if (failed.Contains(key))
                {
                    continue;
                }

                if (!RunAccess(config, baseUrl, sessions, failed, probes, findings, probe))
                {
                    continue;
                }

                if (!RunRace(config, baseUrl, sessions, failed, probes, findings, probe, raceOwners))
                {
                    continue;
                }

                RunInput(config, baseUrl, sessions, failed, probes, findings, probe);
            }
        }
        finally
        {
            foreach (var session in sessions.Values)
            {
                session.Dispose();
            }
        }

        return findings;
    }

    /// <summary>Replays one probe with no session when it names a caller and expects success.</summary>
    /// <param name="config">The consumer configuration holding the verify block.</param>
    /// <param name="baseUrl">The API host address.</param>
    /// <param name="sessions">The sessions by cast person, with the anonymous session under the empty key.</param>
    /// <param name="failed">The session keys whose sign-in already failed.</param>
    /// <param name="probes">Every probe, for the sign-in failure findings.</param>
    /// <param name="findings">The findings the run raised so far.</param>
    /// <param name="probe">The probe to replay.</param>
    /// <returns>False when the host could not be reached, so the caller skips the later matrices of this probe.</returns>
    private static bool RunAccess(
        ConsumerConfig config,
        string baseUrl,
        Dictionary<string, ProbeSession> sessions,
        HashSet<string> failed,
        IReadOnlyList<ProbeCase> probes,
        List<Finding> findings,
        ProbeCase probe)
    {
        if (probe.As is not { Length: > 0 } person || probe.Status is < 200 or > 299)
        {
            return true;
        }

        if (SessionFor(config, baseUrl, sessions, failed, probes, findings, null) is not { } anonymous)
        {
            return true;
        }

        var request = ResolvedProbeRequest.From(probe, VerifyValues.Of(config), ShortNameOf(person));

        int status;

        try
        {
            (status, _) = anonymous.Send(request);
        }
        catch (ProbeUnreachableException exception)
        {
            findings.Add(Unreachable(probe, exception));
            return false;
        }

        if (status is >= 200 and <= 299)
        {
            findings.Add(new Finding(
                "access-limit-failed",
                $"Probe '{probe.ScenarioId}' expects {probe.Status} as {person} and answers {status} with no session; an anonymous caller reaches the use case.",
                probe.File,
                probe.Line,
                probe.ScenarioId));
        }

        return true;
    }

    /// <summary>Sends one probe of a keyed or stateful command five times at once.</summary>
    /// <param name="config">The consumer configuration holding the verify block.</param>
    /// <param name="baseUrl">The API host address.</param>
    /// <param name="sessions">The sessions by cast person, with the anonymous session under the empty key.</param>
    /// <param name="failed">The session keys whose sign-in already failed.</param>
    /// <param name="probes">Every probe, for the sign-in failure findings.</param>
    /// <param name="findings">The findings the run raised so far.</param>
    /// <param name="probe">The probe to replay.</param>
    /// <param name="raceOwners">The use-case ids whose command carries key or state idempotency.</param>
    /// <returns>False when the host could not be reached, so the caller skips the input matrix of this probe.</returns>
    private static bool RunRace(
        ConsumerConfig config,
        string baseUrl,
        Dictionary<string, ProbeSession> sessions,
        HashSet<string> failed,
        IReadOnlyList<ProbeCase> probes,
        List<Finding> findings,
        ProbeCase probe,
        IReadOnlySet<string> raceOwners)
    {
        // Only a write can run twice: a read probe on a command's scenario reads back what the command left, and a
        // read whose answer names its read moment differs per call by design.
        if (!raceOwners.Contains(probe.OwnerId) || probe.Method is "GET" or "HEAD" or "OPTIONS")
        {
            return true;
        }

        if (SessionFor(config, baseUrl, sessions, failed, probes, findings, probe.As) is not { } session)
        {
            return true;
        }

        var request = ResolvedProbeRequest.From(probe, VerifyValues.Of(config), ShortNameOf(probe.As));
        var outcomes = new (bool Reached, int Status, string Body)[RaceRepeatCount];

        Parallel.For(0, RaceRepeatCount, index =>
        {
            try
            {
                var answered = session.Send(request);
                outcomes[index] = (true, answered.Status, answered.Body);
            }
            catch (ProbeUnreachableException)
            {
                outcomes[index] = (false, 0, string.Empty);
            }
        });

        if (outcomes.Any(outcome => !outcome.Reached))
        {
            findings.Add(new Finding(
                "probe-unreachable",
                $"Probe '{probe.ScenarioId}' could not reach the host for the parallel repeat.",
                probe.File,
                probe.Line,
                probe.ScenarioId));
            return false;
        }

        var failedRepeat = outcomes.FirstOrDefault(outcome => outcome.Status is >= 500 and <= 599);

        if (failedRepeat != default)
        {
            findings.Add(new Finding(
                "idempotency-failed",
                $"Probe '{probe.ScenarioId}' answered {failedRepeat.Status} to one of five parallel requests; an idempotent command returns the first result instead.",
                probe.File,
                probe.Line,
                probe.ScenarioId));
            return true;
        }

        var bodies = outcomes
            .Where(outcome => outcome.Status is >= 200 and <= 299)
            .Select(outcome => outcome.Body)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (bodies.Count > 1)
        {
            findings.Add(new Finding(
                "idempotency-failed",
                $"Probe '{probe.ScenarioId}' answered five parallel requests with {bodies.Count} different success bodies; an idempotent command returns one result.",
                probe.File,
                probe.Line,
                probe.ScenarioId));
        }

        return true;
    }

    /// <summary>Replays one probe with each input variant when its body is a JSON object.</summary>
    /// <param name="config">The consumer configuration holding the verify block.</param>
    /// <param name="baseUrl">The API host address.</param>
    /// <param name="sessions">The sessions by cast person, with the anonymous session under the empty key.</param>
    /// <param name="failed">The session keys whose sign-in already failed.</param>
    /// <param name="probes">Every probe, for the sign-in failure findings.</param>
    /// <param name="findings">The findings the run raised so far.</param>
    /// <param name="probe">The probe to replay.</param>
    private static void RunInput(
        ConsumerConfig config,
        string baseUrl,
        Dictionary<string, ProbeSession> sessions,
        HashSet<string> failed,
        IReadOnlyList<ProbeCase> probes,
        List<Finding> findings,
        ProbeCase probe)
    {
        if (probe.Body is null)
        {
            return;
        }

        if (ResolvedProbeRequest.ResolveBody(probe.Body, VerifyValues.Of(config), ShortNameOf(probe.As)) is not { } resolved
            || JsonNode.Parse(resolved) is not { } body)
        {
            return;
        }

        var variants = AdversarialVariants.Build(body);

        if (variants.Count == 0)
        {
            return;
        }

        if (SessionFor(config, baseUrl, sessions, failed, probes, findings, probe.As) is not { } session)
        {
            return;
        }

        var template = ResolvedProbeRequest.From(probe, VerifyValues.Of(config), ShortNameOf(probe.As));

        foreach (var (label, bodyJson) in variants)
        {
            int status;

            try
            {
                (status, _) = session.Send(new ResolvedProbeRequest(template.Method, template.Path, bodyJson));
            }
            catch (ProbeUnreachableException exception)
            {
                findings.Add(Unreachable(probe, exception));
                return;
            }

            if (status is >= 500 and <= 599)
            {
                findings.Add(new Finding(
                    "input-handling-failed",
                    $"Probe '{probe.ScenarioId}' variant '{label}' answered {status}; the product must validate the input.",
                    probe.File,
                    probe.Line,
                    probe.ScenarioId));
            }
            else if (status is >= 200 and <= 299)
            {
                findings.Add(new Finding(
                    "input-validation-failed",
                    $"Probe '{probe.ScenarioId}' variant '{label}' succeeds with the wrong type; the product must refuse it.",
                    probe.File,
                    probe.Line,
                    probe.ScenarioId));
            }
        }
    }

    /// <summary>Gets the session of one cast person, signing the person in once.</summary>
    /// <param name="config">The consumer configuration holding the verify block.</param>
    /// <param name="baseUrl">The API host address.</param>
    /// <param name="sessions">The sessions by cast person, with the anonymous session under the empty key.</param>
    /// <param name="failed">The session keys whose sign-in already failed.</param>
    /// <param name="probes">Every probe, for the sign-in failure findings.</param>
    /// <param name="findings">The findings the run raised so far.</param>
    /// <param name="person">The cast person id, or null for anonymous.</param>
    /// <returns>The session, or null when the sign-in failed and every probe of the person was reported.</returns>
    private static ProbeSession? SessionFor(
        ConsumerConfig config,
        string baseUrl,
        Dictionary<string, ProbeSession> sessions,
        HashSet<string> failed,
        IReadOnlyList<ProbeCase> probes,
        List<Finding> findings,
        string? person)
    {
        var key = person ?? string.Empty;

        if (sessions.TryGetValue(key, out var known))
        {
            return known;
        }

        if (failed.Contains(key))
        {
            return null;
        }

        try
        {
            var session = ProbeSession.SignIn(person, baseUrl, config.Verify.SignIn, VerifyValues.Of(config), config.Verify.Headers);
            sessions[key] = session;
            return session;
        }
        catch (ProbeUnreachableException exception)
        {
            failed.Add(key);

            foreach (var probe in probes.Where(candidate => (candidate.As ?? string.Empty) == key))
            {
                findings.Add(new Finding(
                    "probe-unreachable",
                    $"Probe '{probe.ScenarioId}' could not sign '{person ?? "anonymous"}' in: {ShortReason(exception.Message)}",
                    probe.File,
                    probe.Line,
                    probe.ScenarioId));
            }

            return null;
        }
    }

    /// <summary>The unreachable finding for one probe whose replay never reached the host.</summary>
    /// <param name="probe">The probe whose replay failed.</param>
    /// <param name="exception">The failure that stopped the request.</param>
    /// <returns>The probe-unreachable finding.</returns>
    private static Finding Unreachable(ProbeCase probe, ProbeUnreachableException exception) => new(
        "probe-unreachable",
        $"Probe '{probe.ScenarioId}' could not reach the host: {ShortReason(exception.Message)}",
        probe.File,
        probe.Line,
        probe.ScenarioId);

    /// <summary>The short person name without the person/ prefix, or null for anonymous.</summary>
    /// <param name="person">The cast person id, or null for anonymous.</param>
    /// <returns>The name without the prefix, the id when it carries none, or null for anonymous.</returns>
    private static string? ShortNameOf(string? person) =>
        person is null
            ? null
            : person.StartsWith("person/", StringComparison.Ordinal) ? person["person/".Length..] : person;

    /// <summary>The reason as one trimmed sentence without the trailing stop.</summary>
    /// <param name="message">The full failure message.</param>
    /// <returns>The trimmed message.</returns>
    private static string ShortReason(string message)
    {
        var trimmed = message.Trim();

        return trimmed.EndsWith('.') ? trimmed[..^1] : trimmed;
    }
}
