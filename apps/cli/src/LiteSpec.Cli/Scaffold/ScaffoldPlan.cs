using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Scaffold;

/// <summary>
///     The files <c>litespec scaffold</c> writes for one use case: the plan the command
///     renders in memory from the built model before it touches the disk.
/// </summary>
/// <follows>litespec/core/backend-application#organize-application-by-use-case</follows>
static class ScaffoldPlan
{
    /// <summary>One record input, as the message and the validator read it.</summary>
    /// <param name="Name">The input name as written.</param>
    /// <param name="Meaning">The input meaning the record states.</param>
    /// <param name="Type">The record field type.</param>
    /// <param name="Required">Whether the record marks the input required.</param>
    /// <param name="Origin">The input origin: caller, claim, derived or verified-claim.</param>
    /// <param name="RowFields">The row fields of a list input carrying rows, or null.</param>
    /// <param name="ScalarItem">The item type of a list input carrying scalars, or null.</param>
    public sealed record ScaffoldInput(
        string Name,
        string Meaning,
        string Type,
        bool Required,
        string Origin,
        IReadOnlyList<FieldShape>? RowFields,
        string? ScalarItem);

    /// <summary>One output value, as the result reads it.</summary>
    /// <param name="Name">The value name as written.</param>
    /// <param name="Meaning">The value meaning the record states.</param>
    /// <param name="Type">The record field type.</param>
    /// <param name="Required">Whether the value is always present.</param>
    /// <param name="RowFields">The row fields of a list value carrying rows, or null.</param>
    /// <param name="ScalarItem">The item type of a list value carrying scalars, or null.</param>
    public sealed record FieldShape(
        string Name,
        string Meaning,
        string Type,
        bool Required,
        IReadOnlyList<FieldShape>? RowFields,
        string? ScalarItem);

    /// <summary>One query output group, such as the rows group of a list query.</summary>
    /// <param name="Name">The group name as written.</param>
    /// <param name="Fields">The group fields in record order.</param>
    public sealed record OutputGroup(string Name, IReadOnlyList<FieldShape> Fields);

    /// <summary>One file the plan renders: its consumer-relative path and its expected text.</summary>
    /// <param name="RelativePath">The path relative to the consumer root.</param>
    /// <param name="Content">The expected file text with LF line endings.</param>
    public sealed record ScaffoldFile(string RelativePath, string Content);

    /// <summary>The resolved use case scaffold: every name, record value and file the renderers need.</summary>
    /// <param name="UseCaseId">The use case identifier.</param>
    /// <param name="Title">The use case title.</param>
    /// <param name="Kind">The use case kind: command or query.</param>
    /// <param name="Module">The record module name.</param>
    /// <param name="UseCaseName">The name part of the use case id.</param>
    /// <param name="Stem">The use case stem in PascalCase.</param>
    /// <param name="Act">The aggregate act the handler calls.</param>
    /// <param name="AggregateName">The aggregate name in kebab case, or null for a module-level read.</param>
    /// <param name="ModuleProject">The absolute module project folder the files go under.</param>
    /// <param name="NamespaceBase">The namespace root from the module project folder name.</param>
    /// <param name="Grant">The access grant, or null when the record states none.</param>
    /// <param name="Tenant">The access tenant reach, or null when the record states none.</param>
    /// <param name="TenantReason">The tenant reason, or null when the record states none.</param>
    /// <param name="RecentSignInWindow">The recent sign-in window, or null when the record states none.</param>
    /// <param name="AuditExempt">Whether the record exempts audit.</param>
    /// <param name="AuditReason">The audit exemption reason, or null.</param>
    /// <param name="AuditTargetInputs">The audit target inputs, or null when the target comes from outputs.</param>
    /// <param name="AuditAction">Whether the audit names an action the definition must carry.</param>
    /// <param name="AuditCategory">The audit category, or null.</param>
    /// <param name="IdempotencyMode">The idempotency mode, or null for a query.</param>
    /// <param name="IdempotencyKey">The idempotency key template of mode key, or null.</param>
    /// <param name="IdempotencyScope">The idempotency scope of mode key, or null.</param>
    /// <param name="IdempotencyReplay">The repeat behavior of mode key, or null.</param>
    /// <param name="IdempotencyReason">The exemption reason of mode none, or the state of mode state, or null.</param>
    /// <param name="Inputs">The record inputs in record order.</param>
    /// <param name="CommandOutputs">The command output fields, or null for a query.</param>
    /// <param name="QueryGroups">The query output groups, or null for a command.</param>
    /// <param name="HttpMethod">The endpoint HTTP method.</param>
    /// <param name="HttpRoute">The endpoint route.</param>
    /// <param name="WriteEndpoint">Whether a web entry point reaches the use case.</param>
    /// <param name="FirstStatement">The first step statement, or null when the record states none.</param>
    /// <param name="SuccessMeaning">The success result meaning, or null when no success result states one.</param>
    /// <param name="Files">The rendered files in write order.</param>
    /// <param name="ExpectsLine">The console line naming the aggregate act scaffold expects, or null for a query.</param>
    public sealed record ScaffoldedUseCase(
        string UseCaseId,
        string Title,
        string Kind,
        string Module,
        string UseCaseName,
        string Stem,
        string Act,
        string? AggregateName,
        string ModuleProject,
        string NamespaceBase,
        string? Grant,
        string? Tenant,
        string? TenantReason,
        string? RecentSignInWindow,
        bool AuditExempt,
        string? AuditReason,
        IReadOnlyList<string>? AuditTargetInputs,
        bool AuditAction,
        string? AuditCategory,
        string? IdempotencyMode,
        string? IdempotencyKey,
        string? IdempotencyScope,
        string? IdempotencyReplay,
        string? IdempotencyReason,
        IReadOnlyList<ScaffoldInput> Inputs,
        IReadOnlyList<FieldShape>? CommandOutputs,
        IReadOnlyList<OutputGroup>? QueryGroups,
        string HttpMethod,
        string HttpRoute,
        bool WriteEndpoint,
        string? FirstStatement,
        string? SuccessMeaning,
        IReadOnlyList<ScaffoldFile> Files,
        string? ExpectsLine);

    /// <summary>Builds the scaffold plan for one use case id, without touching the disk.</summary>
    /// <param name="model">The built project model the files render from.</param>
    /// <param name="rawId">The use case id, with or without the use-case prefix.</param>
    /// <param name="plan">The resolved plan, or null when the id cannot scaffold.</param>
    /// <param name="error">Why the id cannot scaffold, or null when it can.</param>
    /// <returns>True when the plan built; otherwise false.</returns>
    public static bool TryBuild(ProjectModel model, string rawId, out ScaffoldedUseCase? plan, out string? error)
    {
        plan = null;
        error = null;

        var id = rawId.Contains('/', StringComparison.Ordinal) ? rawId : $"use-case/{rawId}";

        if (!model.ById.TryGetValue(id, out var record) || record.Derived)
        {
            error = $"Use case '{id}' not found.";
            return false;
        }

        if (record.Type != RecordTypes.UseCase)
        {
            error = $"Record '{id}' is a {record.Type}, not a use case.";
            return false;
        }

        if (!Identifiers.TrySplit(id, out _, out var module, out var useCaseName)
            || module is null
            || useCaseName.Length == 0)
        {
            error = $"Use case id '{id}' names no module and use case.";
            return false;
        }

        var kind = record.Field(RecordFields.Kind);

        if (kind is not ("command" or "query"))
        {
            error = $"Use case '{id}' states kind '{kind ?? "none"}', not command or query.";
            return false;
        }

        var stem = ScaffoldNaming.Stem(useCaseName);
        var act = ScaffoldNaming.ActName(useCaseName);
        var aggregate = UseCaseQueries.AggregateOf(model, record);
        string? aggregateName = null;

        if (aggregate is not null
            && Identifiers.TrySplit(aggregate.Id, out _, out _, out var holder)
            && holder.Length > 0)
        {
            aggregateName = holder;
        }

        if (kind == "command" && aggregateName is null)
        {
            error = $"Use case '{id}' is a command outside an aggregate; scaffold writes commands under their aggregate.";
            return false;
        }

        var project = ModuleProjectOf(model, module);

        if (project is null)
        {
            error = $"No module project for module '{module}': declare it in stackConfig.dotnet.modules.";
            return false;
        }

        var access = ObjectOf(record.Raw, RecordFields.Access);
        var audit = ObjectOf(record.Raw, RecordFields.Audit);
        var idempotency = kind == "command" ? ObjectOf(record.Raw, RecordFields.Idempotency) : null;
        var http = ObjectOf(record.Raw, "http");
        var inputs = ReadInputs(record);
        var groups = kind == "query" ? ReadQueryGroups(record) : null;
        var outputs = kind == "command" ? ReadCommandOutputs(record) : null;
        var web = ReachesWeb(model, record);
        var method = TextOf(http, "method") ?? (kind == "command" ? "POST" : "GET");
        var route = TextOf(http, "route") ?? $"/api/{module}/{useCaseName}";
        var files = new List<ScaffoldFile>();
        var resolved = new ScaffoldedUseCase(
            id,
            record.Title,
            kind,
            module,
            useCaseName,
            stem,
            act,
            aggregateName,
            project,
            Path.GetFileName(project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            TextOf(access, RecordFields.Grant),
            TextOf(access, RecordFields.Tenant),
            TextOf(access, RecordFields.TenantReason),
            TextOf(access, RecordFields.RecentSignInWindow),
            AuditExemptOf(audit),
            TextOf(audit, RecordFields.ExemptBecause),
            AuditTargetsOf(audit),
            NeedsAuditAction(audit),
            TextOf(audit, "category"),
            TextOf(idempotency, RecordFields.Mode),
            TextOf(idempotency, "key"),
            TextOf(idempotency, "scope"),
            TextOf(idempotency, "replay"),
            TextOf(idempotency, "reason") ?? TextOf(idempotency, "recognizedBy"),
            inputs,
            outputs,
            groups,
            method,
            route,
            web,
            FirstStatementOf(record),
            SuccessMeaningOf(model, record),
            files,
            kind == "command" ? $"Expects aggregate act {AggregateTypeOf(aggregateName)}.{act} for {id}." : null);

        files.AddRange(ScaffoldRender.Render(resolved, model.Config.Root));
        plan = resolved;

        return true;
    }

    /// <summary>The module project folder the use case files go under.</summary>
    /// <param name="model">The built project model holding the consumer configuration.</param>
    /// <param name="module">The record module name.</param>
    /// <returns>The project folder, or null when no configured root holds the module.</returns>
    private static string? ModuleProjectOf(ProjectModel model, string module)
    {
        var wanted = Naming.Pascal(module);
        string? fallback = null;

        foreach (var root in model.Config.ModulesRoots)
        {
            var folder = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (string.Equals(folder, wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(folder.Split('.').Last(), wanted, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            fallback ??= root;
        }

        return model.Config.ModulesRoots.Count == 1 ? fallback : null;
    }

    /// <summary>The aggregate type name, or the module stem when the use case is a module-level read.</summary>
    /// <param name="aggregateName">The aggregate name in kebab case, or null.</param>
    /// <returns>The aggregate type name.</returns>
    private static string AggregateTypeOf(string? aggregateName) =>
        aggregateName is null ? "" : Naming.Pascal(aggregateName);

    /// <summary>Whether a web entry point reaches the use case through its via links.</summary>
    /// <param name="model">The built project model holding the entry point records.</param>
    /// <param name="record">The use case record.</param>
    /// <returns>True when a via entry point serves HTTP.</returns>
    private static bool ReachesWeb(ProjectModel model, Record record)
    {
        foreach (var target in record.Ids(RecordFields.Via))
        {
            if (model.ById.TryGetValue(target, out var point)
                && point.Type == RecordTypes.EntryPoint
                && ScaffoldNaming.WebKinds.Contains(point.Field(RecordFields.Kind) ?? ""))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The record inputs in record order.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The inputs with their row and scalar item shapes.</returns>
    private static List<ScaffoldInput> ReadInputs(Record record)
    {
        var inputs = new List<ScaffoldInput>();

        foreach (var entry in record.Objects(RecordFields.Inputs))
        {
            var type = TextOf(entry, "type") ?? "text";
            var (rows, scalar) = ItemShapes(entry);

            inputs.Add(new ScaffoldInput(
                TextOf(entry, "name") ?? "value",
                TextOf(entry, "meaning") ?? "",
                type,
                FlagOf(entry, "required"),
                TextOf(entry, "origin") ?? "caller",
                rows,
                scalar));
        }

        return inputs;
    }

    /// <summary>The command output fields, in record order.</summary>
    /// <param name="record">The command record.</param>
    /// <returns>The output fields; every output field is always present.</returns>
    private static List<FieldShape> ReadCommandOutputs(Record record)
    {
        var outputs = new List<FieldShape>();

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Outputs, out var value)
            && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in value.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object)
                {
                    outputs.Add(ReadField(entry, required: true));
                }
            }
        }

        return outputs;
    }

    /// <summary>The query output groups, in record order.</summary>
    /// <param name="record">The query record.</param>
    /// <returns>The include groups with their fields.</returns>
    private static List<OutputGroup> ReadQueryGroups(Record record)
    {
        var groups = new List<OutputGroup>();

        if (record.Raw.ValueKind == JsonValueKind.Object
            && record.Raw.TryGetProperty(RecordFields.Outputs, out var outputs)
            && outputs.ValueKind == JsonValueKind.Object
            && outputs.TryGetProperty("include", out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                var fields = new List<FieldShape>();

                if (group.Value.ValueKind == JsonValueKind.Object
                    && group.Value.TryGetProperty("fields", out var listed)
                    && listed.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in listed.EnumerateArray())
                    {
                        if (entry.ValueKind == JsonValueKind.Object)
                        {
                            fields.Add(ReadField(entry, required: true));
                        }
                    }
                }

                groups.Add(new OutputGroup(group.Name, fields));
            }
        }

        return groups;
    }

    /// <summary>One output or row field with its item shapes.</summary>
    /// <param name="entry">The field object.</param>
    /// <param name="required">Whether the field is always present.</param>
    /// <returns>The field shape.</returns>
    private static FieldShape ReadField(JsonElement entry, bool required)
    {
        if (entry.TryGetProperty("required", out var flag)
            && flag.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            required = flag.ValueKind == JsonValueKind.True;
        }

        var type = TextOf(entry, "type") ?? "text";
        var (rows, scalar) = ItemShapes(entry);

        return new FieldShape(
            TextOf(entry, "name") ?? "value",
            TextOf(entry, "meaning") ?? "",
            type,
            required,
            rows,
            scalar);
    }

    /// <summary>The item shapes of a list field: its row fields, or its scalar item type.</summary>
    /// <param name="entry">The list field object.</param>
    /// <returns>The row fields with the scalar item type; both null when the field is no list.</returns>
    private static (IReadOnlyList<FieldShape>? Rows, string? Scalar) ItemShapes(JsonElement entry)
    {
        if (TextOf(entry, "type") != "list"
            || !entry.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        if (items.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
        {
            var rows = new List<FieldShape>();

            foreach (var field in fields.EnumerateArray())
            {
                if (field.ValueKind == JsonValueKind.Object)
                {
                    rows.Add(ReadField(field, required: true));
                }
            }

            return (rows, null);
        }

        return (null, TextOf(items, "type") ?? "string");
    }

    /// <summary>Whether the audit record exempts the use case: an exemptBecause, or level none.</summary>
    /// <param name="audit">The audit object, or null when the record states none.</param>
    /// <returns>True when the record exempts audit.</returns>
    private static bool AuditExemptOf(JsonElement? audit)
    {
        if (audit is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        if (element.TryGetProperty(RecordFields.ExemptBecause, out _))
        {
            return true;
        }

        return element.TryGetProperty(RecordFields.Level, out var level)
            && level.ValueKind == JsonValueKind.String
            && level.GetString() == "none";
    }

    /// <summary>The audit target inputs, or null when the target comes from outputs or is absent.</summary>
    /// <param name="audit">The audit object, or null when the record states none.</param>
    /// <returns>The target input names, or null.</returns>
    private static List<string>? AuditTargetsOf(JsonElement? audit)
    {
        if (audit is not JsonElement element
            || element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("target", out var target))
        {
            return null;
        }

        if (target.ValueKind == JsonValueKind.String && target.GetString() is { Length: > 0 } single)
        {
            return [single];
        }

        if (target.ValueKind == JsonValueKind.Array)
        {
            var inputs = target.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.String && entry.GetString() is { Length: > 0 })
                .Select(entry => entry.GetString()!)
                .ToList();

            return inputs.Count > 0 ? inputs : null;
        }

        return null;
    }

    /// <summary>Whether the audit names an action the definition must carry: every non-exempt audit.</summary>
    /// <param name="audit">The audit object, or null when the record states none.</param>
    /// <returns>True when the definition must name the audited action.</returns>
    private static bool NeedsAuditAction(JsonElement? audit) => !AuditExemptOf(audit);

    /// <summary>The first step statement, or null when the record states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The first statement text, or null.</returns>
    private static string? FirstStatementOf(Record record)
    {
        foreach (var step in record.Objects("steps"))
        {
            if (TextOf(step, "statement") is { Length: > 0 } statement)
            {
                return statement;
            }
        }

        return null;
    }

    /// <summary>The success result meaning, or null when no success result states one.</summary>
    /// <param name="model">The built project model holding the result records.</param>
    /// <param name="record">The use case record.</param>
    /// <returns>The first success meaning, or null.</returns>
    private static string? SuccessMeaningOf(ProjectModel model, Record record)
    {
        foreach (var entry in record.Objects(RecordFields.Results))
        {
            if (TextOf(entry, "outcome") == "success" && TextOf(entry, "meaning") is { Length: > 0 } meaning)
            {
                return meaning;
            }

            if (TextOf(entry, "id") is { Length: > 0 } id
                && model.ById.TryGetValue(id, out var result)
                && TextOf(result.Raw, "outcome") == "success"
                && TextOf(result.Raw, "meaning") is { Length: > 0 } held)
            {
                return held;
            }
        }

        return null;
    }

    /// <summary>Reads one string member of a JSON object, or null when it is missing or not a string.</summary>
    /// <param name="entry">The object to read, or null.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The string value, or null.</returns>
    private static string? TextOf(JsonElement? entry, string name)
    {
        if (entry is not JsonElement element
            || element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    /// <summary>Reads one object member of a JSON object, or null when it is missing or not an object.</summary>
    /// <param name="entry">The object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The object value, or null.</returns>
    private static JsonElement? ObjectOf(JsonElement entry, string name) =>
        entry.ValueKind == JsonValueKind.Object
        && entry.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    /// <summary>Reads one boolean member of a JSON object.</summary>
    /// <param name="entry">The object to read.</param>
    /// <param name="name">The member name.</param>
    /// <returns>True when the member is the boolean true; otherwise false.</returns>
    private static bool FlagOf(JsonElement entry, string name) =>
        entry.ValueKind == JsonValueKind.Object
        && entry.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.True;

}
