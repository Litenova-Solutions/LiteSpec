using System.Text.Json;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The message field checks: every command or query type carries the fields its use case input names or implies, every result type carries the fields its output names or implies, and every shared row shape matches the backend type its rowType names.</summary>
/// <follows>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</follows>
/// <follows>litespec/core/use-cases#type-every-output-field</follows>
/// <follows>litespec/core/language#resolve-every-identifier-citation</follows>
internal static class MessageFieldChecks
{
    /// <summary>Reads message, result and helper types from the backend source: one parse per file, one file scan per run.</summary>
    internal sealed class MessageSource
    {
        /// <summary>The loaded project model that names the backend roots.</summary>
        readonly ProjectModel model;

        /// <summary>The compilation the backend read was built from, or null when the syntax-only path ran.</summary>
        readonly BackendSemantics? semantics;

        /// <summary>The backend roots the type files are searched under.</summary>
        readonly IReadOnlyList<string> roots;

        /// <summary>The parsed trees by file path.</summary>
        readonly Dictionary<string, SyntaxTree> trees = new(StringComparer.Ordinal);

        /// <summary>The files that failed to read or parse.</summary>
        readonly HashSet<string> unreadable = new(StringComparer.Ordinal);

        /// <summary>The declaring file by type name, or null when missing or ambiguous; filled by the first search.</summary>
        readonly Dictionary<string, string?> files = new(StringComparer.Ordinal);

        /// <summary>The type names a hand-written backend file carries, without the files <c>litespec generate</c> wrote; filled by the first search.</summary>
        readonly HashSet<string> handWritten = new(StringComparer.Ordinal);

        /// <summary>The type names a generated backend file carries; filled by the first search.</summary>
        readonly HashSet<string> generatedNames = new(StringComparer.Ordinal);

        /// <summary>The type names with two generated files in different folders, which stay ambiguous.</summary>
        readonly HashSet<string> ambiguousGenerated = new(StringComparer.Ordinal);

        /// <summary>Whether the backend file names were indexed.</summary>
        bool indexed;

        /// <summary>Initializes a new instance of the <see cref="MessageSource" /> class.</summary>
        /// <param name="model">The loaded project model that names the backend roots.</param>
        internal MessageSource(ProjectModel model)
        {
            this.model = model;
            semantics = model.BackendSemantics;
            roots = SourceRoots.BackendRoots(model.Config);
        }

        /// <summary>The semantic model of one tree, or null when the syntax-only path ran or the tree is not part of the read.</summary>
        /// <param name="tree">The parsed syntax tree.</param>
        /// <returns>The semantic model of the tree, or null.</returns>
        internal SemanticModel? SemanticOf(SyntaxTree tree) => semantics?.ModelFor(tree);

        /// <summary>The file declaring the type: the generated file beside the hint first, then a generated file anywhere, then the hand-written file beside the hint, then the backend roots by file name.</summary>
        /// <param name="typeName">The simple type name.</param>
        /// <param name="hintFile">A file in the folder the type usually sits in.</param>
        /// <returns>The declaring file, or null when it is missing or two files declare the name.</returns>
        internal string? FileOf(string typeName, string hintFile)
        {
            if (typeName.Length == 0)
            {
                return null;
            }

            string? besideHand = null;

            if (Path.GetDirectoryName(hintFile) is { } folder)
            {
                // The generated part carries the declared fields; a hand-written
                // sibling beside it carries only hand members, which stay unchecked.
                var generated = Path.Combine(folder, typeName + ".generated.cs");

                if (File.Exists(generated))
                {
                    return generated;
                }

                var beside = Path.Combine(folder, typeName + ".cs");

                if (File.Exists(beside))
                {
                    besideHand = beside;
                }
            }

            if (!indexed)
            {
                Index();
            }

            // A generated file wins over a hand-written one wherever each sits: during
            // adoption the hand file is the stale copy to be deleted, so resolving from
            // it would drop the catalog entry on members it no longer carries.
            if (files.TryGetValue(typeName, out var found) && found is not null)
            {
                if (found.EndsWith(".generated.cs", StringComparison.Ordinal))
                {
                    return found;
                }

                if (besideHand is not null)
                {
                    return besideHand;
                }

                return found;
            }

            return besideHand;
        }

        /// <summary>Whether the backend declares the type in a hand-written file and in no generated one, so a message or result row reuses it instead of writing a local row. A hand-written sibling beside a generated row keeps the row local, as does a stale generated row the record no longer renders, until its file is deleted.</summary>
        /// <param name="typeName">The simple type name.</param>
        /// <returns>True when a hand-written file carries the name and no generated file does; otherwise false.</returns>
        internal bool HasHandWritten(string typeName)
        {
            if (typeName.Length == 0)
            {
                return false;
            }

            if (!indexed)
            {
                Index();
            }

            return handWritten.Contains(typeName) && !generatedNames.Contains(typeName);
        }

        /// <summary>Every declaration of the type in the file, for partial types in every part.</summary>
        /// <param name="file">The file to read.</param>
        /// <param name="typeName">The simple type name.</param>
        /// <returns>Each declaration with its tree, or an empty list when the file does not read or declares none.</returns>
        internal IReadOnlyList<(SyntaxTree Tree, TypeDeclarationSyntax Declared)> TypesIn(string file, string typeName)
        {
            if (semantics is not null && semantics.TryTypesIn(file, typeName, out var shared))
            {
                return shared;
            }

            if (unreadable.Contains(file))
            {
                return [];
            }

            if (!trees.TryGetValue(file, out var tree))
            {
                string text;

                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    unreadable.Add(file);
                    return [];
                }

                tree = CSharpSyntaxTree.ParseText(text, path: file);
                trees[file] = tree;
            }

            return
            [
                .. tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Where(declared => declared.Identifier.ValueText == typeName)
                    .Select(declared => (tree, declared)),
            ];
        }

        /// <summary>The result type the handler returns: the second type argument of its command or query handler interface, or its Handle return type.</summary>
        /// <param name="handler">The handler whose result type is wanted.</param>
        /// <returns>The simple result type name, or null when the handler file does not read or the handler returns none.</returns>
        internal string? ResultTypeOf(CodeHandler handler)
        {
            if (handler.Class.Length == 0)
            {
                return null;
            }

            var parts = TypesIn(handler.File, handler.Class);

            if (parts.Count == 0)
            {
                return null;
            }

            foreach (var (tree, declared) in parts)
            {
                if (SemanticOf(tree) is { } model
                    && model.GetDeclaredSymbol(declared) is INamedTypeSymbol symbol
                    && HandlerInterfaces.Of(symbol) is { Result: { Length: > 0 } result })
                {
                    return result;
                }
            }

            foreach (var (_, declared) in parts)
            {
                if (declared.BaseList is not null)
                {
                    foreach (var entry in declared.BaseList.Types)
                    {
                        if (HandlerResultOf(entry.Type) is { } fromFace)
                        {
                            return fromFace;
                        }
                    }
                }
            }

            foreach (var (_, declared) in parts)
            {
                foreach (var method in declared.Members.OfType<MethodDeclarationSyntax>())
                {
                    if (method.Identifier.ValueText is "Handle" or "HandleAsync"
                        && HandleResultOf(method.ReturnType) is { } fromReturn)
                    {
                        return fromReturn;
                    }
                }
            }

            return null;
        }

        /// <summary>Indexes the backend file names by type: the file for a unique name, null for an ambiguous one. A generated file indexes under its type name without the generated suffix, winning over hand-written files wherever they sit; two generated files in different folders stay ambiguous.</summary>
        void Index()
        {
            indexed = true;

            foreach (var root in roots)
            {
                List<string> found;

                try
                {
                    found = [.. SourceTree.Files(root, ".cs")];
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var file in found.Order(StringComparer.Ordinal))
                {
                    var name = Path.GetFileNameWithoutExtension(file);

                    if (name.Length == 0)
                    {
                        continue;
                    }

                    var key = name.EndsWith(".generated", StringComparison.Ordinal)
                        ? name[..^".generated".Length]
                        : name;

                    if (key.Length == 0)
                    {
                        continue;
                    }

                    var generated = key.Length != name.Length;

                    if (generated)
                    {
                        generatedNames.Add(key);
                    }
                    else
                    {
                        handWritten.Add(key);
                    }

                    if (!files.TryGetValue(key, out var held))
                    {
                        files[key] = file;
                        continue;
                    }

                    if (held is null)
                    {
                        // Hands alone stay ambiguous, but a first generated file still
                        // wins over them: it carries the declared fields.
                        if (generated && !ambiguousGenerated.Contains(key))
                        {
                            files[key] = file;
                        }

                        continue;
                    }

                    if (string.Equals(held, file, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var heldGenerated = held.EndsWith(".generated.cs", StringComparison.Ordinal);

                    if (heldGenerated && !generated)
                    {
                        continue;
                    }

                    if (generated && !heldGenerated)
                    {
                        files[key] = file;
                        continue;
                    }

                    files[key] = null;

                    if (generated)
                    {
                        ambiguousGenerated.Add(key);
                    }
                }
            }
        }

        /// <summary>The result type a handler interface names: the second type argument of ICommandHandler or IQueryHandler.</summary>
        /// <param name="type">The base type as written.</param>
        /// <returns>The simple result type name, or null when the base is no handler interface.</returns>
        static string? HandlerResultOf(TypeSyntax type)
        {
            var name = type switch
            {
                GenericNameSyntax generic => generic,
                QualifiedNameSyntax qualified => qualified.Right as GenericNameSyntax,
                _ => null,
            };

            if (name is null
                || name.Identifier.ValueText is not ("ICommandHandler" or "IQueryHandler")
                || name.TypeArgumentList.Arguments.Count != 2)
            {
                return null;
            }

            return SimpleName(name.TypeArgumentList.Arguments[1]);
        }

        /// <summary>The result type a Handle method returns: the task argument without nullability.</summary>
        /// <param name="type">The return type as written.</param>
        /// <returns>The simple result type name, or null when the method returns none.</returns>
        static string? HandleResultOf(TypeSyntax type)
        {
            var inner = type;

            if (inner is NullableTypeSyntax nullable)
            {
                inner = nullable.ElementType;
            }

            if (inner is GenericNameSyntax task
                && task.Identifier.ValueText is ("Task" or "ValueTask")
                && task.TypeArgumentList.Arguments.Count == 1)
            {
                inner = task.TypeArgumentList.Arguments[0];

                if (inner is NullableTypeSyntax element)
                {
                    inner = element.ElementType;
                }

                if (inner is GenericNameSyntax box
                    && box.Identifier.ValueText == "Nullable"
                    && box.TypeArgumentList.Arguments.Count == 1)
                {
                    inner = box.TypeArgumentList.Arguments[0];
                }
            }
            else if (inner is not GenericNameSyntax)
            {
                return null;
            }

            return SimpleName(inner);
        }

        /// <summary>Reads the simple name of a type as written, without qualification or generic arguments.</summary>
        /// <param name="type">The type as written.</param>
        /// <returns>The simple name, or null when the type has none.</returns>
        static string? SimpleName(TypeSyntax type) => type switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            NullableTypeSyntax nullable => SimpleName(nullable.ElementType),
            ArrayTypeSyntax array => SimpleName(array.ElementType),
            _ => null,
        };
    }

    /// <summary>Reports each message whose properties drift from its use case input, and each result type drifting from its output.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var source = new MessageSource(model);
        var tenant = TenantTerm(model, findings);

        foreach (var handler in model.Code.Handlers)
        {
            if (handler.MessageType.Length == 0
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != RecordTypes.UseCase)
            {
                continue;
            }

            CheckInput(source, tenant, handler, useCase, findings, model);
            CheckOutput(source, handler, useCase, findings);
        }
    }

    /// <summary>The tenant term the tenancy selection names, or empty when the selection names none or no record holds it.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>The tenant term id, or empty.</returns>
    static string TenantTerm(ProjectModel model, List<Finding> findings)
    {
        var tenant = model.Config.TenancyTenantTerm;

        if (tenant.Length == 0)
        {
            return "";
        }

        if (!model.ById.ContainsKey(tenant))
        {
            findings.Add(new Finding(
                "reference-unknown",
                $"The tenancy extension names tenant term '{tenant}', which no record holds.",
                ConsumerRoot.ProjectFile(model.Config.Root),
                1,
                null));

            return "";
        }

        return tenant;
    }

    /// <summary>Reports the message carrying a property its use case input neither names nor implies, lacking one the input names, or lacking the tenant key its tenant scope implies.</summary>
    /// <param name="source">The backend source the message type is read from.</param>
    /// <param name="tenant">The tenant term id the tenancy selection names, or empty.</param>
    /// <param name="handler">The handler whose message is compared.</param>
    /// <param name="useCase">The use case record holding the input.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <param name="model">The loaded project model holding the tenant aggregate.</param>
    static void CheckInput(MessageSource source, string tenant, CodeHandler handler, Record useCase, List<Finding> findings, ProjectModel model)
    {
        var message = handler.MessageType;

        if (source.FileOf(message, handler.File) is not { } file)
        {
            return;
        }

        var parts = source.TypesIn(file, message);

        if (parts.Count == 0)
        {
            return;
        }

        var properties = PropertiesOf(source, parts);
        var inputs = ContractFields(useCase, RecordFields.Input);
        var drift = Compare(inputs, properties, "input", implied: ImpliedMessage(useCase, tenant));

        if (MissingTenantKey(model, useCase, tenant, properties) is { } tenantKey)
        {
            drift.Add(tenantKey);
        }

        if (useCase.Raw.ValueKind == JsonValueKind.Object
            && useCase.Raw.TryGetProperty(RecordFields.Input, out var entries)
            && entries.ValueKind == JsonValueKind.Array)
        {
            AddSharedRowDrift(source, file, entries, "", drift);
        }

        if (drift.Count == 0)
        {
            return;
        }

        findings.Add(new Finding(
            "input-field-drift",
            $"Use case '{useCase.Id}' message '{message}' drifts from its declared input: {string.Join("; ", drift)}.",
            file,
            LineOf(parts[0]),
            useCase.Id));
    }

    /// <summary>Reports the result type carrying a property its use case output neither names nor implies, lacking one the output names, or lacking the next cursor its cursor paging implies.</summary>
    /// <param name="source">The backend source the result type is read from.</param>
    /// <param name="handler">The handler whose result type is compared.</param>
    /// <param name="useCase">The use case record holding the output.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    static void CheckOutput(MessageSource source, CodeHandler handler, Record useCase, List<Finding> findings)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return;
        }

        if (source.ResultTypeOf(handler) is not { } result
            || source.FileOf(result, handler.File) is not { } file)
        {
            return;
        }

        var parts = source.TypesIn(file, result);

        if (parts.Count == 0)
        {
            return;
        }

        var properties = PropertiesOf(source, parts);
        List<string> drift;

        if (output.ValueKind == JsonValueKind.Array)
        {
            drift = Compare(FieldsOf(output), properties, "output field", implied: ImpliedResult(useCase));
        }
        else if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            var groups = include.EnumerateObject()
                .Where(group => group.Value.ValueKind == JsonValueKind.Object)
                .Select(group => (group.Name, Fields: FieldsIn(group.Value), Optional: IsOptionalGroup(group.Value)))
                .ToList();

            if (groups.Count == 0)
            {
                return;
            }

            if (groups.Count == 1 && !groups[0].Optional)
            {
                drift = Compare(groups[0].Fields, properties, "output field", implied: ImpliedResult(useCase));
            }
            else if (!TryNestedDrift(source, handler.File, groups.Select(group => (group.Name, group.Fields)).ToList(), properties, out drift))
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (CursorPaged(useCase) && !properties.ContainsKey("NextCursor"))
        {
            drift.Add("no property for paging cursor 'NextCursor'");
        }

        AddOutputRowDrift(source, file, useCase, drift);

        if (drift.Count == 0)
        {
            return;
        }

        findings.Add(new Finding(
            "output-field-drift",
            $"Use case '{useCase.Id}' result '{result}' drifts from its declared output: {string.Join("; ", drift)}.",
            file,
            LineOf(parts[0]),
            useCase.Id));
    }

    /// <summary>Compares each shared row shape of the use-case output with the backend type its rowType names, in both the field list and the include-group forms.</summary>
    /// <param name="source">The backend source each named row type is read from.</param>
    /// <param name="hintFile">The result file the named row types are searched beside.</param>
    /// <param name="useCase">The use case record holding the output.</param>
    /// <param name="drift">The drift clauses the row comparison appends to.</param>
    static void AddOutputRowDrift(MessageSource source, string hintFile, Record useCase, List<string> drift)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Output, out var output))
        {
            return;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            AddSharedRowDrift(source, hintFile, output, "", drift);
            return;
        }

        if (output.ValueKind != JsonValueKind.Object
            || !output.TryGetProperty(RecordFields.Include, out var include)
            || include.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var group in include.EnumerateObject())
        {
            if (group.Value.ValueKind == JsonValueKind.Object
                && group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                && fields.ValueKind == JsonValueKind.Array)
            {
                AddSharedRowDrift(source, hintFile, fields, group.Name, drift);
            }
        }
    }

    /// <summary>Compares each shared row shape with the backend type its rowType names, descending into nested row shapes, so a stated shape that drifts from its code type is reported beside the message drift. A row type naming an aggregate part, a row the backend does not declare, or a generated row file is left unchecked: parts are domain-owned, external types resolve nowhere, and generated-drift covers generated rows.</summary>
    /// <param name="source">The backend source each named row type is read from.</param>
    /// <param name="hintFile">The message or result file the named row types are searched beside.</param>
    /// <param name="fields">The contract fields whose row shapes are compared.</param>
    /// <param name="prefix">The dotted path of the enclosing field, or empty at the top.</param>
    /// <param name="drift">The drift clauses the row comparison appends to.</param>
    static void AddSharedRowDrift(MessageSource source, string hintFile, JsonElement fields, string prefix, List<string> drift)
    {
        foreach (var entry in fields.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || Str(entry, RecordFields.Name) is not { Length: > 0 } name
                || Str(entry, RecordFields.Type) is not ("list" or "object")
                || !entry.TryGetProperty(RecordFields.RowShape, out var shape)
                || shape.ValueKind != JsonValueKind.Object
                || !shape.TryGetProperty(RecordFields.Fields, out var rows)
                || rows.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var shown = prefix.Length > 0 ? prefix + "." + name : name;

            if (Str(entry, RecordFields.RowType) is { Length: > 0 } rowType && !IsPartReference(rowType))
            {
                AddRowDrift(source, hintFile, rowType, rows, shown, drift);
            }

            AddSharedRowDrift(source, hintFile, rows, shown, drift);
        }
    }

    /// <summary>Compares one stated row shape with the members of the backend type its rowType names, when the type resolves outside the generated files.</summary>
    /// <param name="source">The backend source the named row type is read from.</param>
    /// <param name="hintFile">The message or result file the named row type is searched beside.</param>
    /// <param name="rowType">The row type name the record states.</param>
    /// <param name="rows">The stated row fields.</param>
    /// <param name="shown">The field path the clauses show, qualified by its groups and rows.</param>
    /// <param name="drift">The drift clauses the row comparison appends to.</param>
    static void AddRowDrift(MessageSource source, string hintFile, string rowType, JsonElement rows, string shown, List<string> drift)
    {
        if (NestedName(rowType) is not { Length: > 0 } simple
            || source.FileOf(simple, hintFile) is not { } file
            || file.EndsWith(".generated.cs", StringComparison.Ordinal)
            || source.TypesIn(file, simple) is not { Count: > 0 } declarations)
        {
            return;
        }

        drift.AddRange(Compare(FieldsOf(rows), PropertiesOf(source, declarations), "row field", shown));
    }

    /// <summary>Whether a rowType value names an aggregate part: an aggregate reference with a part after a number sign, as the contract checks read it.</summary>
    /// <param name="reference">The rowType value.</param>
    /// <returns>True when the value names a part of an aggregate; otherwise false.</returns>
    static bool IsPartReference(string reference)
    {
        if (!reference.StartsWith("aggregate/", StringComparison.Ordinal))
        {
            return false;
        }

        var hash = reference.IndexOf('#');

        return hash > 0 && hash < reference.Length - 1;
    }

    /// <summary>The message properties the use case implies through its other sections, never restated in input: the idempotency key, the paging cursor and limit, and the tenant identifier.</summary>
    /// <param name="useCase">The use case record holding the sections.</param>
    /// <param name="tenant">The tenant term id the tenancy selection names, or empty.</param>
    /// <returns>The implied property names.</returns>
    static HashSet<string> ImpliedMessage(Record useCase, string tenant)
    {
        var implied = new HashSet<string>(StringComparer.Ordinal);

        if (useCase.Raw.ValueKind != JsonValueKind.Object)
        {
            return implied;
        }

        if (useCase.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            && Str(idempotency, RecordFields.Mode) == "key")
        {
            implied.Add("IdempotencyKey");
        }

        if (Paged(useCase))
        {
            implied.Add("Cursor");
            implied.Add("Limit");

            if (useCase.Raw.TryGetProperty(RecordFields.Paging, out var paging)
                && paging.ValueKind == JsonValueKind.Object
                && paging.TryGetProperty(RecordFields.Offset, out _))
            {
                implied.Add("Offset");
            }
        }

        if (tenant.Length > 0
            && useCase.Raw.TryGetProperty(RecordFields.Access, out var access)
            && Str(access, RecordFields.Tenant) is "own" or "all"
            && Identifiers.TrySplit(tenant, out _, out _, out var name)
            && name.Length > 0)
        {
            var term = Naming.Pascal(name);
            implied.Add(term);
            implied.Add(term + "Id");
        }

        return implied;
    }

    /// <summary>The tenant-key clause for a tenant-scoped message carrying no tenant key, or null when the use case implies none, an input identifying the tenant aggregate carries it, or the message carries it under the identity form or the bare term name.</summary>
    /// <param name="model">The loaded project model holding the tenant aggregate.</param>
    /// <param name="useCase">The use case record holding the tenant scope and inputs.</param>
    /// <param name="tenant">The tenant term id the tenancy selection names, or empty.</param>
    /// <param name="properties">The carried properties by name with their types as written.</param>
    /// <returns>The drift clause naming the tenant rule, or null.</returns>
    static string? MissingTenantKey(ProjectModel model, Record useCase, string tenant, Dictionary<string, string> properties)
    {
        var scope = useCase.Raw.ValueKind == JsonValueKind.Object
            && useCase.Raw.TryGetProperty(RecordFields.Access, out var access)
                ? Str(access, RecordFields.Tenant)
                : null;

        if (tenant.Length == 0
            || scope is not ("own" or "all")
            || !Identifiers.TrySplit(tenant, out _, out _, out var name)
            || name.Length == 0)
        {
            return null;
        }

        var term = Naming.Pascal(name);

        if (properties.ContainsKey(term) || properties.ContainsKey(term + "Id"))
        {
            return null;
        }

        var aggregate = model.Records
            .Where(candidate => candidate.Type == RecordTypes.Aggregate)
            .Where(candidate => Identifiers.TrySplit(candidate.Id, out _, out _, out var aggregateName) && aggregateName == name)
            .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
            .Select(candidate => candidate.Id)
            .FirstOrDefault();

        if (aggregate is not null
            && useCase.Raw.TryGetProperty(RecordFields.Input, out var inputs)
            && inputs.ValueKind == JsonValueKind.Array
            && inputs.EnumerateArray().Any(input => Str(input, RecordFields.Identifies) == aggregate))
        {
            return null;
        }

        return $"no property for tenant key '{term}Id' though the use case declares tenant '{scope}'";
    }

    /// <summary>The result properties the use case implies through its other sections, never restated in output: the next page cursor of a cursor-paged query.</summary>
    /// <param name="useCase">The use case record holding the sections.</param>
    /// <returns>The implied property names.</returns>
    static HashSet<string> ImpliedResult(Record useCase) =>
        CursorPaged(useCase) ? ["NextCursor"] : [];

    /// <summary>Whether the use case pages its rows: it declares paging with a mode other than none.</summary>
    /// <param name="useCase">The use case record holding the paging.</param>
    /// <returns>True when the use case pages its rows.</returns>
    static bool Paged(Record useCase) =>
        useCase.Raw.ValueKind == JsonValueKind.Object
        && useCase.Raw.TryGetProperty(RecordFields.Paging, out var paging)
        && Str(paging, RecordFields.Mode) is { Length: > 0 } mode
        && mode != "none";

    /// <summary>Whether the use case pages its rows by cursor: it declares paging with a stated cursor, so its result carries the next cursor.</summary>
    /// <param name="useCase">The use case record holding the paging.</param>
    /// <returns>True when the use case pages by cursor; otherwise false.</returns>
    static bool CursorPaged(Record useCase) =>
        useCase.Raw.ValueKind == JsonValueKind.Object
        && useCase.Raw.TryGetProperty(RecordFields.Paging, out var paging)
        && paging.ValueKind == JsonValueKind.Object
        && Str(paging, RecordFields.Mode) is { Length: > 0 } mode
        && mode != "none"
        && Str(paging, RecordFields.Cursor) is { Length: > 0 };

    /// <summary>Whether an output group is optional: true only when it states required false, so a single optional group renders nested.</summary>
    /// <param name="group">The output group object.</param>
    /// <returns>True when the use case may answer without the group; otherwise false.</returns>
    static bool IsOptionalGroup(JsonElement group) =>
        group.TryGetProperty(RecordFields.Required, out var required) && required.ValueKind == JsonValueKind.False;

    /// <summary>Compares each output group with its nested type, when the result holds one property per group.</summary>
    /// <param name="source">The backend source each nested type is read from.</param>
    /// <param name="hintFile">The result file the nested types are searched beside.</param>
    /// <param name="groups">The output groups with their fields.</param>
    /// <param name="properties">The result properties by name with their types as written.</param>
    /// <param name="drift">The drift clauses across the groups.</param>
    /// <returns>True when the result maps onto the groups with certainty; otherwise false, and the output is left unchecked.</returns>
    static bool TryNestedDrift(
        MessageSource source,
        string hintFile,
        List<(string Name, List<(string Name, string Type, string CurrencyField, bool Many)> Fields)> groups,
        Dictionary<string, string> properties,
        out List<string> drift)
    {
        drift = [];

        if (properties.Count != groups.Count)
        {
            return false;
        }

        var nested = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, _) in groups)
        {
            var expected = Naming.Pascal(name);

            if (!properties.TryGetValue(expected, out var type))
            {
                return false;
            }

            nested[name] = type;
        }

        foreach (var (name, fields) in groups)
        {
            var type = NestedName(nested[name]);

            if (type is null
                || source.FileOf(type, hintFile) is not { } file
                || source.TypesIn(file, type) is not { Count: > 0 } declarations)
            {
                continue;
            }

            drift.AddRange(Compare(fields, PropertiesOf(source, declarations), "output field", name));
        }

        return true;
    }

    /// <summary>The element type a read-only list holds: the type between its brackets, or null when the type lists nothing.</summary>
    /// <param name="type">The property type as written.</param>
    /// <returns>The element type as written, or null.</returns>
    static string? ListElementOf(string type)
    {
        var name = type.Trim().TrimEnd('?', '!');

        if (name.StartsWith("global::", StringComparison.Ordinal))
        {
            name = name["global::".Length..];
        }

        var open = name.IndexOf("IReadOnlyList<", StringComparison.Ordinal);

        if (open < 0 || !name.EndsWith('>'))
        {
            return null;
        }

        var element = name[(open + "IReadOnlyList<".Length)..^1];

        return element.Length > 0 ? element : null;
    }

    /// <summary>The nested type a group property holds: its simple name, or null for a generic or missing type.</summary>
    /// <param name="type">The property type as written.</param>
    /// <returns>The simple nested type name, or null.</returns>
    static string? NestedName(string type)
    {
        var name = type.Trim().TrimEnd('?', '!');

        if (name.Length == 0 || name.Contains('<'))
        {
            return null;
        }

        var dot = name.LastIndexOf('.');

        return dot >= 0 ? name[(dot + 1)..] : name;
    }

    /// <summary>Compares declared fields with carried properties: the missing, the extra and the mistyped, in order.</summary>
    /// <param name="fields">The declared fields with their record types.</param>
    /// <param name="properties">The carried properties by name with their types as written.</param>
    /// <param name="label">The field label each clause names.</param>
    /// <param name="part">The output group the fields belong to, or empty for a flat shape.</param>
    /// <param name="implied">The property names the record implies through its other sections, never reported as extra.</param>
    /// <returns>The drift clauses, in field then property order.</returns>
    static List<string> Compare(
        List<(string Name, string Type, string CurrencyField, bool Many)> fields, Dictionary<string, string> properties, string label, string part = "", HashSet<string>? implied = null)
    {
        var drift = new List<string>();
        var consumed = new HashSet<string>(StringComparer.Ordinal);
        var mistyped = new List<string>();

        foreach (var (name, type, currencyField, many) in fields)
        {
            var shown = part.Length == 0 ? name : part + "." + name;

            if (!MatchField(name, type, currencyField, properties, consumed))
            {
                drift.Add($"no property for {label} '{shown}'");
            }
            else if (MismatchOf(name, shown, type, many, properties, consumed, label) is { } mismatch)
            {
                mistyped.Add(mismatch);
            }
        }

        foreach (var property in properties.Keys.Order(StringComparer.Ordinal))
        {
            if (!consumed.Contains(property) && implied?.Contains(property) != true)
            {
                drift.Add(part.Length == 0
                    ? $"extra property '{property}'"
                    : $"extra property '{property}' in part '{part}'");
            }
        }

        drift.AddRange(mistyped.Order(StringComparer.Ordinal));

        return drift;
    }

    /// <summary>Matches one declared field against the carried properties, consuming what it matches.</summary>
    /// <param name="name">The declared field name.</param>
    /// <param name="type">The declared record type.</param>
    /// <param name="currencyField">The sibling field carrying a money field's currency, or empty when the record states none.</param>
    /// <param name="properties">The carried properties by name with their types as written.</param>
    /// <param name="consumed">The property names matched so far.</param>
    /// <returns>True when a property matches the field.</returns>
    static bool MatchField(string name, string type, string currencyField, Dictionary<string, string> properties, HashSet<string> consumed)
    {
        var expected = Naming.Pascal(name);

        if (!properties.TryGetValue(expected, out var written))
        {
            return false;
        }

        consumed.Add(expected);

        // A money field may ride as an amount with its currency beside it: the pair
        // answers the field together, as ChangeReceiptThreshold carries Threshold with Currency.
        // The record names the sibling in currencyField; Currency answers when it states none.
        if (type == "money" && IsDecimal(written))
        {
            var pair = currencyField.Length > 0 ? Naming.Pascal(currencyField) : "Currency";

            if (properties.TryGetValue(pair, out var currency) && IsText(currency))
            {
                consumed.Add(pair);
            }
        }

        return true;
    }

    /// <summary>The type clause for a matched field whose property type misses its declared type, or null when the pair is uncertain or matches.</summary>
    /// <param name="name">The declared field name.</param>
    /// <param name="shown">The field name the clause shows, qualified by its group when nested.</param>
    /// <param name="type">The declared record type.</param>
    /// <param name="many">True when the field holds a list of values of its type.</param>
    /// <param name="properties">The carried properties by name with their types as written.</param>
    /// <param name="consumed">The property names matched so far.</param>
    /// <param name="label">The field label the clause names.</param>
    /// <returns>The mistyped clause, or null.</returns>
    static string? MismatchOf(
        string name, string shown, string type, bool many, Dictionary<string, string> properties, HashSet<string> consumed, string label)
    {
        string? property = null;

        foreach (var candidate in Candidates(name))
        {
            if (consumed.Contains(candidate))
            {
                property = candidate;
                break;
            }
        }

        if (property is null)
        {
            return null;
        }

        var written = properties[property];

        if (many)
        {
            if (ListElementOf(written) is not { } element)
            {
                return $"property '{property}' is '{SimpleType(written)}' for {label} '{shown}' of type '{type}' holding many";
            }

            written = element;
        }

        var actual = SimpleType(written);

        var matches = type switch
        {
            "text" or "email" => IsText(written),
            "integer" => IsIntegral(actual),
            "date" => actual == "DateOnly",
            "instant" => actual == "DateTimeOffset",
            "duration" => actual == "TimeSpan",
            "decimal" or "money" => IsDecimal(written) || IsMoney(written),
            "boolean" => actual is "bool" or "Boolean",
            "id" => IsIdentifier(actual),
            "choice" => IsClosedSet(actual),
            "file" => IsBytes(actual),
            _ => true,
        };

        if (matches)
        {
            return null;
        }

        return $"property '{property}' is '{actual}' for {label} '{shown}' of type '{type}'";
    }

    /// <summary>The property names one declared field matches: its PascalCase name, as generators name theirs.</summary>
    /// <param name="name">The declared field name.</param>
    /// <returns>The candidate property names.</returns>
    static List<string> Candidates(string name) => [Naming.Pascal(name)];

    /// <summary>The declared fields of a contract array: each entry with a name, with its type, currency field, and many flag.</summary>
    /// <param name="record">The record holding the array.</param>
    /// <param name="field">The array field name.</param>
    /// <returns>The declared fields with their record types, in written order.</returns>
    static List<(string Name, string Type, string CurrencyField, bool Many)> ContractFields(Record record, string field)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(field, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return FieldsOf(array);
    }

    /// <summary>The declared fields of one output group.</summary>
    /// <param name="group">The output group object.</param>
    /// <returns>The declared fields with their record types, in written order.</returns>
    static List<(string Name, string Type, string CurrencyField, bool Many)> FieldsIn(JsonElement group)
    {
        if (!group.TryGetProperty(RecordFields.Fields, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return FieldsOf(array);
    }

    /// <summary>The declared fields of a contract array element.</summary>
    /// <param name="array">The contract array.</param>
    /// <returns>The declared fields with their record types, in written order.</returns>
    static List<(string Name, string Type, string CurrencyField, bool Many)> FieldsOf(JsonElement array)
    {
        var fields = new List<(string Name, string Type, string CurrencyField, bool Many)>();

        foreach (var entry in array.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty(RecordFields.Name, out var name)
                || name.ValueKind != JsonValueKind.String
                || name.GetString() is not { Length: > 0 } text)
            {
                continue;
            }

            var type = entry.TryGetProperty(RecordFields.Type, out var kind)
                && kind.ValueKind == JsonValueKind.String
                    ? kind.GetString() ?? ""
                    : "";
            var currencyField = entry.TryGetProperty(RecordFields.CurrencyField, out var carrier)
                && carrier.ValueKind == JsonValueKind.String
                    ? carrier.GetString() ?? ""
                    : "";
            var many = entry.TryGetProperty(RecordFields.Many, out var count)
                && count.ValueKind == JsonValueKind.True;

            fields.Add((text, type, currencyField, many));
        }

        return fields;
    }

    /// <summary>The carried properties by name with their resolved types, across every part of a partial type.</summary>
    /// <param name="source">The backend source each declaration is read from.</param>
    /// <param name="parts">Each declaration of the type with its tree.</param>
    /// <returns>The carried properties by name.</returns>
    static Dictionary<string, string> PropertiesOf(MessageSource source, IReadOnlyList<(SyntaxTree Tree, TypeDeclarationSyntax Declared)> parts)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (tree, declared) in parts)
        {
            foreach (var property in MessageProperties.Of(declared, source.SemanticOf(tree)))
            {
                properties.TryAdd(property.Name, property.Type);
            }
        }

        return properties;
    }

    /// <summary>The 1-based line a type declaration starts on.</summary>
    /// <param name="part">The declaration with its tree.</param>
    /// <returns>The 1-based line of the declaration.</returns>
    static int LineOf((SyntaxTree Tree, TypeDeclarationSyntax Declared) part) =>
        part.Tree.GetLineSpan(part.Declared.Span).StartLinePosition.Line + 1;

    /// <summary>Whether the type as written is a string.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>True for string or String, with or without nullability.</returns>
    static bool IsText(string type) => SimpleType(type) is "string" or "String";

    /// <summary>Whether the type as written is a decimal.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>True for decimal or Decimal, with or without nullability.</returns>
    static bool IsDecimal(string type) => SimpleType(type) is "decimal" or "Decimal";

    /// <summary>Whether the type as written is a money type.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>True when the simple name ends with Money.</returns>
    static bool IsMoney(string type) => SimpleType(type).EndsWith("Money", StringComparison.Ordinal);

    /// <summary>Whether a simple type name is an integral number.</summary>
    /// <param name="actual">The simple type name.</param>
    /// <returns>True for the integral C# types and their framework names.</returns>
    static bool IsIntegral(string actual) => actual is "int" or "Int32" or "long" or "Int64"
        or "short" or "Int16" or "byte" or "Byte" or "uint" or "UInt32" or "ulong" or "UInt64"
        or "ushort" or "UInt16" or "sbyte" or "SByte";

    /// <summary>Whether a simple type name is identifier-shaped: a Guid or a typed identifier.</summary>
    /// <param name="actual">The simple type name.</param>
    /// <returns>True for Guid or a name ending in Id.</returns>
    static bool IsIdentifier(string actual) => actual is "Guid" || actual.EndsWith("Id", StringComparison.Ordinal);

    /// <summary>Whether a simple type name is the carried file bytes.</summary>
    /// <param name="actual">The simple type name.</param>
    /// <returns>True for byte[] or Byte[].</returns>
    static bool IsBytes(string actual) => actual is "byte[]" or "Byte[]";

    /// <summary>Whether a simple type name is a closed-set type rather than a scalar the set never rides as.</summary>
    /// <param name="actual">The simple type name.</param>
    /// <returns>False for the scalar types; true for every named type.</returns>
    static bool IsClosedSet(string actual) => actual is not ("string" or "String"
        or "decimal" or "Decimal" or "double" or "Double" or "float" or "Single"
        or "bool" or "Boolean" or "DateOnly" or "DateTime" or "DateTimeOffset" or "TimeSpan" or "Guid")
        && !IsIntegral(actual);

    /// <summary>Reads the simple name of a type as written, without qualification, generic arguments or nullability.</summary>
    /// <param name="type">The type as written.</param>
    /// <returns>The simple name.</returns>
    static string SimpleType(string type)
    {
        var name = type.Trim().TrimEnd('?', '!');

        if (name.StartsWith("global::", StringComparison.Ordinal))
        {
            name = name["global::".Length..];
        }

        var generic = name.IndexOf('<');

        if (generic >= 0)
        {
            name = name[..generic];
        }

        var dot = name.LastIndexOf('.');

        return dot >= 0 ? name[(dot + 1)..] : name;
    }
}
