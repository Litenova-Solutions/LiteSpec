using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     Facts read straight from a use case record: its access grant, whether its
///     inputs carry rules, whether it runs in the background, and the aggregate
///     whose folder holds its page.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class UseCaseFacts
{
    /// <summary>Whether the use case lets anonymous callers in.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when the use case's access grant is anonymous.</returns>
    public static bool Anonymous(Record useCase) => GrantOf(useCase) == "anonymous";

    /// <summary>
    ///     The permission the use case's access grant names, reading through self(),
    ///     or null when the grant is anonymous, system, or missing.
    /// </summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The permission the grant names, or null when it names none.</returns>
    public static string? GrantedPermission(Record useCase)
    {
        if (GrantOf(useCase) is not { } grant)
        {
            return null;
        }

        var permission = UnwrapSelf(grant);

        return permission.StartsWith("permission/", StringComparison.Ordinal) ? permission : null;
    }

    /// <summary>Whether any of the use case's inputs carries rules.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when one of the use case's inputs carries rules.</returns>
    public static bool HasValidatedInput(Record useCase)
    {
        if (useCase.Raw.ValueKind != System.Text.Json.JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Input, out var input)
            || input.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in input.EnumerateArray())
        {
            if (entry.ValueKind != System.Text.Json.JsonValueKind.Object
                || !entry.TryGetProperty("rules", out var rules))
            {
                continue;
            }

            if (rules.ValueKind == System.Text.Json.JsonValueKind.String && rules.GetString() is { Length: > 0 })
            {
                return true;
            }

            if (rules.ValueKind == System.Text.Json.JsonValueKind.Array && rules.GetArrayLength() > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the use case runs in the background: a reaction or job runs it.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>True when a reaction or job runs the use case.</returns>
    public static bool BackgroundOnly(ProjectModel model, Record useCase) =>
        model.Records.Any(record => record.Type is RecordTypes.Reaction or RecordTypes.Job
            && record.EdgeIds(RecordFields.Runs).Contains(useCase.Id, StringComparer.Ordinal));

    /// <summary>The aggregate whose folder holds the use case's page.</summary>
    /// <param name="model">The project model.</param>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The aggregate holding the use case's page, or null when none holds it.</returns>
    public static Record? AggregateOf(ProjectModel model, Record useCase)
    {
        if (useCase.Type != RecordTypes.UseCase)
        {
            return null;
        }

        var folder = Path.GetDirectoryName(useCase.File);

        return model.Records
            .Where(record => record.Type == RecordTypes.Aggregate
                && string.Equals(Path.GetDirectoryName(record.File), folder, StringComparison.Ordinal))
            .OrderBy(record => record.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>The use case's access grant as written.</summary>
    /// <param name="useCase">The use case record.</param>
    /// <returns>The access grant as written, or null when the use case declares none.</returns>
    private static string? GrantOf(Record useCase) =>
        useCase.Raw.ValueKind == System.Text.Json.JsonValueKind.Object
        && useCase.Raw.TryGetProperty(RecordFields.Access, out var access)
        && access.ValueKind == System.Text.Json.JsonValueKind.Object
        && access.TryGetProperty("grant", out var grant)
        && grant.ValueKind == System.Text.Json.JsonValueKind.String
            ? grant.GetString()
            : null;

    /// <summary>The permission inside a self() grant, or the value itself.</summary>
    /// <param name="value">The grant value as written.</param>
    /// <returns>The permission inside a self() grant, or the value itself.</returns>
    internal static string UnwrapSelf(string value) =>
        value.StartsWith("self(", StringComparison.Ordinal)
        && value.EndsWith(')')
        && value.Length > 6
            ? value[5..^1]
            : value;
}
