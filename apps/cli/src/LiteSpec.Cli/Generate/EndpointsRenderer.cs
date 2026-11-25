using System.Text;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Cli.Generate;

/// <summary>
///     Renders the HTTP endpoints the records declare as C#: per module a partial static
///     class holding one nested endpoint class per use case, each mapping its route with
///     its method, binding each input from its source, sending the message through the
///     LiteBus mediator, and answering the success status with the refusal statuses.
///     A hand-written sibling file shapes each response the record projects and derives
///     each value the clock rule does not cover.
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
    /// <param name="Binding">Where the input travels: route, query, body, claim, header, or derived.</param>
    /// <param name="Required">Whether the record requires the input.</param>
    /// <param name="Meaning">The meaning the record states, or null when the record states none.</param>
    /// <param name="SetType">The closed-set type a choice input converts through, or null when the input names no set.</param>
    /// <param name="IdentifierType">The typed identifier an id input binds as, or null for a Guid.</param>
    /// <param name="Actor">The actor a claim input reads, or null when the input binds elsewhere.</param>
    /// <param name="Origin">The input origin: caller, verified-claim, or derived.</param>
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
        string Origin);

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
        MarkStyle Marks);

    /// <summary>
    ///     Renders one module's endpoints as C#: an internal static partial class with one
    ///     nested request record per use case binding body inputs, one nested endpoint class
    ///     per use case, and one partial shaping or derivation method per hand-written value.
    /// </summary>
    /// <param name="module">The record module name.</param>
    /// <param name="endpoints">The module's endpoints in use case order.</param>
    /// <param name="namespace">The C# namespace of the generated class.</param>
    /// <param name="claimReader">The qualified claim reader method, as Namespace.Type.Method.</param>
    /// <param name="usings">The C# namespaces the target declares.</param>
    /// <returns>The file text with LF line endings.</returns>
    public static string RenderDotnet(
        string module,
        IReadOnlyList<EndpointData> endpoints,
        string @namespace,
        string claimReader,
        IReadOnlyList<string> usings)
    {
        var ordered = endpoints.OrderBy(endpoint => endpoint.UseCaseId, StringComparer.Ordinal).ToList();
        var reader = ClaimReaderOf(claimReader);
        var lines = new List<string>
        {
            GenerateNaming.HeaderLineFor(GenerateNaming.CurrentVersion()),
            "// <auto-generated/>",
            "",
        };

        foreach (var heading in UsingLines(ordered, @namespace, reader.Namespace, usings))
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

            EndpointLines(lines, ordered[index], reader);
        }

        lines.Add("}");

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The qualified claim reader method split into the call and its namespace.</summary>
    /// <param name="Call">The type and method the endpoint calls, such as ActorClaims.ReadActorId.</param>
    /// <param name="Namespace">The C# namespace declaring the reader type.</param>
    private sealed record ClaimReader(string Call, string Namespace);

    /// <summary>Splits the qualified claim reader method the target names into its call and namespace.</summary>
    /// <param name="claimReader">The qualified method, as Namespace.Type.Method.</param>
    /// <returns>The type and method call with the declaring namespace.</returns>
    private static ClaimReader ClaimReaderOf(string claimReader)
    {
        var parts = claimReader.Split('.', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length >= 3
            ? new ClaimReader(parts[^2] + "." + parts[^1], string.Join(".", parts[..^2]))
            : new ClaimReader(claimReader, "");
    }

    /// <summary>The using lines the file opens with: the claims, mediator, message, response and reader namespaces with the target usings, ordered, without the file's own namespace or an enclosing one.</summary>
    /// <param name="endpoints">The module's endpoints in use case order.</param>
    /// <param name="namespace">The C# namespace of the generated class.</param>
    /// <param name="readerNamespace">The C# namespace declaring the claim reader type.</param>
    /// <param name="usings">The C# namespaces the target declares.</param>
    /// <returns>The using lines in ordinal order.</returns>
    private static List<string> UsingLines(
        IReadOnlyList<EndpointData> endpoints,
        string @namespace,
        string readerNamespace,
        IReadOnlyList<string> usings)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal)
        {
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Http.HttpResults",
            "Microsoft.AspNetCore.Routing",
        };

        if (endpoints.Any(endpoint => endpoint.Inputs.Any(input => input.Binding is "route" or "query" or "header")))
        {
            names.Add("Microsoft.AspNetCore.Mvc");
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

    /// <summary>Whether the endpoint reads the caller: a claim input or a derived value the sibling reads off the caller.</summary>
    /// <param name="endpoint">The endpoint whose inputs are read.</param>
    /// <returns>True when the handler takes the caller; otherwise false.</returns>
    private static bool UsesUser(EndpointData endpoint) =>
        endpoint.Inputs.Any(input => input.Binding == "claim" || (input.Origin == "derived" && input.Type != "instant"));

    /// <summary>Writes one endpoint: its request record when body inputs exist, its nested endpoint class, and its partial methods.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint the plan renders.</param>
    /// <param name="reader">The claim reader call.</param>
    private static void EndpointLines(List<string> lines, EndpointData endpoint, ClaimReader reader)
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
        HandleLines(lines, endpoint, reader);
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
            endpoint.Inputs.Where(input => input.Binding == "body").Select(input => $"{RequestTypeOf(input)} {input.Property}"));

        lines.Add($"    internal sealed partial record {endpoint.UseCaseName}Request({parameters});");
    }

    /// <summary>Writes the route mapping with the operation name, summary, success and failure metadata.</summary>
    /// <param name="lines">The file lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose call shape is read.</param>
    private static void MapLines(List<string> lines, EndpointData endpoint)
    {
        var map = MapMethodOf(endpoint.Method);

        var summary = endpoint.Summary is { Length: > 0 } stated ? FlatOf(stated) : endpoint.Title;

        lines.Add($"            endpoints.{map}(\"{GenerateNaming.EscapeLiteral(endpoint.Route)}\", HandleAsync)");
        lines.Add($"                .WithName(\"{GenerateNaming.EscapeLiteral(endpoint.UseCaseName)}\")");
        lines.Add($"                .WithSummary(\"{GenerateNaming.EscapeLiteral(summary)}\")");

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
    private static void HandleLines(List<string> lines, EndpointData endpoint, ClaimReader reader)
    {
        var parameters = new List<string>();
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
        }

        if (UsesUser(endpoint))
        {
            parameters.Add("ClaimsPrincipal user");
        }

        parameters.Add(endpoint.Kind == "query" ? "IQueryMediator mediator" : "ICommandMediator mediator");
        parameters.Add("CancellationToken cancellationToken");

        lines.Add($"        private static async Task<{ResultUnionOf(endpoint)}> HandleAsync(");
        lines.Add($"            {string.Join(",\n            ", parameters)})");
        lines.Add("        {");

        var body = new List<string>();

        GuardLines(body, endpoint);
        DispatchLines(body, endpoint, reader);
        ResponseLines(body, endpoint);

        foreach (var line in body)
        {
            lines.Add(line.Length > 0 ? "            " + line : line);
        }

        lines.Add("        }");
    }

    /// <summary>Writes the choice guards: an unlisted label answers 400 before the message runs. A derived choice needs no guard: its derivation answers the set member.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose choice inputs are read.</param>
    private static void GuardLines(List<string> body, EndpointData endpoint)
    {
        foreach (var input in endpoint.Inputs.Where(input => input is { Type: "choice", SetType: not null, Origin: not "derived" }))
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

    /// <summary>Writes the message construction and its dispatch through the mediator.</summary>
    /// <param name="body">The handler body lines, appended in place.</param>
    /// <param name="endpoint">The endpoint whose message is read.</param>
    /// <param name="reader">The claim reader call.</param>
    private static void DispatchLines(List<string> body, EndpointData endpoint, ClaimReader reader)
    {
        var call = endpoint.Kind == "query" ? "mediator.QueryAsync" : "mediator.SendAsync";
        var assign = endpoint.Projects ? "var result = await " : "await ";

        if (endpoint.Inputs.Count == 0)
        {
            body.Add($"{assign}{call}(new {endpoint.MessageType}(), cancellationToken);");
            return;
        }

        body.Add($"{assign}{call}(");
        body.Add($"    new {endpoint.MessageType}(");

        for (var index = 0; index < endpoint.Inputs.Count; index++)
        {
            var input = endpoint.Inputs[index];
            var last = index == endpoint.Inputs.Count - 1;

            body.Add($"        {input.Property}: {ArgumentOf(endpoint, input, reader)}{(last ? "" : ",")}");
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
            lines.Add($"    static partial {endpoint.ResponseModel} Shape{endpoint.UseCaseName}Response({endpoint.ResultType} result);");
        }

        foreach (var input in endpoint.Inputs.Where(input => input.Origin == "derived" && input.Type != "instant"))
        {
            if (lines.Count > 0)
            {
                lines.Add("");
            }

            lines.Add($"    /// <summary>Derives {input.Property} for {endpoint.Title}.</summary>");
            lines.Add($"    static partial {DeriveTypeOf(input)} Derive{endpoint.UseCaseName}{input.Property}(ClaimsPrincipal user);");
        }

        return lines;
    }

    /// <summary>The message argument one input renders: its bound value, its claim reading, or its derivation.</summary>
    /// <param name="endpoint">The endpoint whose use case names the derivation.</param>
    /// <param name="input">The input whose value is read.</param>
    /// <param name="reader">The claim reader call.</param>
    /// <returns>The argument expression.</returns>
    private static string ArgumentOf(EndpointData endpoint, BoundInput input, ClaimReader reader)
    {
        if (input.Origin == "derived")
        {
            return input.Type == "instant"
                ? "DateTimeOffset.UtcNow"
                : $"Derive{endpoint.UseCaseName}{input.Property}(user)";
        }

        if (input.Binding == "claim")
        {
            return $"{reader.Call}(user, \"{GenerateNaming.EscapeLiteral(input.Actor ?? input.Name)}\")";
        }

        if (input is { Type: "choice", SetType: not null })
        {
            return char.ToLowerInvariant(input.Property[0]) + input.Property[1..];
        }

        var source = input.Binding == "body" ? $"request.{input.Property}" : input.Local;

        if (input is { Type: "id", Binding: "body", IdentifierType: not null })
        {
            return Nullable(input)
                ? $"{source} is null ? null : {input.IdentifierType}.From({source}.Value)"
                : $"{input.IdentifierType}.From({source})";
        }

        return source;
    }

    /// <summary>Whether a bound value may be absent: an optional caller input, the way the message generator reads it. The pipeline supplies claim and derived inputs, so they never are.</summary>
    /// <param name="input">The input whose value is read.</param>
    /// <returns>True when the value may be absent; otherwise false.</returns>
    private static bool Nullable(BoundInput input) => !input.Required && input.Origin == "caller";

    /// <summary>The handler parameter type one input binds as: its wire type, with a typed identifier parsed from route, query, and header text.</summary>
    /// <param name="input">The input whose type is read.</param>
    /// <returns>The parameter type.</returns>
    private static string ParamTypeOf(BoundInput input)
    {
        var type = input.Type switch
        {
            "text" or "email" or "choice" => "string",
            "integer" => "int",
            "decimal" => "decimal",
            "money" => "decimal",
            "boolean" => "bool",
            "date" => "DateOnly",
            "instant" => "DateTimeOffset",
            "duration" => "TimeSpan",
            "id" => input.IdentifierType ?? "Guid",
            _ => "string",
        };

        return Nullable(input) ? type + "?" : type;
    }

    /// <summary>The request property type one body input binds as: its wire type, with an id converted through its identifier factory at construction.</summary>
    /// <param name="input">The input whose type is read.</param>
    /// <returns>The property type.</returns>
    private static string RequestTypeOf(BoundInput input)
    {
        var type = input.Type switch
        {
            "id" => "Guid",
            _ => ParamTypeOf(input).TrimEnd('?'),
        };

        return Nullable(input) ? type + "?" : type;
    }

    /// <summary>The return type of a derivation partial: the message property type the value answers.</summary>
    /// <param name="input">The derived input whose type is read.</param>
    /// <returns>The derivation return type.</returns>
    private static string DeriveTypeOf(BoundInput input)
    {
        var optional = Nullable(input) ? "?" : "";

        return input.Type switch
        {
            "text" or "email" => "string" + optional,
            "integer" => "int" + optional,
            "decimal" => "decimal" + optional,
            "money" => "decimal" + optional,
            "boolean" => "bool" + optional,
            "date" => "DateOnly" + optional,
            "duration" => "TimeSpan" + optional,
            "choice" => (input.SetType ?? "string") + optional,
            "id" => (input.IdentifierType ?? "Guid") + optional,
            _ => "string" + optional,
        };
    }

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

    /// <summary>The failure statuses the endpoint declares: 400 for a guarded choice with each cited refusal status, distinct and ascending.</summary>
    /// <param name="endpoint">The endpoint whose failures are read.</param>
    /// <returns>The declared statuses.</returns>
    private static List<int> FailureStatuses(EndpointData endpoint)
    {
        var statuses = new SortedSet<int>(endpoint.RefusalStatuses);

        if (endpoint.Inputs.Any(input => input is { Type: "choice", SetType: not null, Origin: not "derived" }))
        {
            statuses.Add(400);
        }

        return [.. statuses];
    }

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
