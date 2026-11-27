using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     The values a probe path or body names as {name}: the fixed <c>verify.values</c>, overlaid by the JSON object the
///     <c>verify.valuesFrom</c> command prints, such as the identities a fresh seed created. The command runs once per
///     configuration.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal static class VerifyValues
{
    /// <summary>The longest time the values command may run.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    /// <summary>The resolved values, or the reason they could not be read, per configuration.</summary>
    private static readonly ConditionalWeakTable<ConsumerConfig, Resolved> Cache = new();

    /// <summary>Returns the values of the configuration, running the values command on first use.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The merged values.</returns>
    /// <exception cref="ProbeUnreachableException">The values command failed or printed no JSON object.</exception>
    internal static IReadOnlyDictionary<string, string> Of(ConsumerConfig config)
    {
        var resolved = Cache.GetValue(config, Resolve);

        return resolved.Error is { } error ? throw new ProbeUnreachableException(error) : resolved.Values;
    }

    /// <summary>Merges the fixed values with the output of the values command.</summary>
    /// <param name="config">The consumer configuration.</param>
    /// <returns>The merged values, or the reason the command output could not be read.</returns>
    private static Resolved Resolve(ConsumerConfig config)
    {
        var values = new Dictionary<string, string>(config.Verify.Values, StringComparer.Ordinal);

        if (config.Verify.ValuesFrom is not { } command)
        {
            return new Resolved(values, null);
        }

        var run = ShellCommand.Run(command, config.Root, Timeout);
        var output = run.Stdout;

        if (run.Status != 0)
        {
            return new Resolved(values, $"The verify.valuesFrom command exited with {run.Status}: {run.Error.Trim()}");
        }

        try
        {
            if (JsonNode.Parse(output) is not JsonObject printed)
            {
                return new Resolved(values, "The verify.valuesFrom command printed no JSON object.");
            }

            foreach (var (name, value) in printed)
            {
                if (value is JsonValue scalar)
                {
                    values[name] = scalar.GetValueKind() == JsonValueKind.String ? scalar.GetValue<string>() : scalar.ToJsonString();
                }
            }
        }
        catch (JsonException exception)
        {
            return new Resolved(values, $"The verify.valuesFrom command printed no JSON object: {exception.Message}");
        }

        return new Resolved(values, null);
    }

    /// <summary>The merged values, or the reason the values command could not be read.</summary>
    /// <param name="Values">The merged values.</param>
    /// <param name="Error">The reason, or null when the values read.</param>
    private sealed record Resolved(IReadOnlyDictionary<string, string> Values, string? Error);
}
