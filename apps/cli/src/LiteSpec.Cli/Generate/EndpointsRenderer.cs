using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     Renders the HTTP endpoints the records declare as C#: per module a partial static
///     class holding one nested endpoint class per use case, each mapping its route with
///     its method, binding each input from its source with the paging from the query,
///     reading the tenant key from the caller when the use case implies one, sending the
///     message through the LiteBus mediator, and answering the success status with the
///     failure statuses the record implies. A hand-written sibling file shapes each
///     response the record projects and derives each value the clock rule does not cover.
/// </summary>
/// <follows>litespec/core/model#generate-record-determined-code-from-the-records</follows>
static class EndpointsRenderer
{
    /// <summary>One use-case input bound from its HTTP source.</summary>
    /// <param name="Name">The input name.</param>
    /// <param name="Property">The message property name.</param>
    /// <param name="Local">The handler local name in camelCase.</param>
    /// <param name="WireName">The route, query or header name the input travels under.</param>
    /// <param name="Type">The record type.</param>
    /// <param name="Binding">Where the input travels: route, query, body, form, claim, header, or derived.</param>
    /// <param name="Required">Whether the record requires the input.</param>
    /// <param name="Meaning">The meaning the record states, or null when the record states none.</param>
    /// <param name="SetType">The closed-set type a choice input converts through, or null when the input names no set.</param>
    /// <param name="IdentifierType">The typed identifier an id input binds as, or null for a Guid.</param>
    /// <param name="Actor">The actor a claim input reads, or null when the input binds elsewhere.</param>
    /// <param name="Origin">The input origin: caller, verified-claim, or derived.</param>
    /// <param name="Currency">The fixed ISO 4217 code a money input moves in, or null when the record states none.</param>
    /// <param name="CurrencyField">The sibling input carrying a money input's currency code, or null when the record states none.</param>
    /// <param name="Size">The stored width in bits of an integer input: 32, or 64 for values past the 32-bit span.</param>
    /// <param name="Many">Whether the input holds many values of its type, bound as an array.</param>
    /// <param name="CurrencyHome">The aggregate a money input takes its currency from, or null when the amount carries its own currency.</param>
    /// <param name="RowType">The message row type a list input maps each row to, or null for a single-field row without one.</param>
    /// <param name="Rows">The row fields of a list input in written order, or null when the input carries no list.</param>
    /// <param name="CurrencyFromOptional">Whether a money input takes its currency from an optional sibling, so the message carries the decimal amount and the handler joins the currency.</param>
    public sealed record BoundInput(
        string Name,
        string Property,
        string Local,
        string WireName,
        string Type,
        string Binding,
        bool Required,
        string? Meaning,
        string? SetType,
        string? IdentifierType,
        string? Actor,
        string Origin,
        string? Currency = null,
        string? CurrencyField = null,
        int Size = 32,
        bool Many = false,
        string? CurrencyHome = null,
        string? RowType = null,
        IReadOnlyList<RowField>? Rows = null,
        bool CurrencyFromOptional = false);

    /// <summary>One row field of a list input, bound from the request row record, or from one form part per row for a single file row.</summary>
    /// <param name="Name">The row field name.</param>
    /// <param name="Property">The message row property name.</param>
    /// <param name="Type">The record type.</param>
    /// <param name="Required">Whether the record requires the row field.</param>
    /// <param name="Meaning">The meaning the record states, or null when the record states none.</param>
    /// <param name="SetType">The closed-set type a choice row converts through, or null when the row names no set.</param>
    /// <param name="IdentifierType">The typed identifier an id row binds as, or null for a Guid.</param>
    /// <param name="Currency">The fixed ISO 4217 code a money row moves in, or null when the record states none.</param>
    /// <param name="CurrencyField">The sibling row field carrying a money row's currency code, or null when the record states none.</param>
    /// <param name="CurrencyHome">The aggregate a money row takes its currency from, or null when the amount carries its own currency.</param>
    /// <param name="Size">The stored width in bits of an integer row: 32, or 64 for values past the 32-bit span.</param>
    /// <param name="CurrencyFromOptional">Whether a money row takes its currency from an optional sibling row, so the message row carries the decimal amount and the handler joins the currency.</param>
    public sealed record RowField(
        string Name,
        string Property,
        string Type,
        bool Required,
        string? Meaning,
        string? SetType,
        string? IdentifierType,
        string? Currency,
        string? CurrencyField,
        string? CurrencyHome,
        int Size = 32,
        bool CurrencyFromOptional = false);

    /// <summary>One use-case endpoint the plan renders.</summary>
    /// <param name="UseCaseId">The use case identifier.</param>
    /// <param name="UseCaseName">The use case name in PascalCase, without its message suffix.</param>
    /// <param name="Title">The use case title.</param>
    /// <param name="Summary">The summary the record states, or null when the record states none.</param>
    /// <param name="Kind">The use case kind: command or query.</param>
    /// <param name="Method">The HTTP method: GET, POST, PUT, PATCH, or DELETE.</param>
    /// <param name="Route">The route template.</param>
    /// <param name="Created">The created-location template, or null when the record states none.</param>
    /// <param name="Inputs">The declared inputs in written order.</param>
    /// <param name="LocationProperties">The result property by location parameter.</param>
    /// <param name="Projects">Whether the response projects the result: a query, or a command stating a created location.</param>
    /// <param name="RefusalStatuses">The distinct refusal statuses the endpoint answers, ascending.</param>
    /// <param name="MessageType">The simple message type name, such as BorrowCopyCommand.</param>
    /// <param name="MessageNamespace">The C# namespace declaring the message type.</param>
    /// <param name="ResultType">The simple result type name, such as BorrowCopyCommandResult.</param>
    /// <param name="ResponseModel">The simple response model name, such as BorrowCopyResponseModel.</param>
    /// <param name="ResponseNamespace">The C# namespace declaring the response model.</param>
    /// <param name="Marks">The mark style the project declares.</param>
    /// <param name="Tenant">The tenant key the message carries first, or null when the use case implies none.</param>
    /// <param name="Paging">The paging the endpoint binds from the query, or null when the use case pages nothing.</param>
    /// <param name="RequiresAuth">Whether the use case grant requires a signed-in actor, so the endpoint lists authentication problems.</param>
    /// <param name="ResultNamespace">The C# namespace declaring the result type, or empty when the message namespace declares it.</param>
    public sealed record EndpointData(
        string UseCaseId,
        string UseCaseName,
        string Title,
        string? Summary,
        string Kind,
        string Method,
        string Route,
        string? Created,
        IReadOnlyList<BoundInput> Inputs,
        IReadOnlyDictionary<string, string> LocationProperties,
        bool Projects,
        IReadOnlyList<int> RefusalStatuses,
        string MessageType,
        string MessageNamespace,
        string ResultType,
        string ResponseModel,
        string ResponseNamespace,
        MarkStyle Marks,
        MessagesRenderer.TenantKey? Tenant = null,
        EndpointPaging? Paging = null,
        bool RequiresAuth = false,
        string ResultNamespace = "");

    /// <summary>The paging a collection endpoint binds from the query: the cursor or offset with the limit.</summary>
    /// <param name="Cursor">The cursor text the record states, or null when the query pages by offset.</param>
    /// <param name="Offset">The offset text the record states, or null when the query pages by cursor.</param>
    /// <param name="DefaultLimit">The rows per page the record states.</param>
    /// <param name="MaxLimit">The most rows per page the record allows.</param>
    public sealed record EndpointPaging(string? Cursor, string? Offset, int DefaultLimit, int MaxLimit);

    /// <summary>
    ///     Renders one module's endpoints as C#: an internal static partial class with one
    ///     nested request record per use case binding body inputs, one nested endpoint class
    ///     per use case, and one partial shaping or derivation method per hand-written value.
    /// </summary>
    /// <param name="module">The record module name.</param>
    /// <param name="endpoints">The module's endpoints in use case order.</param>
    /// <param name="namespace">The C# namespace of the generated class.</param>
    /// <param name="claimReader">The qualified generic claim reader method, as Namespace.Type.Method, answering the identity as its type argument.</param>
    /// <param name="usings">The C# namespaces the target declares.</param>
    /// <param name="tenantReader">The qualified generic tenant reader method, as Namespace.Type.Method, answering the tenant key as its type argument, or empty when the target names none.</param>
    /// <param name="claimValueReader">The qualified claim value reader method, as Namespace.Type.Method, or empty when the target names none.</param>
    /// <returns>The file text with LF line endings.</returns>
    public static string RenderDotnet(
        string module,
        IReadOnlyList<EndpointData> endpoints,
        string @namespace,
        string claimReader,
        IReadOnlyList<string> usings,
        string tenantReader = "",
        string claimValueReader = "")
    {
        var ordered = endpoints.OrderBy(endpoint => endpoint.UseCaseId, StringComparer.Ordinal).ToList();
        var reader = ReaderOf(claimReader);
        var tenant = ReaderOf(tenantReader);
        var values = ReaderOf(claimValueReader);
        var lines = new List<string>
        {
            GenerateNaming.HeaderLineFor(GenerateNaming.CurrentVersion()),
            "// <auto-generated/>",
            "",
        };

        foreach (var heading in UsingLines(ordered, @namespace, reader.Namespace, tenant.Namespace, values.Namespace, usings))
        {
            lines.Add(heading);
        }

        if (lines[^1].Length > 0)
        {
            lines.Add("");
        }

        var name = GenerateNaming.ModulePascal(module);

        lines.Add($"namespace {@namespace};");
        lines.Add("");
        lines.Add($"/// <summary>Maps every {module} HTTP endpoint from its use case record.</summary>");
        lines.Add($"internal static partial class {name}Endpoints");
        lines.Add("{");

        for (var index = 0; index < ordered.Count; index++)
        {
            if (index > 0)
            {
                lines.Add("");
            }

            EndpointLines(lines, ordered[index], reader, tenant, values);
        }

        lines.Add("}");

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The qualified reader method split into the call and its namespace.</summary>
    /// <param name="Call">The type and method the endpoint calls, such as ActorClaims.ReadActorId.</param>
    /// <param name="Namespace">The C# namespace declaring the reader type.</param>
    private sealed record Reader(string Call, string Namespace);

    /// <summary>Splits a qualified reader method the target names into its call and namespace.</summary>
    /// <param name="qualified">The qualified method, as Namespace.Type.Method.</param>
    /// <returns>The type and method call with the declaring namespace.</returns>
    private static Reader ReaderOf(string qualified)
    {
        var parts = qualified.Split('.', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length >= 3
            ? new Reader(parts[^2] + "." + parts[^1], string.Join(".", parts[..^2]))
            : new Reader(qualified, "");
    }

    /// <summary>The using lines the file opens with: the claims, mediator, message, result, response and reader namespaces with the target usings, ordered, without the file's own namespace or an enclosing one.</summary>
    /// <param name="endpoints">The module's endpoints in use case order.</param>
    /// <param name="namespace">The C# namespace of the generated class.</param>
    /// <param name="readerNamespace">The C# namespace declaring the claim reader type.</param>
    /// <param name="tenantNamespace">The C# namespace declaring the tenant reader type, or empty when the target names none.</param>
    /// <param name="valuesNamespace">The C# namespace declaring the claim value reader type, or empty when the target names none.</param>
    /// <param name="usings">The C# namespaces the target declares.</param>
    /// <returns>The using lines in ordinal order.</returns>
    private static List<string> UsingLines(
        IReadOnlyList<EndpointData> endpoints,
        string @namespace,
        string readerNamespace,
        string tenantNamespace,
        string valuesNamespace,
        IReadOnlyList<string> usings)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal)
        {
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Http.HttpResults",
            "Microsoft.AspNetCore.Routing",
        };

        if (endpoints.Any(endpoint => endpoint.Paging is not null
            || endpoint.Inputs.Any(input => input.Binding is "route" or "query" or "header")))
        {
            names.Add("Microsoft.AspNetCore.Mvc");
        }

        if (endpoints.Any(endpoint => endpoint.Inputs.Any(input =>
            input.Binding == "form" && (input.Type == "file" || SingleRowOf(input) is { Type: "file" }))))
        {
            names.Add("System.IO");
        }

        if (endpoints.Any(UsesUser))
        {
            names.Add("System.Security.Claims");
        }

        if (endpoints.Any(endpoint => endpoint.Kind == "command"))
        {
            names.Add("LiteBus.Commands.Abstractions");
        }

        if (endpoints.Any(endpoint => endpoint.Kind == "query"))
        {
            names.Add("LiteBus.Queries.Abstractions");
        }

        if (readerNamespace.Length > 0)
        {
            names.Add(readerNamespace);
        }

        if (tenantNamespace.Length > 0)
        {
            names.Add(tenantNamespace);
        }

        if (valuesNamespace.Length > 0)
        {
            names.Add(valuesNamespace);
        }

        foreach (var message in endpoints.Select(endpoint => endpoint.MessageNamespace).Distinct(StringComparer.Ordinal))
        {
            if (message.Length > 0)
            {
                names.Add(message);
            }
        }

        foreach (var response in endpoints
            .Where(endpoint => endpoint.Projects)
            .Select(endpoint => endpoint.ResponseNamespace)
            .Distinct(StringComparer.Ordinal))
        {
            if (response.Length > 0)
            {
                names.Add(response);
            }
        }

        foreach (var result in endpoints
            .Where(endpoint => endpoint.Projects)
            .Select(endpoint => endpoint.ResultNamespace)
            .Distinct(StringComparer.Ordinal))
        {
            if (result.Length > 0)
            {
                names.Add(result);
            }
        }

        foreach (var declared in usings)
        {
            if (declared.Length > 0)
            {
                names.Add(declared);
            }
        }

        names.RemoveWhere(candidate =>
            candidate == @namespace || @namespace.StartsWith(candidate + ".", StringComparison.Ordinal));

        return [.. names.Select(candidate => $"using {candidate};")];
    }

    /// <summary>Whether the endpoint reads the caller: a tenant key, a claim input, or a derived value the sibling reads off the caller.</summary>
    /// <param name="endpoint">The endpoint whose inputs are read.</param>
    /// <returns>True when the handler takes the caller; otherwise false.</returns>
    private static bool UsesUser(EndpointData endpoint) =>
        endpoint.Tenant is not null
        || endpoint.Inputs.Any(input => input.Binding == "claim" || (input.Origin == "derived" && (input.Type != "instant" || input.Many)));

    /// <summary>Writes one endpoint: its request record when body inputs exist, its nested endpoint class, and its partial methods.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint the plan renders.</param>
    /// <param name="reader">The claim reader call.</param>
    /// <param name="tenant">The tenant reader call.</param>
    /// <param name="values">The claim value reader call.</param>
    private static void EndpointLines(List<string> lines, EndpointData endpoint, Reader reader, Reader tenant, Reader values)
    {
        if (endpoint.Inputs.Any(input => input.Binding == "body"))
        {
            RequestLines(lines, endpoint);
            lines.Add("");
        }

        var type = endpoint.UseCaseName + "Endpoint";

        lines.AddRange(DocLines(endpoint.Summary, endpoint.Title, "    "));

        if (endpoint.Marks == MarkStyle.Attribute)
        {
            lines.Add($"    [Implements(\"{GenerateNaming.EscapeLiteral(endpoint.UseCaseId)}\")]");
        }
        else
        {
            lines.Add($"    /// <implements>{GenerateNaming.EscapeXml(endpoint.UseCaseId)}</implements>");
        }

        lines.Add($"    internal sealed partial class {type} : IEndpoint");
        lines.Add("    {");
        lines.Add("        /// <inheritdoc />");
        lines.Add("        public void MapEndpoint(IEndpointRouteBuilder endpoints)");
        lines.Add("        {");
        MapLines(lines, endpoint);
        lines.Add("        }");
        lines.Add("");
        HandleLines(lines, endpoint, reader, tenant, values);
        lines.Add("    }");

        var partials = PartialLines(endpoint);

        if (partials.Count > 0)
        {
            lines.Add("");
        }

        foreach (var partial in partials)
        {
            lines.Add(partial);
        }
    }

    /// <summary>Writes the request record carrying the endpoint's body inputs.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose body inputs are read.</param>
    private static void RequestLines(List<string> lines, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input =>
            input is { Binding: "body", Rows: not null }
            && (input.Type == "object" || (input.Type == "list" && NeedsRequestRow(input)))))
        {
            RequestRowLines(lines, endpoint, input);
            lines.Add("");
        }

        lines.Add($"    /// <summary>The {endpoint.UseCaseName} request body.</summary>");

        foreach (var input in endpoint.Inputs.Where(input => input.Binding == "body"))
        {
            if (input.Meaning is { Length: > 0 } meaning)
            {
                lines.Add($"    /// <param name=\"{input.Property}\">{GenerateNaming.EscapeXml(FlatOf(meaning))}</param>");
            }
        }

        var parameters = string.Join(
            ", ",
            endpoint.Inputs.Where(input => input.Binding == "body").Select(input => $"{RequestTypeOf(endpoint, input)} {input.Property}"));

        lines.Add($"    internal sealed partial record {endpoint.UseCaseName}Request({parameters});");
    }

    /// <summary>Writes the request row record carrying one list input's rows or one object input on the wire.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose list or object input is read.</param>
    /// <param name="input">The list or object input whose rows are read.</param>
    private static void RequestRowLines(List<string> lines, EndpointData endpoint, BoundInput input)
    {
        lines.Add($"    /// <summary>One row of {input.Property}.</summary>");

        foreach (var row in input.Rows!)
        {
            if (row.Meaning is { Length: > 0 } meaning)
            {
                lines.Add($"    /// <param name=\"{row.Property}\">{GenerateNaming.EscapeXml(FlatOf(meaning))}</param>");
            }
        }

        var parameters = string.Join(", ", input.Rows!.Select(row => $"{RowWireOf(row)} {row.Property}"));

        lines.Add($"    internal sealed partial record {RequestRowOf(endpoint, input)}({parameters});");
    }

    /// <summary>The request row record name of one list input: the use case and property with a Row suffix.</summary>
    /// <param name="endpoint">The endpoint whose use case names the row.</param>
    /// <param name="input">The list input whose property names the row.</param>
    /// <returns>The row record name.</returns>
    private static string RequestRowOf(EndpointData endpoint, BoundInput input) => endpoint.UseCaseName + input.Property + "Row";

    /// <summary>Whether a list input needs a request row record: a multi-field row, whose fields the message row type carries together.</summary>
    /// <param name="input">The list input whose rows are read.</param>
    /// <returns>True for a multi-field row; otherwise false.</returns>
    private static bool NeedsRequestRow(BoundInput input) => (input.Rows?.Count ?? 0) > 1;

    /// <summary>The request property type of one row field: its wire type, nullable when the row states no required.</summary>
    /// <param name="row">The row field whose type is read.</param>
    /// <returns>The property type.</returns>
    private static string RowWireOf(RowField row)
    {
        var type = row.Type == "id" ? "Guid" : ScalarWireOf(row.Type, row.Size);

        return row.Required ? type : type + "?";
    }

    /// <summary>Writes the route mapping with the operation name, summary, description, success and failure metadata.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose call shape is read.</param>
    private static void MapLines(List<string> lines, EndpointData endpoint)
    {
        var map = MapMethodOf(endpoint.Method);

        lines.Add($"            endpoints.{map}(\"{GenerateNaming.EscapeLiteral(endpoint.Route)}\", HandleAsync)");
        lines.Add($"                .WithName(\"{GenerateNaming.EscapeLiteral(OperationIdOf(endpoint.UseCaseName))}\")");
        lines.Add($"                .WithSummary(\"{GenerateNaming.EscapeLiteral(endpoint.Title)}\")");

        if (endpoint.Summary is { Length: > 0 } stated)
        {
            lines.Add($"                .WithDescription(\"{GenerateNaming.EscapeLiteral(FlatOf(stated))}\")");
        }

        if (endpoint is { Method: "PATCH" } && endpoint.Inputs.Any(input => input.Binding == "body"))
        {
            lines.Add($"                .Accepts<{endpoint.UseCaseName}Request>(\"application/merge-patch+json\")");
        }

        if (endpoint is { Projects: true, Created: { Length: > 0 } })
        {
            lines.Add($"                .Produces<{endpoint.ResponseModel}>(StatusCodes.Status201Created)");
        }
        else if (endpoint.Projects)
        {
            lines.Add($"                .Produces<{endpoint.ResponseModel}>(StatusCodes.Status200OK)");
        }
        else
        {
            lines.Add("                .Produces(StatusCodes.Status204NoContent)");
        }

        foreach (var status in FailureStatuses(endpoint))
        {
            lines.Add($"                .ProducesProblem(StatusCodes.{StatusConstOf(status)})");
        }

        lines[^1] += ";";
    }

    /// <summary>Writes the handler: its parameters, the choice guards, the message dispatch, and the response.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose inputs are read.</param>
    /// <param name="reader">The claim reader call.</param>
    /// <param name="tenant">The tenant reader call.</param>
    /// <param name="values">The claim value reader call.</param>
    private static void HandleLines(List<string> lines, EndpointData endpoint, Reader reader, Reader tenant, Reader values)
    {
        var parameters = new List<string>();
        var optional = new List<string>();
        var requested = false;

        foreach (var input in endpoint.Inputs)
        {
            if (input.Binding == "body")
            {
                if (!requested)
                {
                    parameters.Add($"{endpoint.UseCaseName}Request request");
                    requested = true;
                }

                continue;
            }

            if (input.Binding is "route" or "query" or "header")
            {
                parameters.Add($"{AttributeOf(input)} {ParamTypeOf(input)} {input.Local}");
            }

            if (input is { Binding: "form", Type: "list" })
            {
                if (Nullable(input))
                {
                    optional.Add($"IFormFileCollection? {input.Local} = null");
                }
                else
                {
                    parameters.Add($"IFormFileCollection {input.Local}");
                }
            }
            else if (input.Binding == "form")
            {
                if (Nullable(input))
                {
                    optional.Add($"IFormFile? {input.Local} = null");
                }
                else
                {
                    parameters.Add($"IFormFile {input.Local}");
                }
            }
        }

        if (endpoint.Paging is { } paging)
        {
            if (paging.Cursor is { Length: > 0 })
            {
                optional.Add("[FromQuery(Name = \"cursor\")] string? cursor = null");
            }

            if (paging.Offset is { Length: > 0 })
            {
                optional.Add("[FromQuery(Name = \"offset\")] int? offset = null");
            }

            optional.Add("[FromQuery(Name = \"limit\")] int? limit = null");
        }

        if (UsesUser(endpoint))
        {
            parameters.Add("ClaimsPrincipal user");
        }

        parameters.Add(endpoint.Kind == "query" ? "IQueryMediator mediator" : "ICommandMediator mediator");
        parameters.Add("CancellationToken cancellationToken");
        parameters.AddRange(optional);

        lines.Add($"        private static async Task<{ResultUnionOf(endpoint)}> HandleAsync(");
        lines.Add($"            {string.Join(",\n            ", parameters)})");
        lines.Add("        {");

        var body = new List<string>();

        GuardLines(body, endpoint);
        PagingLines(body, endpoint);
        FileLines(body, endpoint);
        FileListLines(body, endpoint);
        ConversionLines(body, endpoint);
        ObjectLines(body, endpoint);
        DispatchLines(body, endpoint, reader, tenant, values);
        ResponseLines(body, endpoint);

        foreach (var line in body)
        {
            lines.Add(line.Length > 0 ? "            " + line : line);
        }

        lines.Add("        }");
    }

    /// <summary>Writes the choice guards: an unlisted label answers 400 before the message runs. A derived choice needs no guard: its derivation answers the set member. A many-valued choice guards each label in the conversions.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose choice inputs are read.</param>
    private static void GuardLines(List<string> body, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input => input is { Type: "choice", SetType: not null, Origin: not "derived", Many: false }))
        {
            var source = input.Binding == "body" ? $"request.{input.Property}" : input.Local;
            var local = char.ToLowerInvariant(input.Property[0]) + input.Property[1..];
            var set = SpacedOf(input.SetType!);
            var name = SpacedOf(input.Name);

            if (!Nullable(input))
            {
                body.Add($"if (!{input.SetType}.TryFromCode({source}, out var {local}))");
                body.Add("{");
                body.Add("    return TypedResults.Problem(");
                body.Add($"        title: \"Unknown {set}.\",");
                body.Add($"        detail: $\"The {name} '{{{source}}}' names no {set}.\",");
                body.Add("        statusCode: StatusCodes.Status400BadRequest);");
                body.Add("}");
            }
            else
            {
                body.Add($"{input.SetType}? {local} = null;");
                body.Add($"if ({source} is not null)");
                body.Add("{");
                body.Add($"    if (!{input.SetType}.TryFromCode({source}, out var parsed))");
                body.Add("    {");
                body.Add("        return TypedResults.Problem(");
                body.Add($"            title: \"Unknown {set}.\",");
                body.Add($"            detail: $\"The {name} '{{{source}}}' names no {set}.\",");
                body.Add("            statusCode: StatusCodes.Status400BadRequest);");
                body.Add("    }");
                body.Add("");
                body.Add($"    {local} = parsed;");
                body.Add("}");
            }

            body.Add("");
        }
    }

    /// <summary>Writes the paging validation: the limit defaults to the declared default and answers 400 past the declared maximum or below one, and the offset defaults to zero and answers 400 when negative. The cursor passes through: the query decodes it.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose paging is read.</param>
    private static void PagingLines(List<string> body, EndpointData endpoint)
    {
        if (endpoint.Paging is not { } paging)
        {
            return;
        }

        body.Add($"var effectiveLimit = limit ?? {paging.DefaultLimit};");
        body.Add($"if (effectiveLimit < 1 || effectiveLimit > {paging.MaxLimit})");
        body.Add("{");
        body.Add("    return TypedResults.Problem(");
        body.Add("        title: \"Invalid limit.\",");
        body.Add($"        detail: $\"The limit '{{limit}}' is outside 1 to {paging.MaxLimit}.\",");
        body.Add("        statusCode: StatusCodes.Status400BadRequest);");
        body.Add("}");
        body.Add("");

        if (paging.Offset is { Length: > 0 })
        {
            body.Add("var effectiveOffset = offset ?? 0;");
            body.Add("if (effectiveOffset < 0)");
            body.Add("{");
            body.Add("    return TypedResults.Problem(");
            body.Add("        title: \"Invalid offset.\",");
            body.Add("        detail: $\"The offset '{offset}' is negative.\",");
            body.Add("        statusCode: StatusCodes.Status400BadRequest);");
            body.Add("}");
            body.Add("");
        }
    }

    /// <summary>Writes the upload mapping: each form file read into the bytes the message carries.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose file inputs are read.</param>
    private static void FileLines(List<string> body, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input => input is { Type: "file", Binding: "form" }))
        {
            var bytes = input.Local + "Bytes";
            var stream = input.Local + "Stream";

            if (!Nullable(input))
            {
                body.Add($"using var {stream} = new MemoryStream();");
                body.Add($"await {input.Local}.CopyToAsync({stream}, cancellationToken);");
                body.Add($"var {bytes} = {stream}.ToArray();");
            }
            else
            {
                body.Add($"byte[]? {bytes} = null;");
                body.Add($"if ({input.Local} is not null)");
                body.Add("{");
                body.Add($"    using var {stream} = new MemoryStream();");
                body.Add($"    await {input.Local}.CopyToAsync({stream}, cancellationToken);");
                body.Add($"    {bytes} = {stream}.ToArray();");
                body.Add("}");
            }

            body.Add("");
        }
    }

    /// <summary>Writes the upload list mapping: each form part read into the bytes of one row, one part per row.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose file lists are read.</param>
    private static void FileListLines(List<string> body, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input =>
            input is { Binding: "form", Type: "list" } && SingleRowOf(input) is { Type: "file" }))
        {
            var row = SingleRowOf(input)!;
            var bytes = input.Local + "Bytes";
            var stream = input.Local + "Stream";
            var element = input.RowType is { Length: > 0 } rowType ? rowType : "byte[]";
            var indent = Nullable(input) ? "    " : "";

            if (!Nullable(input))
            {
                body.Add($"var {bytes} = new List<{element}>({input.Local}.Count);");
            }
            else
            {
                body.Add($"List<{element}>? {bytes} = null;");
                body.Add($"if ({input.Local} is not null)");
                body.Add("{");
                body.Add($"    {bytes} = new List<{element}>({input.Local}.Count);");
            }

            body.Add($"{indent}foreach (var part in {input.Local})");
            body.Add($"{indent}{{");
            body.Add($"{indent}    using var {stream} = new MemoryStream();");
            body.Add($"{indent}    await part.CopyToAsync({stream}, cancellationToken);");

            body.Add(input.RowType is { Length: > 0 } named
                ? $"{indent}    {bytes}.Add(new {named}({row.Property}: {stream}.ToArray()));"
                : $"{indent}    {bytes}.Add({stream}.ToArray());");

            body.Add($"{indent}}}");

            if (Nullable(input))
            {
                body.Add("}");
            }

            body.Add("");
        }
    }

    /// <summary>Writes the collection conversions: each list row mapped to its message row type, and each many-valued choice label guarded through its closed set. An unlisted label answers 400 before the message runs, as a single choice does.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose collections are read.</param>
    private static void ConversionLines(List<string> body, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input => input is { Type: "list", Origin: not "derived" } && NeedsRequestRow(input)))
        {
            RowLoopLines(body, endpoint, input);
            body.Add("");
        }

        foreach (var input in endpoint.Inputs.Where(input =>
            input is { Origin: not "derived" }
            && (input is { Type: "choice", SetType: not null, Many: true }
                || SingleRowOf(input) is { Type: "choice", SetType: not null })))
        {
            ChoiceLoopLines(body, endpoint, input);
            body.Add("");
        }
    }

    /// <summary>Writes the row mapping of one multi-field list: each request row built into the message row type, with an unlisted choice label answering 400.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose list input is read.</param>
    /// <param name="input">The list input whose rows are read.</param>
    private static void RowLoopLines(List<string> body, EndpointData endpoint, BoundInput input)
    {
        var source = $"request.{input.Property}";
        var rows = input.Local + "Rows";
        var indent = Nullable(input) ? "    " : "";

        if (!Nullable(input))
        {
            body.Add($"var {rows} = new List<{input.RowType}>({source}.Length);");
        }
        else
        {
            body.Add($"List<{input.RowType}>? {rows} = null;");
            body.Add($"if ({source} is not null)");
            body.Add("{");
            body.Add($"    {rows} = new List<{input.RowType}>({source}.Length);");
        }

        body.Add($"{indent}foreach (var row in {source})");
        body.Add($"{indent}{{");

        foreach (var row in input.Rows!)
        {
            if (row is { Type: "choice", SetType: not null })
            {
                RowGuardLines(body, input, row, indent + "    ");
                body.Add("");
            }
        }

        body.Add($"{indent}    {rows}.Add(new {input.RowType}(");

        for (var index = 0; index < input.Rows!.Count; index++)
        {
            var row = input.Rows![index];
            var last = index == input.Rows!.Count - 1;

            body.Add($"{indent}        {row.Property}: {RowArgumentOf(input, row)}{(last ? "" : ",")}");
        }

        body.Add($"{indent}    ));");
        body.Add($"{indent}}}");

        if (Nullable(input))
        {
            body.Add("}");
        }
    }

    /// <summary>Writes the guard of one choice row field: an unlisted label answers 400 before the message runs.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="input">The list or object input whose row is read.</param>
    /// <param name="row">The choice row field whose labels are read.</param>
    /// <param name="indent">The indentation before each guard line.</param>
    /// <param name="sourcePrefix">The expression the row hangs off: the loop row for a list, the request object for a single object.</param>
    private static void RowGuardLines(List<string> body, BoundInput input, RowField row, string indent, string sourcePrefix = "row")
    {
        var local = input.Local + row.Property;
        var source = $"{sourcePrefix}.{row.Property}";
        var set = SpacedOf(row.SetType!);
        var name = SpacedOf(row.Name);

        if (row.Required)
        {
            body.Add($"{indent}if (!{row.SetType}.TryFromCode({source}, out var {local}))");
            body.Add($"{indent}{{");
            body.Add($"{indent}    return TypedResults.Problem(");
            body.Add($"{indent}        title: \"Unknown {set}.\",");
            body.Add($"{indent}        detail: $\"The {name} '{{{source}}}' names no {set}.\",");
            body.Add($"{indent}        statusCode: StatusCodes.Status400BadRequest);");
            body.Add($"{indent}}}");
        }
        else
        {
            body.Add($"{indent}{row.SetType}? {local} = null;");
            body.Add($"{indent}if ({source} is not null)");
            body.Add($"{indent}{{");
            body.Add($"{indent}    if (!{row.SetType}.TryFromCode({source}, out var {local}Parsed))");
            body.Add($"{indent}    {{");
            body.Add($"{indent}        return TypedResults.Problem(");
            body.Add($"{indent}            title: \"Unknown {set}.\",");
            body.Add($"{indent}            detail: $\"The {name} '{{{source}}}' names no {set}.\",");
            body.Add($"{indent}            statusCode: StatusCodes.Status400BadRequest);");
            body.Add($"{indent}    }}");
            body.Add("");
            body.Add($"{indent}    {local} = {local}Parsed;");
            body.Add($"{indent}}}");
        }
    }

    /// <summary>Writes the label conversion of one many-valued choice or single-field choice list: each label guarded through its closed set into the converted items.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose choice collection is read.</param>
    /// <param name="input">The choice collection whose labels are read.</param>
    private static void ChoiceLoopLines(List<string> body, EndpointData endpoint, BoundInput input)
    {
        var source = input.Binding == "body" ? $"request.{input.Property}" : input.Local;
        var items = input.Local + "Items";
        var code = input.Local + "Code";
        var member = input.Local + "Member";
        var row = SingleRowOf(input);
        var set = input.Type == "choice" ? input.SetType! : row!.SetType!;
        var element = input.Type == "choice" ? set : input.RowType ?? (row!.Required ? set : set + "?");
        var display = SpacedOf(set);
        var name = SpacedOf(row?.Name ?? input.Name);
        var indent = Nullable(input) ? "    " : "";

        if (!Nullable(input))
        {
            body.Add($"var {items} = new List<{element}>({source}.Length);");
        }
        else
        {
            body.Add($"List<{element}>? {items} = null;");
            body.Add($"if ({source} is not null)");
            body.Add("{");
            body.Add($"    {items} = new List<{element}>({source}.Length);");
        }

        body.Add($"{indent}foreach (var {code} in {source})");
        body.Add($"{indent}{{");

        if (row is null || row.Required)
        {
            body.Add($"{indent}    if (!{set}.TryFromCode({code}, out var {member}))");
            body.Add($"{indent}    {{");
            body.Add($"{indent}        return TypedResults.Problem(");
            body.Add($"{indent}            title: \"Unknown {display}.\",");
            body.Add($"{indent}            detail: $\"The {name} '{{{code}}}' names no {display}.\",");
            body.Add($"{indent}            statusCode: StatusCodes.Status400BadRequest);");
            body.Add($"{indent}    }}");
        }
        else
        {
            body.Add($"{indent}    {set}? {member} = null;");
            body.Add($"{indent}    if ({code} is not null)");
            body.Add($"{indent}    {{");
            body.Add($"{indent}        if (!{set}.TryFromCode({code}, out var {member}Parsed))");
            body.Add($"{indent}        {{");
            body.Add($"{indent}            return TypedResults.Problem(");
            body.Add($"{indent}                title: \"Unknown {display}.\",");
            body.Add($"{indent}                detail: $\"The {name} '{{{code}}}' names no {display}.\",");
            body.Add($"{indent}                statusCode: StatusCodes.Status400BadRequest);");
            body.Add($"{indent}        }}");
            body.Add("");
            body.Add($"{indent}        {member} = {member}Parsed;");
            body.Add($"{indent}    }}");
        }

        body.Add("");
        body.Add($"{indent}    {AddOf(input, items, member)}");
        body.Add($"{indent}}}");

        if (Nullable(input))
        {
            body.Add("}");
        }
    }

    /// <summary>The converted item one choice label appends: the set member, or the named row carrying it for a single-field row stating a row type.</summary>
    /// <param name="input">The choice collection whose labels are read.</param>
    /// <param name="items">The converted items local name.</param>
    /// <param name="member">The converted member local name.</param>
    /// <returns>The append expression.</returns>
    private static string AddOf(BoundInput input, string items, string member)
    {
        if (input.Type == "list" && input.RowType is { Length: > 0 } rowType)
        {
            return $"{items}.Add(new {rowType}({SingleRowOf(input)!.Property}: {member}));";
        }

        return $"{items}.Add({member});";
    }

    /// <summary>Writes the object conversions: each body object built from its request row into the message row type, with an unlisted choice label answering 400.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose objects are read.</param>
    private static void ObjectLines(List<string> body, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input =>
            input is { Type: "object", Binding: "body", Origin: not "derived", Rows: not null, RowType: not null }))
        {
            var source = $"request.{input.Property}";

            if (!Nullable(input))
            {
                foreach (var row in input.Rows!.Where(row => row is { Type: "choice", SetType: not null }))
                {
                    RowGuardLines(body, input, row, "", source);
                    body.Add("");
                }

                body.Add($"var {input.Local} = new {input.RowType}(");

                for (var index = 0; index < input.Rows!.Count; index++)
                {
                    var row = input.Rows![index];
                    var last = index == input.Rows!.Count - 1;

                    body.Add($"    {row.Property}: {RowArgumentOf(input, row, source)}{(last ? "" : ",")}");
                }

                body.Add(");");
            }
            else
            {
                body.Add($"{input.RowType}? {input.Local} = null;");
                body.Add($"if ({source} is not null)");
                body.Add("{");

                foreach (var row in input.Rows!.Where(row => row is { Type: "choice", SetType: not null }))
                {
                    RowGuardLines(body, input, row, "    ", source);
                    body.Add("");
                }

                body.Add($"    {input.Local} = new {input.RowType}(");

                for (var index = 0; index < input.Rows!.Count; index++)
                {
                    var row = input.Rows![index];
                    var last = index == input.Rows!.Count - 1;

                    body.Add($"        {row.Property}: {RowArgumentOf(input, row, source)}{(last ? "" : ",")}");
                }

                body.Add("    );");
                body.Add("}");
            }

            body.Add("");
        }
    }

    /// <summary>The single row of a single-field list input, or null for any other input.</summary>
    /// <param name="input">The input whose row is read.</param>
    /// <returns>The single row, or null.</returns>
    private static RowField? SingleRowOf(BoundInput input) =>
        input is { Type: "list", Rows: { Count: 1 } rows } ? rows[0] : null;

    /// <summary>Writes the message construction and its dispatch through the mediator: the tenant key first, then each input, then the paging members.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose message is read.</param>
    /// <param name="reader">The claim reader call.</param>
    /// <param name="tenant">The tenant reader call.</param>
    /// <param name="values">The claim value reader call.</param>
    private static void DispatchLines(List<string> body, EndpointData endpoint, Reader reader, Reader tenant, Reader values)
    {
        var call = endpoint.Kind == "query" ? "mediator.QueryAsync" : "mediator.SendAsync";
        var assign = endpoint.Projects ? "var result = await " : "await ";
        var arguments = new List<string>();

        if (endpoint.Tenant is { } tenantKey)
        {
            arguments.Add($"{tenantKey.Property}: {tenant.Call}<{tenantKey.Type}>(user)");
        }

        arguments.AddRange(endpoint.Inputs.Select(input => $"{input.Property}: {ArgumentOf(endpoint, input, reader, values)}"));

        if (endpoint.Paging is { } paging)
        {
            if (paging.Cursor is { Length: > 0 })
            {
                arguments.Add("Cursor: cursor");
            }

            if (paging.Offset is { Length: > 0 })
            {
                arguments.Add("Offset: effectiveOffset");
            }

            arguments.Add("Limit: effectiveLimit");
        }

        if (arguments.Count == 0)
        {
            body.Add($"{assign}{call}(new {endpoint.MessageType}(), cancellationToken);");
            return;
        }

        body.Add($"{assign}{call}(");
        body.Add($"    new {endpoint.MessageType}(");

        for (var index = 0; index < arguments.Count; index++)
        {
            body.Add($"        {arguments[index]}{(index == arguments.Count - 1 ? "" : ",")}");
        }

        body.Add("    ),");
        body.Add("    cancellationToken);");
    }

    /// <summary>Writes the success response: the shaped result with its status, or an empty answer.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose response is read.</param>
    private static void ResponseLines(List<string> body, EndpointData endpoint)
    {
        body.Add("");

        if (endpoint is { Projects: true, Created: { Length: > 0 } created })
        {
            body.Add("return TypedResults.Created(");
            body.Add($"    {LocationOf(created, endpoint.LocationProperties)},");
            body.Add($"    Shape{endpoint.UseCaseName}Response(result));");
        }
        else if (endpoint.Projects)
        {
            body.Add($"return TypedResults.Ok(Shape{endpoint.UseCaseName}Response(result));");
        }
        else
        {
            body.Add("return TypedResults.NoContent();");
        }
    }

    /// <summary>The partial methods the sibling implements: one response shaping per projected result, one derivation per value the clock rule does not cover.</summary>
    /// <param name="endpoint">The endpoint whose hand-written values are read.</param>
    /// <returns>The partial declaration lines.</returns>
    private static List<string> PartialLines(EndpointData endpoint)
    {
        var lines = new List<string>();

        if (endpoint.Projects)
        {
            lines.Add($"    /// <summary>Shapes the {endpoint.Title} response.</summary>");
            lines.Add($"    private static partial {endpoint.ResponseModel} Shape{endpoint.UseCaseName}Response({endpoint.ResultType} result);");
        }

        foreach (var input in endpoint.Inputs.Where(input => input.Origin == "derived" && (input.Type != "instant" || input.Many)))
        {
            if (lines.Count > 0)
            {
                lines.Add("");
            }

            lines.Add($"    /// <summary>Derives {input.Property} for {endpoint.Title}.</summary>");
            lines.Add($"    private static partial {DeriveTypeOf(input)} Derive{endpoint.UseCaseName}{input.Property}(ClaimsPrincipal user);");
        }

        return lines;
    }

    /// <summary>The message argument one input renders: its bound value, its claim reading, or its derivation.</summary>
    /// <param name="endpoint">The endpoint whose use case names the derivation.</param>
    /// <param name="input">The input whose value is read.</param>
    /// <param name="reader">The claim reader call.</param>
    /// <param name="values">The claim value reader call.</param>
    /// <returns>The argument expression.</returns>
    private static string ArgumentOf(EndpointData endpoint, BoundInput input, Reader reader, Reader values)
    {
        if (input.Origin == "derived")
        {
            return input is { Type: "instant", Many: false }
                ? "DateTimeOffset.UtcNow"
                : $"Derive{endpoint.UseCaseName}{input.Property}(user)";
        }

        if (input.Binding == "claim")
        {
            return input.Type == "id"
                ? $"{reader.Call}<{input.IdentifierType ?? "Guid"}>(user, \"{GenerateNaming.EscapeLiteral(input.Actor ?? input.Name)}\")"
                : $"{values.Call}<{ClaimTypeOf(input)}>(user, \"{GenerateNaming.EscapeLiteral(input.Actor ?? input.Name)}\")";
        }

        if (input.Type == "list" && NeedsRequestRow(input))
        {
            return input.Local + "Rows";
        }

        if (input is { Type: "object", Binding: "body", Origin: not "derived" })
        {
            return input.Local;
        }

        if (input is { Type: "choice", SetType: not null, Many: true } || SingleRowOf(input) is { Type: "choice", SetType: not null })
        {
            return input.Local + "Items";
        }

        if (input is { Type: "choice", SetType: not null, Many: false })
        {
            return char.ToLowerInvariant(input.Property[0]) + input.Property[1..];
        }

        if (input is { Type: "money", Many: false } && SingleRowOf(input) is null)
        {
            if (input.CurrencyHome is { Length: > 0 } || input.CurrencyFromOptional)
            {
                return input.Binding == "body" ? $"request.{input.Property}" : input.Local;
            }

            var amount = input.Binding == "body" ? $"request.{input.Property}" : input.Local;
            var currency = CurrencyOf(endpoint, input);

            return Nullable(input)
                ? $"{amount} is null ? null : Money.Create({amount}.Value, {currency})"
                : $"Money.Create({amount}, {currency})";
        }

        if (input is { Binding: "form", Type: "list" } && SingleRowOf(input) is { Type: "file" })
        {
            return input.Local + "Bytes";
        }

        if (input.Many || SingleRowOf(input) is not null)
        {
            return CollectionArgumentOf(endpoint, input);
        }

        var source = input.Binding == "body" ? $"request.{input.Property}" : input.Local;

        if (input is { Type: "id", Binding: "body", IdentifierType: not null, Many: false })
        {
            return Nullable(input)
                ? $"{source} is null ? null : {input.IdentifierType}.From({source}.Value)"
                : $"{input.IdentifierType}.From({source})";
        }

        if (input is { Type: "file", Binding: "form" })
        {
            return input.Local + "Bytes";
        }

        return source;
    }

    /// <summary>The message argument one many-valued input or single-field list renders: its array, with each element converted through its identifier factory or money object.</summary>
    /// <param name="endpoint">The endpoint whose inputs hold the currency sibling.</param>
    /// <param name="input">The collection whose values are read.</param>
    /// <returns>The argument expression.</returns>
    private static string CollectionArgumentOf(EndpointData endpoint, BoundInput input)
    {
        var source = input.Binding == "body" ? $"request.{input.Property}" : input.Local;

        if (ElementConvertOf(endpoint, input, SingleRowOf(input)) is not { } convert)
        {
            return source;
        }

        var mapped = $"Array.ConvertAll({source}, {convert})";

        return Nullable(input) ? $"{source} is null ? null : {mapped}" : mapped;
    }

    /// <summary>The element conversion of one many-valued input or single-field list: each wire element built into the message element, or null when the wire value rides as-is.</summary>
    /// <param name="endpoint">The endpoint whose inputs hold the currency sibling.</param>
    /// <param name="input">The collection whose values are read.</param>
    /// <param name="row">The single row of a single-field list, or null for a many-valued input.</param>
    /// <returns>The conversion lambda, or null.</returns>
    private static string? ElementConvertOf(EndpointData endpoint, BoundInput input, RowField? row)
    {
        var type = row?.Type ?? input.Type;

        string? Value(string target)
        {
            if (type == "id" && (row?.IdentifierType ?? (input.Binding == "body" ? input.IdentifierType : null)) is { } identifier)
            {
                return $"{identifier}.From({target})";
            }

            var joins = row is null
                ? input.CurrencyHome is { Length: > 0 } || input.CurrencyFromOptional
                : row.CurrencyHome is { Length: > 0 } || row.CurrencyFromOptional;

            if (type == "money" && !joins)
            {
                var currency = row is null ? CurrencyOf(endpoint, input) : RowCurrencyOf(input, row);

                return $"Money.Create({target}, {currency})";
            }

            return null;
        }

        if (row is null)
        {
            return Value("item") is { } converted ? $"item => {converted}" : null;
        }

        var converts = Value("item") is not null;
        var wraps = input.RowType is { Length: > 0 };

        if (!converts && !wraps)
        {
            return null;
        }

        var value = Value(row.Required ? "item" : "item.Value") ?? "item";

        if (!row.Required && converts)
        {
            value = $"item is null ? null : {value}";
        }

        if (input.RowType is { Length: > 0 } rowType)
        {
            value = $"new {rowType}({row.Property}: {value})";
        }

        return $"item => {value}";
    }

    /// <summary>The message row argument one row field renders: its converted wire value.</summary>
    /// <param name="input">The list or object input whose row is read.</param>
    /// <param name="row">The row field whose value is read.</param>
    /// <param name="sourcePrefix">The expression the row hangs off: the loop row for a list, the request object for a single object.</param>
    /// <returns>The argument expression.</returns>
    private static string RowArgumentOf(BoundInput input, RowField row, string sourcePrefix = "row")
    {
        var source = $"{sourcePrefix}.{row.Property}";

        if (row is { Type: "choice", SetType: not null })
        {
            return input.Local + row.Property;
        }

        if (row is { Type: "id", IdentifierType: not null })
        {
            return row.Required
                ? $"{row.IdentifierType}.From({source})"
                : $"{source} is null ? null : {row.IdentifierType}.From({source}.Value)";
        }

        if (row.Type == "money" && row.CurrencyHome is null && !row.CurrencyFromOptional)
        {
            var currency = RowCurrencyOf(input, row);

            return row.Required
                ? $"Money.Create({source}, {currency})"
                : $"{source} is null ? null : Money.Create({source}.Value, {currency})";
        }

        return source;
    }

    /// <summary>The currency a money row moves in: the fixed code, or the bound value of the sibling row field carrying the code.</summary>
    /// <param name="input">The list input whose rows hold the sibling.</param>
    /// <param name="row">The money row whose currency is read.</param>
    /// <returns>The currency expression.</returns>
    private static string RowCurrencyOf(BoundInput input, RowField row)
    {
        if (row.Currency is { Length: > 0 } fixedCode)
        {
            return $"\"{GenerateNaming.EscapeLiteral(fixedCode)}\"";
        }

        if (row.CurrencyField is { Length: > 0 } carrier)
        {
            var sibling = input.Rows?.FirstOrDefault(candidate => candidate.Name == carrier);

            return $"row.{sibling?.Property ?? GenerateNaming.ConstName(carrier)}";
        }

        return "\"\"";
    }

    /// <summary>The claim value type one non-identity claim input reads as: the scalar the value reader answers.</summary>
    /// <param name="input">The claim input whose type is read.</param>
    /// <returns>The value type.</returns>
    private static string ClaimTypeOf(BoundInput input) => input.Type switch
    {
        "text" or "email" => "string",
        "integer" => input.Size == 64 ? "long" : "int",
        "decimal" => "decimal",
        "boolean" => "bool",
        "date" => "DateOnly",
        "instant" => "DateTimeOffset",
        "duration" => "TimeSpan",
        _ => "string",
    };

    /// <summary>The currency a money input moves in: the fixed code, or the bound value of the sibling carrying the code.</summary>
    /// <param name="endpoint">The endpoint whose inputs hold the sibling.</param>
    /// <param name="input">The money input whose currency is read.</param>
    /// <returns>The currency expression.</returns>
    private static string CurrencyOf(EndpointData endpoint, BoundInput input)
    {
        if (input.Currency is { Length: > 0 } fixedCode)
        {
            return $"\"{GenerateNaming.EscapeLiteral(fixedCode)}\"";
        }

        var sibling = endpoint.Inputs.FirstOrDefault(candidate => candidate.Name == input.CurrencyField);

        if (sibling is null)
        {
            return "\"\"";
        }

        return sibling.Binding == "body" ? $"request.{sibling.Property}" : sibling.Local;
    }

    /// <summary>Whether a bound value may be absent: an input stating required false, whatever its origin, the way the message generator reads it.</summary>
    /// <param name="input">The input whose value is read.</param>
    /// <returns>True when the value may be absent; otherwise false.</returns>
    private static bool Nullable(BoundInput input) => !input.Required;

    /// <summary>The wire type of one scalar record type: the C# type the value travels as.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="size">The stored width in bits of an integer: 32, or 64 for values past the 32-bit span.</param>
    /// <returns>The wire type.</returns>
    private static string ScalarWireOf(string type, int size) => type switch
    {
        "text" or "email" or "choice" => "string",
        "integer" => size == 64 ? "long" : "int",
        "decimal" or "money" => "decimal",
        "boolean" => "bool",
        "date" => "DateOnly",
        "instant" => "DateTimeOffset",
        "duration" => "TimeSpan",
        _ => "string",
    };

    /// <summary>The handler parameter type one input binds as: its wire type, with a typed identifier parsed from route, query, and header text. A many-valued input binds as an array, which a repeated query parameter fills.</summary>
    /// <param name="input">The input whose type is read.</param>
    /// <returns>The parameter type.</returns>
    private static string ParamTypeOf(BoundInput input)
    {
        var type = input.Type == "id" ? input.IdentifierType ?? "Guid" : ScalarWireOf(input.Type, input.Size);

        if (input.Many)
        {
            return Nullable(input) ? type + "[]?" : type + "[]";
        }

        return Nullable(input) ? type + "?" : type;
    }

    /// <summary>The request property type one body input binds as: its wire type, with an id converted through its identifier factory at construction. A list binds as an array of its request rows, and a many-valued input as an array of its wire type.</summary>
    /// <param name="endpoint">The endpoint whose use case names the request row.</param>
    /// <param name="input">The input whose type is read.</param>
    /// <returns>The property type.</returns>
    private static string RequestTypeOf(EndpointData endpoint, BoundInput input)
    {
        if (input.Type == "list")
        {
            var row = NeedsRequestRow(input) ? RequestRowOf(endpoint, input) : RowWireOf(input.Rows![0]);

            return Nullable(input) ? row + "[]?" : row + "[]";
        }

        if (input.Type == "object")
        {
            var row = RequestRowOf(endpoint, input);

            return Nullable(input) ? row + "?" : row;
        }

        if (input.Many)
        {
            var element = input.Type == "id" ? "Guid" : ScalarWireOf(input.Type, input.Size);

            return Nullable(input) ? element + "[]?" : element + "[]";
        }

        var type = input.Type == "id" ? "Guid" : ScalarWireOf(input.Type, input.Size);

        return Nullable(input) ? type + "?" : type;
    }

    /// <summary>The return type of a derivation partial: the message property type the value answers.</summary>
    /// <param name="input">The derived input whose type is read.</param>
    /// <returns>The derivation return type.</returns>
    private static string DeriveTypeOf(BoundInput input)
    {
        if (input.Many)
        {
            return $"IReadOnlyList<{DeriveScalarOf(input.Type, input.Size, input.SetType, input.IdentifierType, input.CurrencyHome)}>";
        }

        if (input.Type == "list")
        {
            if (input.RowType is { Length: > 0 } rowType)
            {
                return $"IReadOnlyList<{rowType}>";
            }

            if (SingleRowOf(input) is { } row)
            {
                var scalar = DeriveScalarOf(row.Type, row.Size, row.SetType, row.IdentifierType, row.CurrencyHome);

                return $"IReadOnlyList<{scalar}{(row.Required ? "" : "?")}>";
            }

            return "IReadOnlyList<string>";
        }

        if (input.Type == "object" && input.RowType is { Length: > 0 } objectRow)
        {
            return objectRow + (Nullable(input) ? "?" : "");
        }

        var optional = Nullable(input) ? "?" : "";

        return DeriveScalarOf(input.Type, input.Size, input.SetType, input.IdentifierType, input.CurrencyHome) + optional;
    }

    /// <summary>The scalar a derivation partial answers for one record type: the message property type without nullability.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="size">The stored width in bits of an integer: 32, or 64 for values past the 32-bit span.</param>
    /// <param name="setType">The closed-set type a choice answers, or null when the field names no set.</param>
    /// <param name="identifierType">The typed identifier an id answers, or null for a Guid.</param>
    /// <param name="currencyHome">The aggregate a money field takes its currency from, or null when the amount carries its own currency.</param>
    /// <returns>The scalar type.</returns>
    private static string DeriveScalarOf(string type, int size, string? setType, string? identifierType, string? currencyHome) => type switch
    {
        "text" or "email" => "string",
        "integer" => size == 64 ? "long" : "int",
        "decimal" => "decimal",
        "money" => currencyHome is { Length: > 0 } ? "decimal" : "Money",
        "boolean" => "bool",
        "date" => "DateOnly",
        "instant" => "DateTimeOffset",
        "duration" => "TimeSpan",
        "file" => "byte[]",
        "choice" => setType ?? "string",
        "id" => identifierType ?? "Guid",
        _ => "string",
    };

    /// <summary>The binding attribute one input carries: its wire name from the route, query, or header.</summary>
    /// <param name="input">The input whose binding is read.</param>
    /// <returns>The attribute text.</returns>
    private static string AttributeOf(BoundInput input)
    {
        var attribute = input.Binding switch
        {
            "route" => "FromRoute",
            "header" => "FromHeader",
            _ => "FromQuery",
        };

        return $"[{attribute}(Name = \"{GenerateNaming.EscapeLiteral(input.WireName)}\")]";
    }

    /// <summary>The typed-results union the handler answers: the success shape with a problem when the endpoint declares a failure.</summary>
    /// <param name="endpoint">The endpoint whose response is read.</param>
    /// <returns>The union type text.</returns>
    private static string ResultUnionOf(EndpointData endpoint)
    {
        var success = endpoint switch
        {
            { Projects: true, Created: { Length: > 0 } } => $"Created<{endpoint.ResponseModel}>",
            { Projects: true } => $"Ok<{endpoint.ResponseModel}>",
            _ => "NoContent",
        };

        return FailureStatuses(endpoint).Count > 0 ? $"Results<{success}, ProblemHttpResult>" : success;
    }

    /// <summary>The failure statuses the endpoint declares, distinct and ascending: 400 for caller inputs or paging the endpoint validates, 401 and 403 for a grant requiring a signed-in actor, 404 for a route identifier loading an aggregate, with each cited refusal status.</summary>
    /// <param name="endpoint">The endpoint whose failures are read.</param>
    /// <returns>The declared statuses.</returns>
    private static List<int> FailureStatuses(EndpointData endpoint)
    {
        var statuses = new SortedSet<int>(endpoint.RefusalStatuses);

        if (endpoint.Paging is not null
            || endpoint.Inputs.Any(input => input.Origin == "caller")
            || endpoint.Inputs.Any(input => input is { Type: "choice", SetType: not null, Origin: not "derived" })
            || endpoint.Inputs.Any(input => input is { Type: "list", Origin: not "derived", Rows: not null }
                && input.Rows!.Any(row => row is { Type: "choice", SetType: not null }))
            || endpoint.Inputs.Any(input => input is { Type: "object", Origin: not "derived", Rows: not null }
                && input.Rows!.Any(row => row is { Type: "choice", SetType: not null })))
        {
            statuses.Add(400);
        }

        if (endpoint.RequiresAuth)
        {
            statuses.Add(401);
            statuses.Add(403);
        }

        if (endpoint.Inputs.Any(input => input is { Binding: "route", Type: "id", IdentifierType: not null }))
        {
            statuses.Add(404);
        }

        return [.. statuses];
    }

    /// <summary>The operation id of a use case: its PascalCase name in camelCase, the usual OpenAPI convention client generators derive method names from.</summary>
    /// <param name="useCaseName">The use case name in PascalCase, without its message suffix.</param>
    /// <returns>The operation id.</returns>
    private static string OperationIdOf(string useCaseName) =>
        useCaseName.Length == 0 ? useCaseName : char.ToLowerInvariant(useCaseName[0]) + useCaseName[1..];

    /// <summary>The minimal API mapping of an HTTP method.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <returns>The mapping method name.</returns>
    private static string MapMethodOf(string method) => method switch
    {
        "GET" => "MapGet",
        "POST" => "MapPost",
        "PUT" => "MapPut",
        "PATCH" => "MapPatch",
        "DELETE" => "MapDelete",
        _ => "Map" + method,
    };

    /// <summary>The StatusCodes constant name one status answers with.</summary>
    /// <param name="status">The HTTP status.</param>
    /// <returns>The constant name without its type.</returns>
    private static string StatusConstOf(int status) => status switch
    {
        400 => "Status400BadRequest",
        401 => "Status401Unauthorized",
        403 => "Status403Forbidden",
        404 => "Status404NotFound",
        409 => "Status409Conflict",
        422 => "Status422UnprocessableEntity",
        503 => "Status503ServiceUnavailable",
        _ => $"Status{status}",
    };

    /// <summary>The created-location expression: the template as an interpolated string over the result properties.</summary>
    /// <param name="created">The location template.</param>
    /// <param name="properties">The result property by location parameter.</param>
    /// <returns>The interpolation text.</returns>
    private static string LocationOf(string created, IReadOnlyDictionary<string, string> properties)
    {
        var body = new StringBuilder();
        var written = 0;

        foreach (Match match in Parameter.Matches(created))
        {
            body.Append(EscapeInterpolated(created[written..match.Index]));

            var name = match.Groups["name"].Value.Trim();

            body.Append("{result.");
            body.Append(properties.TryGetValue(name, out var property) ? property : GenerateNaming.ConstName(name));
            body.Append('}');

            written = match.Index + match.Length;
        }

        body.Append(EscapeInterpolated(created[written..]));

        return $"$\"{body}\"";
    }

    /// <summary>A location parameter in a template, written between braces.</summary>
    private static readonly Regex Parameter =
        new(@"\{(?<name>[^{}]+)\}", RegexOptions.CultureInvariant);

    /// <summary>Escapes literal text for an interpolated string body.</summary>
    /// <param name="text">The literal text.</param>
    /// <returns>The escaped body.</returns>
    private static string EscapeInterpolated(string text) =>
        GenerateNaming.EscapeLiteral(text)
            .Replace("{", "{{", StringComparison.Ordinal)
            .Replace("}", "}}", StringComparison.Ordinal);

    /// <summary>The spaced words of a kebab-case or camelCase name, for problem prose.</summary>
    /// <param name="name">The input or set name.</param>
    /// <returns>The name with its words spaced and lowered.</returns>
    private static string SpacedOf(string name)
    {
        var spaced = Regex.Replace(
            name.Replace('-', ' ').Replace('_', ' '),
            @"(?<=[a-z0-9])(?=[A-Z])",
            " ",
            RegexOptions.CultureInvariant);

        return spaced.ToLowerInvariant();
    }

    /// <summary>The documentation lines of the endpoint: its summary, or its title when the record states no summary.</summary>
    /// <param name="summary">The summary the record states, or null.</param>
    /// <param name="title">The use case title.</param>
    /// <param name="indent">The indentation before each documentation line.</param>
    /// <returns>The documentation lines.</returns>
    private static List<string> DocLines(string? summary, string title, string indent) =>
        summary is { Length: > 0 } stated
            ? GenerateNaming.XmlDocLines(FlatOf(stated), indent)
            : [$"{indent}/// <summary>{GenerateNaming.EscapeXml(title)}.</summary>"];

    /// <summary>One line of documentation text: newlines read as spaces.</summary>
    /// <param name="text">The documentation text.</param>
    /// <returns>The flattened text.</returns>
    private static string FlatOf(string text) =>
        text.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' ').Replace('\r', ' ');
}
