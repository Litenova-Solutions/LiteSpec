using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The definition check: every message declares its pipeline positions beside the message, and each declaration matches its use case record.</summary>
/// <follows>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</follows>
/// <follows>litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class</follows>
/// <follows>litespec/workspace/design#name-each-definition-after-its-message-and-concern</follows>
internal static class DefinitionChecks
{
    /// <summary>
    ///     Reports every message without its definitions, every definition that disagrees with its use case
    ///     record: the authorization policy against access.grant, the audit declaration against audit, the
    ///     tenancy declaration against access.tenant, and the idempotency declaration against idempotency,
    ///     and every hand-enforced authorization or tenancy policy no record declares. Definitions carry no
    ///     mark; each use case's definitions are found from the message type its handler handles, in the
    ///     handler's own Application folder. A use case without a handler keeps no definition rule, and a
    ///     query keeps only authorization and tenancy.
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
            CheckTenancy(handler, record, folder, findings);
            CheckIdempotency(handler, record, folder, findings);
        }

        CheckHandPolicies(model, findings);
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
            // An anonymous grant is a grant like any other: the definition either exempts
            // the message or authorizes the anonymous policy, as it authorizes the system
            // policy for a system grant.
            var authorizesAnonymous = text.Contains("Authorized", StringComparison.Ordinal)
                && (text.Contains("Anonymous", StringComparison.Ordinal)
                    || text.Contains("anonymous", StringComparison.Ordinal));

            if (!text.Contains("Exempt", StringComparison.Ordinal) && !authorizesAnonymous)
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

            // A definition may name the permission through its constant, such as BooksPermissions.PostEntry
            // for permission/post-entry, rather than writing the literal.
            if (!text.Contains("Authorized", StringComparison.Ordinal)
                || (!text.Contains(permission, StringComparison.Ordinal)
                    && !text.Contains(Naming.Pascal(permission), StringComparison.Ordinal)))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Authorization definition '{definition}' declares no permission '{permission}' for '{record.Id}'.",
                    file,
                    1,
                    record.Id));
            }
        }

        if (RecentSignInOf(record) is not null && !text.Contains("RecentSignIn", StringComparison.Ordinal))
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
            if (AuditExemptOf(record))
            {
                return;
            }

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
            // A preparatory command of a use case may exempt itself with a reason naming
            // the use case, when a sibling definition audits that use case's action.
            if (!CoveredBySiblingAudit(folder, file, text, record))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    $"Audit definition '{definition}' declares an audit exemption, but '{record.Id}' requires audit.",
                    file,
                    1,
                    record.Id));
            }

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

        // The handler adds the target it learns while running through the audit
        // scope, so a definition without a target extraction reads beside it.
        if (!text.Contains("WithTargetFrom", StringComparison.Ordinal) && !HandlerAddsTarget(handler))
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

    /// <summary>Reports the missing tenancy definition, and the tenancy declaration that misses the record's tenant reach or reason.</summary>
    /// <param name="handler">The handler whose message names the definition.</param>
    /// <param name="record">The use case record holding access.tenant.</param>
    /// <param name="folder">The Application folder holding the handler and its definitions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTenancy(CodeHandler handler, Record record, string folder, List<Finding> findings)
    {
        if (TenantOf(record) is not { } tenant)
        {
            return;
        }

        var message = handler.MessageType;
        var definition = message + "TenancyDefinition";
        var file = Path.Combine(folder, definition + ".cs");

        if (!File.Exists(file))
        {
            findings.Add(new Finding(
                "definition-missing",
                $"Message '{message}' has no Tenancy definition for '{record.Id}'.",
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

        if (!text.Contains("ITenancyDefinition", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Tenancy definition '{definition}' implements no ITenancyDefinition for '{record.Id}'.",
                file,
                1,
                record.Id));
            return;
        }

        if (TenantMarker(tenant) is not { } marker || !text.Contains(marker, StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Tenancy definition '{definition}' declares no {tenant} tenancy for '{record.Id}'.",
                file,
                1,
                record.Id));
            return;
        }

        if (TenantReasonOf(record) is { } reason && !text.Contains(reason, StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Tenancy definition '{definition}' states no reason '{reason}' for the {tenant} tenant of '{record.Id}'.",
                file,
                1,
                record.Id));
        }
    }

    /// <summary>The tenant reach the use case record states in access.tenant, or null when it states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The tenant reach string, or null.</returns>
    private static string? TenantOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object
            || !access.TryGetProperty(RecordFields.Tenant, out var tenant)
            || tenant.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return tenant.GetString();
    }

    /// <summary>The tenant reason the use case record states in access.tenantReason, or null when it states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The tenant reason string, or null.</returns>
    private static string? TenantReasonOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object
            || !access.TryGetProperty(RecordFields.TenantReason, out var reason)
            || reason.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return reason.GetString();
    }

    /// <summary>The declaration marker a tenancy text carries for a tenant reach, or null for an unknown reach.</summary>
    /// <param name="tenant">The tenant reach the use case record states.</param>
    /// <returns>The marker naming the reach in a TenancyDeclaration, or null.</returns>
    private static string? TenantMarker(string tenant) => tenant switch
    {
        "own" => "Required",
        "cross" => "CrossTenant",
        "none" => ".None(",
        "public" => "Public",
        _ => null,
    };

    /// <summary>Reports every authorization policy and tenancy attribute the endpoint and handler code enforces by hand that no record declares.</summary>
    /// <param name="model">The loaded project model that holds every permission record, use case grant, endpoint and handler.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckHandPolicies(ProjectModel model, List<Finding> findings)
    {
        var declared = DeclaredPolicies(model);

        var files = model.Code.Endpoints.Select(endpoint => endpoint.File)
            .Concat(model.Code.Handlers.Select(handler => handler.File))
            .Where(file => file.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            string[] lines;

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            for (var index = 0; index < lines.Length; index++)
            {
                CheckHandPolicyLine(model, file, index + 1, lines[index].Trim(), declared, findings);
            }
        }
    }

    /// <summary>Reports a hand-enforced policy one code line names that no record declares.</summary>
    /// <param name="model">The loaded project model that supplies the consumer root for relative paths.</param>
    /// <param name="file">The code file holding the line.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <param name="text">The trimmed code line.</param>
    /// <param name="declared">The policy names the records declare.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckHandPolicyLine(ProjectModel model, string file, int line, string text, HashSet<string> declared, List<Finding> findings)
    {
        if (text.Contains("[TenantRequired", StringComparison.Ordinal) || text.Contains("[CrossTenant", StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "policy-undeclared",
                $"Code enforces tenancy by hand in '{PathText.Relative(model.Config.Root, file)}', which no record declares. Declare the tenant reach on the use case record and write its Tenancy definition.",
                file,
                line,
                null));
            return;
        }

        var policy = PolicyOf(text);

        if (policy is null || declared.Contains(policy))
        {
            return;
        }

        findings.Add(new Finding(
            "policy-undeclared",
            $"Code enforces authorization policy '{policy}' in '{PathText.Relative(model.Config.Root, file)}', which no record declares. Declare the policy on a use case grant or a permission record, or stop enforcing it.",
            file,
            line,
            null));
    }

    /// <summary>The policy name one code line enforces by hand, or null when it enforces none.</summary>
    /// <param name="text">The trimmed code line.</param>
    /// <returns>The enforced policy name, or null.</returns>
    private static string? PolicyOf(string text)
    {
        var start = text.IndexOf("RequireAuthorization(", StringComparison.Ordinal);

        if (start >= 0)
        {
            return QuotedAfter(text, start + "RequireAuthorization(".Length);
        }

        var authorize = text.IndexOf("[Authorize", StringComparison.Ordinal);

        if (authorize < 0)
        {
            return null;
        }

        var named = text.IndexOf("Policy", authorize, StringComparison.Ordinal);

        if (named >= 0)
        {
            var equals = text.IndexOf('=', named);

            if (equals >= 0)
            {
                return QuotedAfter(text, equals + 1);
            }
        }

        var open = text.IndexOf('(', authorize);

        return open >= 0 ? QuotedAfter(text, open + 1) : null;
    }

    /// <summary>The first quoted string at or after a position, or null when none follows.</summary>
    /// <param name="text">The code line to read.</param>
    /// <param name="start">The position to read from.</param>
    /// <returns>The quoted string, or null.</returns>
    private static string? QuotedAfter(string text, int start)
    {
        var open = text.IndexOf('"', start);

        if (open < 0)
        {
            return null;
        }

        var close = text.IndexOf('"', open + 1);

        return close > open + 1 ? text[(open + 1)..close] : null;
    }

    /// <summary>Every policy name the records declare: each permission and each use case grant, with the Pascal spelling of each kebab-case name.</summary>
    /// <param name="model">The loaded project model that holds every permission record and use case record.</param>
    /// <returns>The declared policy names.</returns>
    private static HashSet<string> DeclaredPolicies(ProjectModel model)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Type == RecordTypes.PermissionSet
                && record.Raw.ValueKind == JsonValueKind.Object
                && record.Raw.TryGetProperty(RecordFields.Permissions, out var permissions)
                && permissions.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in permissions.EnumerateArray())
                {
                    if (RecordEntries.Str(entry, RecordFields.Id) is { Length: > 0 } id
                        && Identifiers.TrySplit(id, out var prefix, out _, out var name)
                        && prefix == "permission"
                        && name.Length > 0)
                    {
                        declared.Add(name);
                        declared.Add(Naming.Pascal(name));
                    }
                }
            }

            if (record is { Type: RecordTypes.UseCase, Derived: false })
            {
                foreach (var policy in GrantPolicies(record))
                {
                    declared.Add(policy);
                }
            }
        }

        return declared;
    }

    /// <summary>The policy names one use case record declares through its access grant.</summary>
    /// <param name="record">The use case record whose grant is read.</param>
    /// <returns>The declared policy names.</returns>
    private static IEnumerable<string> GrantPolicies(Record record)
    {
        if (GrantOf(record) is not { } grant)
        {
            yield break;
        }

        if (grant.StartsWith("permission/", StringComparison.Ordinal))
        {
            var permission = grant["permission/".Length..];

            yield return permission;
            yield return Naming.Pascal(permission);
        }
        else if (grant.StartsWith("self(permission/", StringComparison.Ordinal) && grant.EndsWith(')'))
        {
            var permission = grant["self(permission/".Length..^1];

            yield return "Self";
            yield return permission;
            yield return Naming.Pascal(permission);
        }
        else if (grant == "anonymous")
        {
            yield return "Anonymous";
        }
        else if (grant == "system")
        {
            yield return "System";
        }

        if (RecentSignInOf(record) is not null)
        {
            yield return "RecentSignIn";
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

        // Mode state recognizes a repeat by the aggregate's state, so the pipeline keys nothing: a stored key would
        // outlive the state it mirrors and replay a stale result after an undo, such as a grant, a revoke and a grant again.
        if (mode is "none" or "state")
        {
            if (!DeclaresIdempotencyExemption(text))
            {
                findings.Add(new Finding(
                    "definition-drift",
                    mode == "none"
                        ? $"Idempotency definition '{definition}' declares no idempotency exemption for '{record.Id}'."
                        : $"Idempotency definition '{definition}' declares no idempotency exemption for '{record.Id}', which recognizes a repeat by state; a pipeline key would outlive that state.",
                    file,
                    1,
                    record.Id));
            }

            return;
        }

        if (text.Contains("NotIdempotent", StringComparison.Ordinal) || DeclaresIdempotencyExemption(text))
        {
            findings.Add(new Finding(
                "definition-drift",
                $"Idempotency definition '{definition}' declares an idempotency exemption, but '{record.Id}' requires idempotency.",
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

    /// <summary>Whether the definition declares the idempotency exemption with its reason: the generic exemption over the idempotency declaration.</summary>
    /// <param name="text">The definition file text.</param>
    /// <returns>True when the text exempts the idempotency declaration; otherwise false.</returns>
    private static bool DeclaresIdempotencyExemption(string text) =>
        text.Contains("Exempt", StringComparison.Ordinal)
        && text.Contains("IdempotencyDeclaration", StringComparison.Ordinal);

    /// <summary>Whether the handler adds the audit target it learns while running through the audit scope.</summary>
    /// <param name="handler">The handler whose file is read.</param>
    /// <returns>True when the handler file calls WithTarget; otherwise false.</returns>
    private static bool HandlerAddsTarget(CodeHandler handler)
    {
        string text;

        try
        {
            text = File.ReadAllText(handler.File);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        return text.Contains("WithTarget(", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Whether a reasoned exemption stands beside a sibling definition that audits the same use case:
    ///     the exemption names the use case it prepares, and another audit definition in the folder audits that action.
    /// </summary>
    /// <param name="folder">The Application folder holding the definitions.</param>
    /// <param name="file">The exempting definition file, skipped among the siblings.</param>
    /// <param name="text">The exempting definition text.</param>
    /// <param name="record">The use case record holding the audited action.</param>
    /// <returns>True when a sibling audits the named action; otherwise false.</returns>
    private static bool CoveredBySiblingAudit(string folder, string file, string text, Record record)
    {
        if (!text.Contains(record.Id, StringComparison.Ordinal))
        {
            return false;
        }

        string[] siblings;

        try
        {
            siblings = Directory.GetFiles(folder, "*AuditDefinition.cs");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        foreach (var sibling in siblings)
        {
            if (string.Equals(sibling, file, StringComparison.Ordinal))
            {
                continue;
            }

            string siblingText;

            try
            {
                siblingText = File.ReadAllText(sibling);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (siblingText.Contains("Audited(\"" + record.Id + "\"", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
    private static string? RecentSignInOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Access, out var access)
            || access.ValueKind != JsonValueKind.Object
            || !access.TryGetProperty(RecordFields.RecentSignInWindow, out var window)
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
