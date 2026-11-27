using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema effect shapes: every idempotency key templates its record inputs, every reaction or job runs at most one use case, and every failure policy states a mode object with a retry that names its attempts, delays and dead letter.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
internal static class SchemaEffectShapes
{
    /// <summary>The key scopes an idempotency key may name.</summary>
    private static readonly string[] IdempotencyScopes = ["tenant", "account", "login", "global"];

    /// <summary>A {field} placeholder inside an idempotency key template.</summary>
    static readonly Regex IdempotencyPlaceholder = new(
        @"\{(?<name>[^{}]+)\}",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     The idempotency key and scope: the key templates {field} placeholders over
    ///     the record's inputs field names, and the scope names one of the four tenancies.
    /// </summary>
    /// <param name="record">The record whose idempotency key and scope are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckIdempotency(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (idempotency.TryGetProperty(RecordFields.Scope, out var scope)
            && scope.ValueKind == JsonValueKind.String
            && scope.GetString() is { } scopeName
            && !IdempotencyScopes.Contains(scopeName, StringComparer.Ordinal))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency scope '{scopeName}' outside {string.Join(", ", IdempotencyScopes)}.", record.File, record.Line, SubjectOf(record)));
        }

        if (!idempotency.TryGetProperty(RecordFields.Key, out var key)
            || key.ValueKind != JsonValueKind.String
            || key.GetString() is not { } template)
        {
            return;
        }

        var fields = InputNames(record);
        var inScope = fields.Count > 0 ? string.Join(", ", fields) : "none";

        var placeholders = IdempotencyPlaceholder.Matches(template)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (placeholders.Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds idempotency key '{template}' with no {{field}} placeholder and input fields in scope: {inScope}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        // A placeholder naming no carried field is reported by IdempotencyKeyChecks, which
        // reads the triggering event fields of a reaction and the run window of a job as
        // well as the input fields of a use case.
    }

    /// <summary>The record's inputs field names, in written order.</summary>
    /// <param name="record">The record whose inputs array is read.</param>
    /// <returns>The inputs field names in written order, or an empty list when the record holds no inputs array.</returns>
    internal static IReadOnlyList<string> InputNames(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Inputs, out var input)
            || input.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. input.EnumerateArray()
                      .Where(entry => entry.ValueKind == JsonValueKind.Object
                          && entry.TryGetProperty(RecordFields.Name, out var name)
                          && name.ValueKind == JsonValueKind.String
                          && name.GetString() is { Length: > 0 })
                      .Select(entry => entry.GetProperty(RecordFields.Name).GetString()!),
        ];
    }

    /// <summary>
    ///     Reports a reaction or job whose effects hold more than one runs entry: it runs at most one use case. The
    ///     onRefusal use case an entry names does not count, since it runs only after a refusal that committed nothing,
    ///     and an onRefusal on an entry with no runs target reports.
    /// </summary>
    /// <param name="record">The reaction or job record whose effects are counted.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job</follows>
    internal static void CheckRunsAtMostOne(Record record, List<Finding> findings)
    {
        if (!record.Raw.TryGetProperty(RecordFields.Effects, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var runs = 0;

        foreach (var entry in list.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var running = entry.TryGetProperty(RecordFields.Runs, out _);

            if (running)
            {
                runs++;
            }
            else if (entry.TryGetProperty(RecordFields.OnRefusal, out _))
            {
                findings.Add(new Finding("record-invalid", $"Record '{record.Id}' names an onRefusal use case on an effect that runs no use case; only a run use case can refuse.", record.File, record.Line, SubjectOf(record)));
            }
        }

        if (runs > 1)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' has {runs} runs effects; a reaction or job runs at most one use case.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>
    ///     The reaction or job failure handling: a mode object naming retry with its
    ///     attempts and delays, or skip-and-log, or discard. Prose and malformed values report.
    /// </summary>
    /// <param name="record">The reaction or job record whose failure handling is judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckFailurePolicy(Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.OnFailure, out var policy))
        {
            CheckOnFailureObject(record, policy, "onFailure", findings);
        }

        CheckEffectFailurePolicy(record, findings);
    }

    /// <summary>One structured onFailure for every edge: each effect entry carries the same mode object.</summary>
    /// <param name="record">The record whose effect entries carry the failure policies under test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckEffectFailurePolicy(Record record, List<Finding> findings)
    {
        foreach (var entry in EffectEntries(record))
        {
            if (entry.TryGetProperty(RecordFields.OnFailure, out var policy))
            {
                CheckOnFailureObject(record, policy, "effect onFailure", findings);
            }
        }
    }

    /// <summary>The failure mode object: retry with attempts, delays and dead-letter, or a terminal mode.</summary>
    /// <param name="record">The record that holds the failure policy.</param>
    /// <param name="policy">The failure policy value under test.</param>
    /// <param name="where">The policy location description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckOnFailureObject(Record record, JsonElement policy, string where, List<Finding> findings)
    {
        if (policy.ValueKind == JsonValueKind.String)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} '{policy.GetString()}' as prose with no mode object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (policy.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} as no object.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!policy.TryGetProperty(RecordFields.Mode, out var mode)
            || mode.ValueKind != JsonValueKind.String
            || mode.GetString() is not { } shape)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no mode.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (shape is not ("retry" or "skip-and-log" or "discard"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} mode '{shape}' outside retry, skip-and-log, discard.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (shape != "retry")
        {
            return;
        }

        if (!policy.TryGetProperty(RecordFields.Retry, out var retry) || retry.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no retry.", record.File, record.Line, SubjectOf(record)));
        }
        else
        {
            CheckRetryObject(record, retry, where, findings);
        }

        if (!policy.TryGetProperty(RecordFields.IfExhausted, out var then)
            || then.ValueKind != JsonValueKind.String
            || then.GetString() is not { Length: > 0 })
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no ifExhausted.", record.File, record.Line, SubjectOf(record)));
        }
        else if (then.GetString() != "dead-letter")
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} ifExhausted '{then.GetString()}' outside dead-letter.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The retry object: positive attempts, ISO 8601 delays and a backoff of exponential or fixed.</summary>
    /// <param name="record">The record that holds the failure policy.</param>
    /// <param name="retry">The retry object under test.</param>
    /// <param name="where">The policy location description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckRetryObject(Record record, JsonElement retry, string where, List<Finding> findings)
    {
        if (!retry.TryGetProperty(RecordFields.MaxAttempts, out var attempts)
            || attempts.ValueKind != JsonValueKind.Number
            || !attempts.TryGetInt32(out var count)
            || count < 1)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no positive maxAttempts.", record.File, record.Line, SubjectOf(record)));
        }

        CheckPolicyDuration(record, retry, "initialDelay", $"{where} retry initialDelay", findings);
        CheckPolicyDuration(record, retry, "maxDelay", $"{where} retry maxDelay", findings);

        if (!retry.TryGetProperty(RecordFields.Backoff, out var backoff)
            || backoff.ValueKind != JsonValueKind.String
            || backoff.GetString() is not { } shape)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry with no backoff.", record.File, record.Line, SubjectOf(record)));
        }
        else if (shape is not ("exponential" or "fixed"))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} retry backoff '{shape}' outside exponential, fixed.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>One ISO 8601 duration inside the failure policy: missing or malformed reports.</summary>
    /// <param name="record">The record that holds the failure policy.</param>
    /// <param name="holder">The policy object that holds the duration field.</param>
    /// <param name="field">The duration field name under test.</param>
    /// <param name="where">The duration location description quoted in each finding.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckPolicyDuration(Record record, JsonElement holder, string field, string where, List<Finding> findings)
    {
        if (!holder.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.String
            || value.GetString() is not { } text)
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} with no duration.", record.File, record.Line, SubjectOf(record)));
        }
        else if (!IsoDuration.IsMatch(text))
        {
            findings.Add(new Finding("record-invalid", $"Record '{record.Id}' holds {where} '{text}', which is not an ISO 8601 duration.", record.File, record.Line, SubjectOf(record)));
        }
    }
}
