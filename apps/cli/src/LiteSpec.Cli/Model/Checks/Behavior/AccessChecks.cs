using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The access checks: role scopes have their glossary terms, permission names open with a use-case verb, and use-case access declares its window, self scope, and preconditions.</summary>
/// <follows>litespec/core/access#define-each-scope-level-in-the-glossary</follows>
/// <follows>litespec/core/access#name-each-permission-by-a-use-case-verb</follows>
/// <follows>litespec/core/access#limit-the-age-of-a-sign-in</follows>
/// <follows>litespec/core/access#name-the-caller-identity-of-a-self-scope</follows>
/// <follows>litespec/core/access#name-every-authorization-precondition</follows>
internal static class AccessChecks
{
    /// <summary>The prefix every permission identifier carries before its name.</summary>
    private const string PermissionPrefix = "permission/";

    /// <summary>Runs every access check.</summary>
    /// <param name="model">The loaded project model that holds every record and the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckScopeTerms(model, findings);
        CheckPermissionVerbs(model, findings);
        CheckUseCaseAccess(model, findings);
    }

    /// <summary>The grant forms a use case may state: signed-in, anonymous, system, a permission, or self over a permission.</summary>
    private static readonly System.Text.RegularExpressions.Regex GrantForm = new(
        @"^(signed-in|anonymous|system|self\(permission/[a-z0-9-]+\)|permission/[a-z0-9-]+)$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>The entry point auth kind a device caller states, which only a device entry point may carry.</summary>
    private const string DeviceAuth = "device";

    /// <summary>
    ///     Reports a grant outside its forms or naming no permission record, a system grant reached through an outside entry point,
    ///     a sign-in window naming no duration setting, a self scope without its verified caller input, a precondition naming
    ///     no cited precondition rule, and a device entry point whose kind is not device.
    /// </summary>
    /// <param name="model">The loaded project model that holds every use case, permission, setting and entry point.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseAccess(ProjectModel model, List<Finding> findings)
    {
        var applied = GraphChecks.AppliedByUseCase(model);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && !record.Derived))
        {
            if (useCase.Raw.ValueKind != JsonValueKind.Object || !useCase.Raw.TryGetProperty(RecordFields.Access, out var access) || access.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var reached = useCase.Ids(RecordFields.Via);

            foreach (var grant in LinkStrings(access, RecordFields.Grant))
            {
                if (!GrantForm.IsMatch(grant))
                {
                    findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' holds grant '{grant}', which is no signed-in, anonymous, system, permission or self grant.", useCase.File, useCase.Line, SubjectOf(useCase)));
                    continue;
                }

                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(grant, @"permission/[a-z0-9-]+"))
                {
                    if (!model.ById.ContainsKey(match.Value))
                    {
                        findings.Add(new Finding("reference-unknown", $"Use case '{useCase.Id}' grants '{match.Value}', which no record holds.", useCase.File, useCase.Line, SubjectOf(useCase)));
                    }
                }

                if (grant == "system" && reached.Any(id => !IsModuleId(id)))
                {
                    findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' grants system, which authorizes no outside caller, yet it is reached through an entry point.", useCase.File, useCase.Line, SubjectOf(useCase)));
                }
            }

            CheckRecentSignIn(model, useCase, access, findings);
            CheckTenantReason(useCase, access, findings);
            CheckSelfScope(model, useCase, access, findings);
            CheckPreconditions(model, useCase, access, applied, findings);

            foreach (var id in reached)
            {
                if (model.ById.TryGetValue(id, out var point)
                    && point.Type == RecordTypes.EntryPoint
                    && point.Field(RecordFields.Authentication) == DeviceAuth
                    && point.Field(RecordFields.Kind) != "device")
                {
                    findings.Add(new Finding("record-invalid", $"Entry point '{point.Id}' states auth device but its kind is '{point.Field(RecordFields.Kind)}', not device.", point.File, point.Line, SubjectOf(point)));
                }
            }
        }
    }

    /// <summary>
    ///     Reports a sign-in window that names no duration setting: a record of another type,
    ///     or a setting of another value type. A dangling setting identifier belongs to the
    ///     citation check, and a duration window reads from the schema, which holds its shape.
    /// </summary>
    /// <param name="model">The loaded project model that holds every setting record.</param>
    /// <param name="useCase">The use case record whose access window is judged.</param>
    /// <param name="access">The access object of the use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRecentSignIn(ProjectModel model, Record useCase, JsonElement access, List<Finding> findings)
    {
        if (Str(access, RecordFields.RecentSignInWindow) is not { } window
            || !window.StartsWith(RecordTypes.Setting + "/", StringComparison.Ordinal))
        {
            return;
        }

        if (!model.ById.TryGetValue(window, out var setting))
        {
            return;
        }

        if (setting.Type != RecordTypes.Setting)
        {
            findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names recentSignInWindow '{window}', which is no setting.", useCase.File, useCase.Line, SubjectOf(useCase)));
            return;
        }

        if (SettingValueType(setting) is { } valueType && valueType != "duration")
        {
            findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names recentSignInWindow '{window}', which is no duration setting.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>
    ///     Reads the value type a setting declares in its value object.
    /// </summary>
    /// <param name="setting">The setting record whose value type is read.</param>
    /// <returns>The value type name, or null when the setting carries none.</returns>
    private static string? SettingValueType(Record setting)
    {
        if (setting.Raw.ValueKind != JsonValueKind.Object
            || !setting.Raw.TryGetProperty(RecordFields.Value, out var value)
            || value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return Str(value, RecordFields.Type);
    }

    /// <summary>
    ///     Reports a use case with tenant none or cross that states no tenant reason.
    /// </summary>
    /// <param name="useCase">The use case record whose tenant reach is judged.</param>
    /// <param name="access">The access object of the use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTenantReason(Record useCase, JsonElement access, List<Finding> findings)
    {
        if (Str(access, RecordFields.Tenant) is not { } tenant
            || (tenant != "none" && tenant != "cross"))
        {
            return;
        }

        if (Str(access, RecordFields.TenantReason) is not { Length: > 0 })
        {
            findings.Add(new Finding("tenant-reason-missing", $"Use case '{useCase.Id}' runs with tenant '{tenant}' but states no tenant reason.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>
    ///     Reports a self scope whose identity input names no input, carries another origin than
    ///     verified-claim, or identifies no actor. The schema pairs scope with its identity input.
    /// </summary>
    /// <param name="model">The loaded project model that holds every actor record.</param>
    /// <param name="useCase">The use case record whose self scope is judged.</param>
    /// <param name="access">The access object of the use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSelfScope(ProjectModel model, Record useCase, JsonElement access, List<Finding> findings)
    {
        if (Str(access, RecordFields.Scope) is null || Str(access, RecordFields.IdentityInput) is not { } name)
        {
            return;
        }

        var input = useCase.Objects(RecordFields.Inputs)
            .FirstOrDefault(entry => Str(entry, RecordFields.Name) == name);

        if (input.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names identity input '{name}', which is no input of the use case.", useCase.File, useCase.Line, SubjectOf(useCase)));
            return;
        }

        if (Str(input, RecordFields.Origin) is { } origin && origin != "verified-claim")
        {
            findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names identity input '{name}', which takes origin '{origin}', not verified-claim.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }

        if (Str(input, RecordFields.Identifies) is not { } identifies)
        {
            findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names identity input '{name}', which identifies no actor.", useCase.File, useCase.Line, SubjectOf(useCase)));
            return;
        }

        // A dangling identifies is reference-unknown, so only a resolving record that is no actor is reported here.
        if (ContractChecks.IdentifiesResolves(model, identifies)
            && (!model.ById.TryGetValue(Identifiers.BaseId(identifies), out var identified) || identified.Type != RecordTypes.Actor))
        {
            findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names identity input '{name}', which identifies '{identifies}', not an actor.", useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>
    ///     Reports a precondition naming no rule of kind precondition, or a rule the use case
    ///     cites nowhere in its steps, inputs, or results. A dangling rule identifier belongs
    ///     to the citation check.
    /// </summary>
    /// <param name="model">The loaded project model that holds every rule record.</param>
    /// <param name="useCase">The use case record whose preconditions are judged.</param>
    /// <param name="access">The access object of the use case.</param>
    /// <param name="applied">The rule identifiers each use case applies, by use case identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPreconditions(ProjectModel model, Record useCase, JsonElement access, Dictionary<string, HashSet<string>> applied, List<Finding> findings)
    {
        var cited = applied.TryGetValue(useCase.Id, out var rules) ? rules : new HashSet<string>(StringComparer.Ordinal);

        foreach (var precondition in LinkStrings(access, RecordFields.Preconditions))
        {
            if (!model.ById.TryGetValue(precondition, out var rule))
            {
                continue;
            }

            if (rule.Type != RecordTypes.Rule)
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names precondition '{precondition}', which is no rule.", useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            if (rule.Field(RecordFields.Kind) is { } kind && kind != "precondition")
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names precondition '{precondition}', which is a rule of kind '{kind}', not a precondition.", useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            if (!cited.Contains(precondition))
            {
                findings.Add(new Finding("record-invalid", $"Use case '{useCase.Id}' names precondition '{precondition}', which the use case cites nowhere in its steps, inputs, or results.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Whether the identifier names a module.</summary>
    /// <param name="id">The identifier under test.</param>
    /// <returns>True when the identifier opens with the module prefix.</returns>
    private static bool IsModuleId(string id) =>
        Identifiers.TrySplit(id, out var prefix, out _, out _) && prefix == RecordTypes.Module;

    /// <summary>Reports each role whose scope has no glossary term titled with the level and Scope.</summary>
    /// <param name="model">The loaded project model that holds every record and the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScopeTerms(ProjectModel model, List<Finding> findings)
    {
        var titles = model.Records
            .Where(record => record.Type == RecordTypes.Term)
            .Select(record => record.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var role in model.Records.Where(record => record.Type == RecordTypes.Role && !record.Derived))
        {
            var scope = role.Field(RecordFields.Scope);

            if (string.IsNullOrEmpty(scope))
            {
                continue;
            }

            var title = ScopeTitle(scope);

            if (!titles.Contains(title))
            {
                findings.Add(new Finding("scope-term-missing", $"Role '{role.Id}' names scope '{scope}', and no glossary term is titled '{title}'.", role.File, role.Line, SubjectOf(role)));
            }
        }
    }

    /// <summary>Reports each permission whose name opens with no use-case verb and not with manage.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPermissionVerbs(ProjectModel model, List<Finding> findings)
    {
        var verbs = UseCaseVerbChecks.GlossaryVerbs(model);

        foreach (var permission in model.Records.Where(record => record.Type == RecordTypes.Permission && !record.Derived))
        {
            var name = NameOf(permission.Id);

            if (name is null)
            {
                continue;
            }

            var first = name.Split('-')[0];

            if (first.Length == 0 || first == UseCaseVerbs.Manage)
            {
                continue;
            }

            var verb = UseCaseVerbChecks.DeclaredVerbOf(name, verbs);

            if (verb is null)
            {
                findings.Add(new Finding("permission-verb-unknown", $"Permission '{permission.Id}' opens with '{first}', which is no verb of the use-case verb table.", permission.File, permission.Line, SubjectOf(permission)));
                continue;
            }

            // A baseline verb of any kind opens a permission; a glossary verb opens one only when it changes.
            if (UseCaseVerbs.Kinds.ContainsKey(verb))
            {
                continue;
            }

            if (verbs.TryGetValue(verb, out var kind) && kind != "query")
            {
                continue;
            }

            findings.Add(new Finding("permission-verb-unknown", $"Permission '{permission.Id}' opens with '{verb}', which is a glossary verb of kind query.", permission.File, permission.Line, SubjectOf(permission)));
        }
    }

    /// <summary>The glossary title a scope level takes: each kebab word capitalized, then Scope, as in Platform Scope.</summary>
    /// <param name="scope">The kebab-case scope level a role names.</param>
    /// <returns>The title the glossary term must carry.</returns>
    private static string ScopeTitle(string scope) =>
        string.Join(' ', scope.Split('-').Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]))
        + " Scope";

    /// <summary>The name a permission identifier carries after the prefix.</summary>
    /// <param name="id">The permission identifier.</param>
    /// <returns>The name after the prefix, or null when the identifier carries none.</returns>
    private static string? NameOf(string id)
    {
        if (!id.StartsWith(PermissionPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var name = id[PermissionPrefix.Length..];

        return name.Length == 0 ? null : name;
    }
}
