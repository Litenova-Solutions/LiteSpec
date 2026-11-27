using static LiteSpec.Cli.Scaffold.ScaffoldPlan;

namespace LiteSpec.Cli.Scaffold;

/// <summary>
///     Renders one use case scaffold as C#: the message, handler, validator and result in
///     the use case Application folder, one definition file per pipeline concern, and the
///     endpoint with its request and response models when a web entry point reaches it.
/// </summary>
/// <follows>litespec/core/backend-application#organize-application-by-use-case</follows>
static class ScaffoldRender
{
    /// <summary>Renders every scaffold file of the plan, in write order.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="root">The consumer root the relative paths resolve under.</param>
    /// <returns>The rendered files with consumer-relative paths.</returns>
    public static IReadOnlyList<ScaffoldFile> Render(ScaffoldedUseCase plan, string root)
    {
        var names = NamesOf(plan);
        var moduleDir = Path.GetRelativePath(root, plan.ModuleProject);
        var layers = plan.AggregateName is null ? [] : new[] { names.Aggregates };
        var appDir = Path.Combine([moduleDir, "Application", .. layers, plan.Stem]);
        var files = new List<ScaffoldFile>
        {
            new(At(appDir, names.Message + ".cs"), Message(plan, names)),
            new(At(appDir, names.Message + "Handler.cs"), Handler(plan, names)),
        };

        if (plan.Kind == "command" && plan.Inputs.Count > 0)
        {
            files.Add(new(At(appDir, names.Message + "Validator.cs"), Validator(plan, names)));
        }

        files.Add(new(At(appDir, names.Result + ".cs"), Result(plan, names)));

        if (plan.Grant is not null)
        {
            files.Add(new(At(appDir, names.Message + "AuthorizationDefinition.cs"), Authorization(plan, names)));
        }

        if (!plan.AuditExempt)
        {
            files.Add(new(At(appDir, names.Message + "AuditDefinition.cs"), Audit(plan, names)));
        }

        if (plan.Tenant is not null)
        {
            files.Add(new(At(appDir, names.Message + "TenancyDefinition.cs"), Tenancy(plan, names)));
        }

        if (plan.Kind == "command" && plan.IdempotencyMode is not null)
        {
            files.Add(new(At(appDir, names.Message + "IdempotencyDefinition.cs"), Idempotency(plan, names)));
        }

        if (plan.WriteEndpoint)
        {
            var webDir = Path.Combine([moduleDir, "WebApi", .. layers, plan.Stem]);
            files.Add(new(At(webDir, plan.Stem + "Endpoint.cs"), Endpoint(plan, names)));
            files.Add(new(At(webDir, plan.Stem + "Request.cs"), Request(plan, names)));
            files.Add(new(At(webDir, plan.Stem + "Response.cs"), Response(plan, names)));
        }

        return files;
    }

    /// <summary>The computed namespaces and type names of the scaffold.</summary>
    /// <param name="App">The Application folder namespace.</param>
    /// <param name="Web">The WebApi folder namespace.</param>
    /// <param name="Domain">The aggregate Domain namespace.</param>
    /// <param name="Aggregates">The plural aggregate folder name.</param>
    /// <param name="Aggregate">The aggregate type name.</param>
    /// <param name="Message">The message type name.</param>
    /// <param name="Result">The result type name.</param>
    private sealed record ScaffoldNames(
        string App,
        string Web,
        string Domain,
        string Aggregates,
        string Aggregate,
        string Message,
        string Result);

    /// <summary>The computed namespaces and type names of the plan.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <returns>The names the renderers share.</returns>
    private static ScaffoldNames NamesOf(ScaffoldedUseCase plan)
    {
        var aggregates = plan.AggregateName is null ? "" : LiteSpec.Model.Naming.PluralPascal(plan.AggregateName);
        var aggregate = plan.AggregateName is null ? "" : LiteSpec.Model.Naming.Pascal(plan.AggregateName);
        var segment = plan.AggregateName is null ? "" : "." + aggregates;
        var message = plan.Stem + (plan.Kind == "command" ? "Command" : "Query");
        var result = plan.Stem + "Result";

        return new ScaffoldNames(
            $"{plan.NamespaceBase}.Application{segment}.{plan.Stem}",
            $"{plan.NamespaceBase}.WebApi{segment}.{plan.Stem}",
            $"{plan.NamespaceBase}.Domain.{aggregates}",
            aggregates,
            aggregate,
            message,
            result);
    }

    /// <summary>The consumer-relative path of a file in a folder.</summary>
    /// <param name="folder">The folder relative to the consumer root.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The relative file path.</returns>
    private static string At(string folder, string file) => Path.Combine(folder, file);

    /// <summary>Renders the command or query message from the record inputs.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Message(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
            $"/// <summary>{plan.Title}.</summary>",
        };

        foreach (var input in plan.Inputs)
        {
            lines.Add($"/// <param name=\"{ScaffoldNaming.PropertyName(input.Name)}\">{input.Meaning}</param>");
        }

        lines.Add($"/// <implements>{plan.UseCaseId}</implements>");

        var rows = MessageRows(plan, names).ToList();

        if (plan.Inputs.Count == 0)
        {
            lines.Add($"internal sealed record {names.Message};");
        }
        else
        {
            lines.Add($"internal sealed record {names.Message}(");

            for (var index = 0; index < plan.Inputs.Count; index++)
            {
                var input = plan.Inputs[index];
                var last = index == plan.Inputs.Count - 1;
                var comma = last && rows.Count == 0 ? ");" : last ? ")" : ",";
                lines.Add($"    {InputType(plan, names, input, names.Message)} {ScaffoldNaming.PropertyName(input.Name)}{comma}");
            }
        }

        if (rows.Count > 0)
        {
            lines.Add("{");

            foreach (var row in rows)
            {
                lines.AddRange(row.Select(line => line.Length > 0 ? "    " + line : line));
            }

            lines.Add("}");
        }

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The nested row records of message list inputs carrying rows.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The nested record lines, one block per row input.</returns>
    private static IEnumerable<List<string>> MessageRows(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        foreach (var input in plan.Inputs)
        {
            if (input.RowFields is not null)
            {
                yield return RowRecord(RowName(input.Name), input.RowFields, input.Meaning);
            }
        }
    }

    /// <summary>The nested row record name of a list field, such as LoanRow for loans.</summary>
    /// <param name="fieldName">The field name as written.</param>
    /// <returns>The row record name.</returns>
    private static string RowName(string fieldName) => ScaffoldNaming.PropertyName(ScaffoldNaming.Singular(fieldName)) + "Row";

    /// <summary>The C# type of a message input, with nested rows named for the message.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <param name="input">The input to type.</param>
    /// <param name="owner">The type owning the nested rows.</param>
    /// <returns>The C# type name.</returns>
    private static string InputType(ScaffoldedUseCase plan, ScaffoldNames names, ScaffoldInput input, string owner)
    {
        if (input.RowFields is not null)
        {
            return ScaffoldNaming.FieldType("list", input.Required, owner + "." + RowName(input.Name));
        }

        if (input.Type == "list")
        {
            return ScaffoldNaming.FieldType("list", input.Required, ScaffoldNaming.FieldType(input.ScalarItem ?? "string", true));
        }

        return ScaffoldNaming.FieldType(input.Type, input.Required);
    }

    /// <summary>Renders the command or query handler: load, call and stage for a command, projection for a query.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Handler(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var lines = new List<string>();

        if (plan.Kind == "command")
        {
            lines.Add($"using {names.Domain};");
            lines.Add("");
        }

        lines.Add($"namespace {names.App};");
        lines.Add("");
        lines.Add($"/// <summary>{plan.FirstStatement ?? plan.Title + "."}</summary>");
        lines.Add($"/// <implements>{plan.UseCaseId}</implements>");

        return plan.Kind == "command"
            ? string.Join('\n', CommandHandler(plan, names, lines)) + "\n"
            : string.Join('\n', QueryHandler(plan, names, lines)) + "\n";
    }

    /// <summary>Appends the command handler body: load the aggregate, call the act, and stage it.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <param name="lines">The file lines built so far.</param>
    /// <returns>The complete file lines.</returns>
    private static List<string> CommandHandler(ScaffoldedUseCase plan, ScaffoldNames names, List<string> lines)
    {
        var variable = char.ToLowerInvariant(names.Aggregate[0]) + names.Aggregate[1..];
        var repository = "I" + names.Aggregate + "Repository";
        var field = "_" + variable + "Repository";
        var loader = LoadInput(plan);
        var load = loader is null
            ? $"var {variable} = new {names.Aggregate}();"
            : $"var {variable} = await {field}.GetByIdAsync(command.{ScaffoldNaming.PropertyName(loader.Name)}, cancellationToken);";

        lines.Add($"internal sealed class {names.Message}Handler : ICommandHandler<{names.Message}, {names.Result}>");
        lines.Add("{");
        lines.Add($"    /// <summary>The {plan.AggregateName} store the handler loads and stages.</summary>");
        lines.Add($"    private readonly {repository} {field};");
        lines.Add("");
        lines.Add($"    /// <summary>Initializes a new instance of the <see cref=\"{names.Message}Handler\"/> class.</summary>");
        lines.Add($"    /// <param name=\"{variable}Repository\">The {plan.AggregateName} store the handler loads and stages.</param>");
        lines.Add($"    public {names.Message}Handler({repository} {variable}Repository)");
        lines.Add("    {");
        lines.Add($"        {field} = {variable}Repository;");
        lines.Add("    }");
        lines.Add("");
        lines.Add("    /// <summary>Handles the message: loads the aggregate, calls the act, and stages the change.</summary>");
        lines.Add("    /// <param name=\"command\">The command to handle.</param>");
        lines.Add("    /// <param name=\"cancellationToken\">The token to cancel the operation.</param>");
        lines.Add("    /// <returns>The command result.</returns>");
        lines.Add($"    public async Task<{names.Result}> HandleAsync({names.Message} command, CancellationToken cancellationToken)");
        lines.Add("    {");
        lines.Add($"        {load}");
        lines.Add($"        {variable}.{plan.Act}();");
        lines.Add($"        {field}.Store({variable});");
        lines.Add($"        return {ResultValue(plan, names, variable)};");
        lines.Add("    }");
        lines.Add("}");

        return lines;
    }

    /// <summary>The input the handler loads the aggregate by: the first caller id input, else the first id input.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <returns>The loading input, or null when no input carries an id.</returns>
    private static ScaffoldInput? LoadInput(ScaffoldedUseCase plan)
    {
        foreach (var input in plan.Inputs)
        {
            if (input.Type == "id" && input.Origin == "caller")
            {
                return input;
            }
        }

        foreach (var input in plan.Inputs)
        {
            if (input.Type == "id")
            {
                return input;
            }
        }

        return null;
    }

    /// <summary>The result value the command handler returns.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <param name="variable">The aggregate variable name.</param>
    /// <returns>The return expression.</returns>
    private static string ResultValue(ScaffoldedUseCase plan, ScaffoldNames names, string variable)
    {
        if (plan.CommandOutputs is null || plan.CommandOutputs.Count == 0)
        {
            return $"new {names.Result}({variable}.Id)";
        }

        var identity = names.Aggregate + "Id";
        var values = plan.CommandOutputs
            .Select(output => string.Equals(ScaffoldNaming.PropertyName(output.Name), identity, StringComparison.Ordinal)
                ? variable + ".Id"
                : "default")
            .ToList();

        return $"new {names.Result}({string.Join(", ", values)})";
    }

    /// <summary>Appends the query handler body: project the read through the read session.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <param name="lines">The file lines built so far.</param>
    /// <returns>The complete file lines.</returns>
    private static List<string> QueryHandler(ScaffoldedUseCase plan, ScaffoldNames names, List<string> lines)
    {
        lines.Add($"internal sealed class {names.Message}Handler : IQueryHandler<{names.Message}, {names.Result}>");
        lines.Add("{");
        lines.Add("    /// <summary>The read session the handler projects through.</summary>");
        lines.Add("    private readonly IQuerySession _session;");
        lines.Add("");
        lines.Add($"    /// <summary>Initializes a new instance of the <see cref=\"{names.Message}Handler\"/> class.</summary>");
        lines.Add("    /// <param name=\"session\">The read session the handler projects through.</param>");
        lines.Add($"    public {names.Message}Handler(IQuerySession session)");
        lines.Add("    {");
        lines.Add("        _session = session;");
        lines.Add("    }");
        lines.Add("");
        lines.Add("    /// <summary>Handles the query: projects the read session into the result.</summary>");
        lines.Add("    /// <param name=\"query\">The query to handle.</param>");
        lines.Add("    /// <param name=\"cancellationToken\">The token to cancel the operation.</param>");
        lines.Add("    /// <returns>The query result.</returns>");
        lines.Add($"    public Task<{names.Result}> HandleAsync({names.Message} query, CancellationToken cancellationToken)");
        lines.Add("    {");
        lines.Add($"        return Task.FromResult({EmptyResult(plan, names)});");
        lines.Add("    }");
        lines.Add("}");

        return lines;
    }

    /// <summary>The empty query result the handler returns before the reader fills the projection.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The empty result expression.</returns>
    private static string EmptyResult(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var values = new List<string>();

        foreach (var group in plan.QueryGroups ?? [])
        {
            foreach (var field in group.Fields)
            {
                values.Add(field.Type == "list" ? "[]" : "default");
            }
        }

        return $"new {names.Result}({string.Join(", ", values)})";
    }

    /// <summary>Renders the validator of a command with inputs: one presence check per required caller input.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Validator(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
            $"/// <summary>Checks the structural input of {plan.UseCaseName} before Domain runs.</summary>",
            $"/// <implements>{plan.UseCaseId}</implements>",
            $"internal sealed class {names.Message}Validator",
            "{",
            "    /// <summary>A structural finding: the member, the reason code, and the caller-safe message.</summary>",
            "    /// <param name=\"Member\">The input member the finding reports on.</param>",
            "    /// <param name=\"Code\">The stable reason code.</param>",
            "    /// <param name=\"Message\">The caller-safe message.</param>",
            "    public sealed record ValidationFinding(string Member, string Code, string Message);",
            "",
            "    /// <summary>Validates the message, reporting every structural problem in one answer.</summary>",
            $"    /// <param name=\"command\">The command to validate.</param>",
            "    /// <returns>The structural findings; empty when the input is well formed.</returns>",
            $"    public IReadOnlyList<ValidationFinding> Validate({names.Message} command)",
            "    {",
            "        var findings = new List<ValidationFinding>();",
            "",
        };

        foreach (var input in plan.Inputs)
        {
            if (input is { Required: true, Origin: "caller" } && PresenceCheck(input) is { } check)
            {
                var member = ScaffoldNaming.PropertyName(input.Name);
                lines.Add($"        if ({check})");
                lines.Add("        {");
                lines.Add($"            findings.Add(new ValidationFinding(\"{member}\", \"required\", \"A {input.Name} is required.\"));");
                lines.Add("        }");
                lines.Add("");
            }
        }

        lines.Add("        return findings;");
        lines.Add("    }");
        lines.Add("}");

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The presence check of a required caller input, or null when its type carries no empty value.</summary>
    /// <param name="input">The input to check.</param>
    /// <returns>The check expression, or null.</returns>
    private static string? PresenceCheck(ScaffoldInput input)
    {
        var member = "command." + ScaffoldNaming.PropertyName(input.Name);

        if (input.Type is "text" or "email" or "choice")
        {
            return $"string.IsNullOrWhiteSpace({member})";
        }

        if (input.Type == "id")
        {
            return $"{member} == Guid.Empty";
        }

        if (input.Type == "list")
        {
            return $"{member} is null || {member}.Count == 0";
        }

        return null;
    }

    /// <summary>Renders the command or query result from the record outputs.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Result(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
        };

        if (plan.Kind == "command")
        {
            var outputs = plan.CommandOutputs ?? [];
            var summary = outputs.Count == 0 && plan.SuccessMeaning is { Length: > 0 } meaning
                ? meaning
                : plan.Title + ".";

            if (outputs.Count == 0 && plan.AggregateName is not null)
            {
                lines.Add($"/// <summary>{summary}</summary>");
                lines.Add($"/// <param name=\"{names.Aggregate}Id\">The identity of the {plan.AggregateName}.</param>");
                lines.Add($"/// <implements>{plan.UseCaseId}</implements>");
                lines.Add($"internal sealed record {names.Result}(Guid {names.Aggregate}Id);");
            }
            else
            {
                lines.Add($"/// <summary>{summary}</summary>");

                foreach (var output in outputs)
                {
                    lines.Add($"/// <param name=\"{ScaffoldNaming.PropertyName(output.Name)}\">{output.Meaning}</param>");
                }

                lines.Add($"/// <implements>{plan.UseCaseId}</implements>");
                lines.Add($"internal sealed record {names.Result}(");

                for (var index = 0; index < outputs.Count; index++)
                {
                    var comma = index == outputs.Count - 1 ? ");" : ",";
                    lines.Add($"    {ShapeType(outputs[index], names.Result)} {ScaffoldNaming.PropertyName(outputs[index].Name)}{comma}");
                }

                var rows = outputs.Where(output => output.RowFields is not null).ToList();

                if (rows.Count > 0)
                {
                    lines[^1] = lines[^1][..^2] + ")";
                    lines.Add("{");

                    foreach (var output in rows)
                    {
                        lines.AddRange(RowRecord(RowName(output.Name), output.RowFields!, output.Meaning)
                            .Select(line => line.Length > 0 ? "    " + line : line));
                    }

                    lines.Add("}");
                }
            }
        }
        else
        {
            var summary = plan.SuccessMeaning is { Length: > 0 } meaning ? meaning : plan.Title + ".";
            var fields = (plan.QueryGroups ?? []).SelectMany(group => group.Fields).ToList();
            lines.Add($"/// <summary>{summary}</summary>");

            foreach (var field in fields)
            {
                lines.Add($"/// <param name=\"{ScaffoldNaming.PropertyName(field.Name)}\">{field.Meaning}</param>");
            }

            lines.Add($"/// <implements>{plan.UseCaseId}</implements>");

            if (fields.Count == 0)
            {
                lines.Add($"internal sealed record {names.Result};");
            }
            else
            {
                lines.Add($"internal sealed record {names.Result}(");

                for (var index = 0; index < fields.Count; index++)
                {
                    var comma = index == fields.Count - 1 ? ")" : ",";
                    lines.Add($"    {ShapeType(fields[index], names.Result)} {ScaffoldNaming.PropertyName(fields[index].Name)}{comma}");
                }

                lines.Add("{");

                foreach (var field in fields)
                {
                    if (field.RowFields is not null)
                    {
                        lines.AddRange(RowRecord(RowName(field.Name), field.RowFields, field.Meaning)
                            .Select(line => line.Length > 0 ? "    " + line : line));
                    }
                }

                lines.Add("}");
            }
        }

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The C# type of an output value, with nested rows named for the result.</summary>
    /// <param name="field">The output value to type.</param>
    /// <param name="result">The result type owning the nested rows.</param>
    /// <returns>The C# type name.</returns>
    private static string ShapeType(FieldShape field, string result)
    {
        if (field.RowFields is not null)
        {
            return $"IReadOnlyList<{result}.{RowName(field.Name)}>";
        }

        if (field.Type == "list")
        {
            return ScaffoldNaming.FieldType("list", true, ScaffoldNaming.FieldType(field.ScalarItem ?? "string", true));
        }

        return ScaffoldNaming.FieldType(field.Type, true);
    }

    /// <summary>One nested row record with a line per leaf field.</summary>
    /// <param name="name">The row record name.</param>
    /// <param name="leaves">The leaf fields in record order.</param>
    /// <param name="meaning">The row meaning the record states.</param>
    /// <returns>The record lines.</returns>
    private static List<string> RowRecord(string name, IReadOnlyList<FieldShape> leaves, string meaning)
    {
        var lines = new List<string> { $"/// <summary>{meaning}</summary>" };

        foreach (var leaf in leaves)
        {
            lines.Add($"/// <param name=\"{ScaffoldNaming.PropertyName(leaf.Name)}\">{leaf.Meaning}</param>");
        }

        lines.Add($"internal sealed record {name}(");

        for (var index = 0; index < leaves.Count; index++)
        {
            var comma = index == leaves.Count - 1 ? ");" : ",";
            lines.Add($"    {ScaffoldNaming.FieldType(leaves[index].Type, leaves[index].Required)} {ScaffoldNaming.PropertyName(leaves[index].Name)}{comma}");
        }

        return lines;
    }

    /// <summary>Renders the authorization definition from the access grant.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Authorization(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var declaration = AuthorizationOf(plan);
        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
            $"/// <summary>Authorizes {plan.Title} from the {plan.Grant} grant.</summary>",
            $"/// <implements>{plan.UseCaseId}</implements>",
            $"internal sealed class {names.Message}AuthorizationDefinition : IAuthorizationDefinition<{names.Message}>",
            "{",
            "    /// <summary>Gets the authorization the guard evaluates for the message.</summary>",
            $"    public AuthorizationDeclaration Authorization => {declaration};",
            "}",
        };

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The authorization declaration for the access grant.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <returns>The declaration expression.</returns>
    private static string AuthorizationOf(ScaffoldedUseCase plan)
    {
        var grant = plan.Grant ?? "anonymous";
        string declaration = grant switch
        {
            "anonymous" => "AuthorizationDeclaration.Exempt(\"Anonymous callers reach this use case.\")",
            "system" => "AuthorizationDeclaration.Authorized(AccessPolicies.System)",
            "signed-in" => "AuthorizationDeclaration.Authorized(AccessPolicies.SignedIn)",
            _ when grant.StartsWith("self(permission/", StringComparison.Ordinal) && grant.EndsWith(')') =>
                $"AuthorizationDeclaration.Authorized(AccessPolicies.Self(\"{ScaffoldNaming.Escape(grant["self(permission/".Length..^1])}\"))",
            _ when grant.StartsWith("permission/", StringComparison.Ordinal) =>
                $"AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"{ScaffoldNaming.Escape(grant["permission/".Length..])}\"))",
            _ => $"AuthorizationDeclaration.Exempt(\"The {grant} grant reaches this use case.\")",
        };

        if (plan.RecentSignInWindow is { Length: > 0 } window)
        {
            declaration += $".WithRecentSignIn(\"{ScaffoldNaming.Escape(window)}\")";
        }

        return declaration;
    }

    /// <summary>Renders the audit definition from the audit target.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Audit(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var declaration = $"AuditDeclaration.Audited(\"{plan.UseCaseId}\")";
        var summary = plan.AuditCategory is { Length: > 0 } category
            ? $"Audits {plan.Title} under {category}."
            : $"Audits {plan.Title}.";

        if (plan.AuditTargetInputs is { Count: > 0 } targets)
        {
            var projector = targets.Count == 1
                ? $"static command => command.{ScaffoldNaming.PropertyName(targets[0])}"
                : $"static command => new {{ {string.Join(", ", targets.Select(target => "command." + ScaffoldNaming.PropertyName(target)))} }}";
            declaration += $".WithTargetFrom<{names.Message}>({projector})";
        }

        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
            $"/// <summary>{summary}</summary>",
            $"/// <implements>{plan.UseCaseId}</implements>",
            $"internal sealed class {names.Message}AuditDefinition : IAuditDefinition<{names.Message}>",
            "{",
            "    /// <summary>Gets the audit the pipeline records for the message.</summary>",
            $"    public AuditDeclaration Audit => {declaration};",
            "}",
        };

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>Renders the tenancy definition from the access tenant reach.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Tenancy(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var declaration = plan.Tenant switch
        {
            "cross" => "TenancyDeclaration.CrossTenant()",
            "none" => plan.TenantReason is { Length: > 0 } reason
                ? $"TenancyDeclaration.None(\"{ScaffoldNaming.Escape(reason)}\")"
                : "TenancyDeclaration.None()",
            "public" => "TenancyDeclaration.Public()",
            _ => "TenancyDeclaration.Required()",
        };
        var summary = plan.TenantReason is { Length: > 0 } tenantReason
            ? $"Declares the {plan.Tenant} tenant reach: {tenantReason}."
            : $"Declares the {plan.Tenant} tenant reach for the message.";
        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
            $"/// <summary>{summary}</summary>",
            $"/// <implements>{plan.UseCaseId}</implements>",
            $"internal sealed class {names.Message}TenancyDefinition : ITenancyDefinition<{names.Message}>",
            "{",
            "    /// <summary>Gets the tenancy the guard enforces for the message.</summary>",
            $"    public TenancyDeclaration Tenancy => {declaration};",
            "}",
        };

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>Renders the idempotency definition from the idempotency mode.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Idempotency(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        string declaration = plan.IdempotencyMode switch
        {
            "key" => $"new Keyed<IdempotencyDeclaration>(\"{ScaffoldNaming.Escape(plan.IdempotencyKey ?? "")}\", \"{ScaffoldNaming.Escape(plan.IdempotencyScope ?? "")}\")",
            _ => $"new Exempt<IdempotencyDeclaration>(\"{ScaffoldNaming.Escape(plan.IdempotencyReason ?? plan.IdempotencyMode ?? "none")}\")",
        };
        var lines = new List<string>
        {
            $"namespace {names.App};",
            "",
            $"/// <summary>Declares the {plan.IdempotencyMode} idempotency of {plan.Title}.</summary>",
            $"/// <implements>{plan.UseCaseId}</implements>",
            $"internal sealed class {names.Message}IdempotencyDefinition : IIdempotencyDefinition<{names.Message}>",
            "{",
            "    /// <summary>Gets the idempotency the pipeline enforces for the message.</summary>",
            $"    public IdempotencyDeclaration Idempotency => {declaration};",
            "}",
        };

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>Renders the endpoint: maps the route, converts the request, and maps the result back.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Endpoint(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var map = plan.HttpMethod.ToUpperInvariant() switch
        {
            "GET" => "MapGet",
            "PUT" => "MapPut",
            "PATCH" => "MapPatch",
            "DELETE" => "MapDelete",
            _ => "MapPost",
        };
        var mediator = plan.Kind == "command" ? "ICommandMediator" : "IQueryMediator";
        var request = plan.Stem + "Request";
        var response = plan.Stem + "Response";
        var arguments = string.Join(", ", plan.Inputs.Select(input => "request." + ScaffoldNaming.PropertyName(input.Name)));
        var lines = new List<string>
        {
            $"using {names.App};",
            "",
            $"namespace {names.Web};",
            "",
            $"/// <summary>Serves {plan.Title} over HTTP.</summary>",
            $"/// <implements>{plan.UseCaseId}</implements>",
            $"internal static class {plan.Stem}Endpoint",
            "{",
            "    /// <summary>Maps the route the use case answers on.</summary>",
            "    /// <param name=\"app\">The route builder the endpoint maps on.</param>",
            "    public static void Map(WebApplication app)",
            "    {",
            $"        app.{map}(\"{plan.HttpRoute}\", HandleAsync).WithName(\"{plan.Stem}\");",
            "    }",
            "",
            "    /// <summary>Handles the HTTP call: converts the request, sends the message, and maps the result back.</summary>",
            $"    /// <param name=\"request\">The HTTP request.</param>",
            $"    /// <param name=\"commands\">The mediator the endpoint sends through.</param>",
            "    /// <param name=\"cancellationToken\">The token to cancel the operation.</param>",
            "    /// <returns>The HTTP response.</returns>",
            $"    private static async Task<IResult> HandleAsync({request} request, {mediator} commands, CancellationToken cancellationToken)",
            "    {",
            $"        var result = await commands.SendAsync(new {names.Message}({arguments}), cancellationToken);",
            $"        return Results.Ok({ResponseValue(plan, names)});",
            "    }",
            "}",
        };

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The response value the endpoint maps the result into.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The response expression.</returns>
    private static string ResponseValue(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var response = plan.Stem + "Response";

        if (plan.Kind == "command" && (plan.CommandOutputs is null || plan.CommandOutputs.Count == 0))
        {
            return $"new {response}(result.{names.Aggregate}Id)";
        }

        var fields = plan.Kind == "command"
            ? (IReadOnlyList<FieldShape>)(plan.CommandOutputs ?? [])
            : (plan.QueryGroups ?? []).SelectMany(group => group.Fields).ToList();
        var values = string.Join(", ", fields.Select(field => "result." + ScaffoldNaming.PropertyName(field.Name)));

        return $"new {response}({values})";
    }

    /// <summary>Renders the endpoint request model from the record inputs.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Request(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var lines = new List<string>();

        if (plan.Inputs.Any(input => input.RowFields is not null))
        {
            lines.Add($"using {names.App};");
            lines.Add("");
        }

        lines.Add($"namespace {names.Web};");
        lines.Add("");
        lines.Add($"/// <summary>{plan.Title} over HTTP.</summary>");

        foreach (var input in plan.Inputs)
        {
            lines.Add($"/// <param name=\"{ScaffoldNaming.PropertyName(input.Name)}\">{input.Meaning}</param>");
        }

        lines.Add($"/// <implements>{plan.UseCaseId}</implements>");

        if (plan.Inputs.Count == 0)
        {
            lines.Add($"internal sealed record {plan.Stem}Request;");
        }
        else
        {
            lines.Add($"internal sealed record {plan.Stem}Request(");

            for (var index = 0; index < plan.Inputs.Count; index++)
            {
                var input = plan.Inputs[index];
                var comma = index == plan.Inputs.Count - 1 ? ");" : ",";
                lines.Add($"    {RequestType(plan, names, input)} {ScaffoldNaming.PropertyName(input.Name)}{comma}");
            }
        }

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The C# type of a request field, mirroring the message with rows named for the message.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <param name="input">The input to type.</param>
    /// <returns>The C# type name.</returns>
    private static string RequestType(ScaffoldedUseCase plan, ScaffoldNames names, ScaffoldInput input)
    {
        if (input.RowFields is not null)
        {
            return ScaffoldNaming.FieldType("list", input.Required, names.Message + "." + RowName(input.Name));
        }

        return InputType(plan, names, input, names.Message);
    }

    /// <summary>Renders the endpoint response model from the record outputs.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <returns>The file text with LF line endings.</returns>
    private static string Response(ScaffoldedUseCase plan, ScaffoldNames names)
    {
        var fields = plan.Kind == "command"
            ? (IReadOnlyList<FieldShape>)(plan.CommandOutputs ?? [])
            : (plan.QueryGroups ?? []).SelectMany(group => group.Fields).ToList();
        var lines = new List<string>();

        if (fields.Any(field => field.RowFields is not null))
        {
            lines.Add($"using {names.App};");
            lines.Add("");
        }

        lines.Add($"namespace {names.Web};");
        lines.Add("");
        lines.Add($"/// <summary>{plan.Title} answer over HTTP.</summary>");

        if (plan.Kind == "command" && (plan.CommandOutputs is null || plan.CommandOutputs.Count == 0))
        {
            lines.Add($"/// <param name=\"{names.Aggregate}Id\">The identity of the {plan.AggregateName}.</param>");
            lines.Add($"/// <implements>{plan.UseCaseId}</implements>");
            lines.Add($"internal sealed record {plan.Stem}Response(Guid {names.Aggregate}Id);");
        }
        else
        {
            foreach (var field in fields)
            {
                lines.Add($"/// <param name=\"{ScaffoldNaming.PropertyName(field.Name)}\">{field.Meaning}</param>");
            }

            lines.Add($"/// <implements>{plan.UseCaseId}</implements>");
            lines.Add($"internal sealed record {plan.Stem}Response(");

            for (var index = 0; index < fields.Count; index++)
            {
                var comma = index == fields.Count - 1 ? ");" : ",";
                lines.Add($"    {ResponseType(plan, names, fields[index])} {ScaffoldNaming.PropertyName(fields[index].Name)}{comma}");
            }
        }

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The C# type of a response field, reusing the result rows.</summary>
    /// <param name="plan">The resolved use case scaffold.</param>
    /// <param name="names">The computed namespaces and type names.</param>
    /// <param name="field">The output value to type.</param>
    /// <returns>The C# type name.</returns>
    private static string ResponseType(ScaffoldedUseCase plan, ScaffoldNames names, FieldShape field)
    {
        if (field.RowFields is not null)
        {
            return $"IReadOnlyList<{names.Result}.{RowName(field.Name)}>";
        }

        if (field.Type == "list")
        {
            return ScaffoldNaming.FieldType("list", true, ScaffoldNaming.FieldType(field.ScalarItem ?? "string", true));
        }

        return ScaffoldNaming.FieldType(field.Type, true);
    }
}
