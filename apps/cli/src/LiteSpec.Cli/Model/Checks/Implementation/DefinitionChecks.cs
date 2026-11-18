using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The definition check: every message declares its pipeline positions beside the message, and each declaration matches its use case record.</summary>
/// <follows>litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</follows>
/// <follows>litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class</follows>
/// <follows>litespec/workspace/design#name-each-definition-after-its-message-and-concern</follows>
internal static class DefinitionChecks
{
    /// <summary>
    ///     Reports every message without its definitions, and every definition that disagrees with its use case
    ///     record: the authorization policy against access.grant, the audit declaration against audit, and the
    ///     idempotency declaration against idempotency. Definitions carry no mark; each use case's definitions
    ///     are found from the message type its handler handles, in the handler's own Application folder.
    ///     A use case without a handler keeps no definition rule, and a query keeps only authorization.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each handler with its message type.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.UseCaseId.Length == 0 || handler.MessageType.Length == 0)
            {
                continue;
            }

            if (!model.ById.TryGetValue(handler.UseCaseId, out var record)
                || record.Type != RecordTypes.UseCase
                || record.Derived)
            {
                continue;
            }

            var folder = Path.GetDirectoryName(handler.File);

            if (folder is null)
            {
                continue;
            }

            CheckAuthorization(model, handler, record, folder, findings);
            CheckAudit(handler, record, folder, findings);
            CheckIdempotency(handler, record, folder, findings);
        }
    }

    /// <summary>Reports the missing authorization definition, and the authorization policy that misses the record's grant.</summary>
    /// <param name="model">The loaded project model that holds the permission records.</param>
    /// <param name="handler">The handler whose message names the definition.</param>
    /// <param name="record">The use case record holding access.grant.</param>
    /// <param name="folder">The Application folder holding the handler and its definitions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAuthorization(ProjectModel model, CodeHandler handler, Record record, string folder, List<Finding> findings)
    {
        var message = handler.MessageType;
        var definition = message + "AuthorizationDefinition";
        var file = Path.Combine(folder, definition + ".cs");

        if (!File.Exists(file))
        {
            findings.Add(new Finding(
                "definition-missing",
                $"Message '{message}' has no Authorization definition for '{record.Id}'.",
                handler.File,
                1,
                record.Id));
            return;
        }

        if (GrantOf(record) is not { } grant)
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        if (grant == "anonymous")
        {
            if (!text.Contains("Exempt", StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares no exemption for the anonymous grant of '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }

            return;
        }

        if (grant == "system")
        {
            if (!text.Contains(".System", StringComparison.Ordinal)
                && !text.Contains("System)", StringComparison.Ordinal)
                && !text.Contains("System,", StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares no System policy for '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }

            return;
        }

        if (grant == "signed-in")
        {
            if (text.Contains("Exempt", StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares an exemption, but '{record.Id}' grants signed-in.",
                    file,
                    1,
                    record.Id));
            }
            else if (!text.Contains("Authorized", StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares no authorization for the signed-in grant of '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }
        }
        else if (grant.StartsWith("self(permission/", StringComparison.Ordinal) && grant.EndsWith(')'))
        {
            var permission = grant["self(permission/".Length..^1];

            if (!text.Contains("Authorized", StringComparison.Ordinal)
                || !text.Contains("Self", StringComparison.Ordinal)
                || !text.Contains(permission, StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares no Self policy for '{grant}' of '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }
        }
        else if (grant.StartsWith("permission/", StringComparison.Ordinal))
        {
            var permission = grant["permission/".Length..];

            if (!text.Contains("Authorized", StringComparison.Ordinal) || !text.Contains(permission, StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares no permission '{permission}' for '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }
        }

        if (RecentSignInLimitOf(record) is not null && !text.Contains("RecentSignIn", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Authorization definition '{definition}' declares no RecentSignIn policy for '{record.Id}'.",
                file,
                1,
                record.Id));
        }
    }

    /// <summary>Reports the missing audit definition, and the audit declaration that misses the record's audit position.</summary>
    /// <param name="handler">The handler whose message names the definition.</param>
    /// <param name="record">The use case record holding audit.</param>
    /// <param name="folder">The Application folder holding the handler and its definitions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAudit(CodeHandler handler, Record record, string folder, List<Finding> findings)
    {
        if (!record.Has(RecordFields.Audit))
        {
            return;
        }

        var message = handler.MessageType;
        var definition = message + "AuditDefinition";
        var file = Path.Combine(folder, definition + ".cs");

        if (!File.Exists(file))
        {
            findings.Add(new Finding(
                "definition-missing",
                $"Message '{message}' has no Audit definition for '{record.Id}'.",
                handler.File,
                1,
                record.Id));
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        if (AuditExemptOf(record))
        {
            if (!text.Contains("NotAudited", StringComparison.Ordinal)
                && !text.Contains("Exempt", StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Audit definition '{definition}' declares no audit exemption for '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }

            return;
        }

        if (text.Contains("NotAudited", StringComparison.Ordinal)
            || text.Contains(".Exempt(", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Audit definition '{definition}' declares an audit exemption, but '{record.Id}' requires audit.",
                file,
                1,
                record.Id));
            return;
        }

        if (!text.Contains("Audited", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Audit definition '{definition}' declares no audit for '{record.Id}'.",
                file,
                1,
                record.Id));
            return;
        }

        if (!text.Contains("WithTargetFrom", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Audit definition '{definition}' declares no target for '{record.Id}'.",
                file,
                1,
                record.Id));
        }

        if (!text.Contains(record.Id, StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Audit definition '{definition}' declares no action '{record.Id}'.",
                file,
                1,
                record.Id));
        }
    }

    /// <summary>Reports the missing idempotency definition, and the idempotency declaration that misses the record's mode.</summary>
    /// <param name="handler">The handler whose message names the definition.</param>
    /// <param name="record">The use case record holding idempotency.</param>
    /// <param name="folder">The Application folder holding the handler and its definitions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIdempotency(CodeHandler handler, Record record, string folder, List<Finding> findings)
    {
        if (!record.Has(RecordFields.Idempotency))
        {
            return;
        }

        var message = handler.MessageType;
        var definition = message + "IdempotencyDefinition";
        var file = Path.Combine(folder, definition + ".cs");

        if (!File.Exists(file))
        {
            findings.Add(new Finding(
                "definition-missing",
                $"Message '{message}' has no Idempotency definition for '{record.Id}'.",
                handler.File,
                1,
                record.Id));
            return;
        }

        if (IdempotencyModeOf(record) is not { } mode)
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        if (mode == "none")
        {
            if (!text.Contains("NotIdempotent", StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Idempotency definition '{definition}' declares no NotIdempotent exemption for '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }

            return;
        }

        if (text.Contains("NotIdempotent", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Idempotency definition '{definition}' declares NotIdempotent, but '{record.Id}' requires idempotency.",
                file,
                1,
                record.Id));
            return;
        }

        if (!text.Contains("Idempotent", StringComparison.Ordinal)
            && !text.Contains("IdempotencyDeclaration", StringComparison.Ordinal)
            && !text.Contains("IIdempotencyDefinition", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Idempotency definition '{definition}' declares no idempotency for '{record.Id}'.",
                file,
                1,
                record.Id));
        }
    }

    /// <summary>The grant the use case record states in access.grant, or null when it states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The grant string, or null.</returns>
    private static string? GrantOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object
            || !access.TryGetProperty(RecordFields.Grant, out var grant)
            || grant.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return grant.GetString();
    }

    /// <summary>The recent sign-in window the use case record states, or null when it states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The setting id, or null.</returns>
    private static string? RecentSignInLimitOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object
            || !access.TryGetProperty("recentSignInLimit", out var window)
            || window.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return window.GetString();
    }

    /// <summary>Whether the use case record exempts audit: an exemptBecause, or level none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>True when the record exempts audit.</returns>
    private static bool AuditExemptOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Audit, out var audit)
            || audit.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (audit.TryGetProperty(RecordFields.ExemptBecause, out _))
        {
            return true;
        }

        return audit.TryGetProperty(RecordFields.Level, out var level)
            && level.ValueKind == JsonValueKind.String
            && level.GetString() == "none";
    }

    /// <summary>The idempotency mode the use case record states, or null when it states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The mode string, or null.</returns>
    private static string? IdempotencyModeOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object
            || !idempotency.TryGetProperty(RecordFields.Mode, out var mode)
            || mode.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return mode.GetString();
    }
}
