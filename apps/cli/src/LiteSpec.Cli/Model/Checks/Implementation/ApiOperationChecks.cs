using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Ui;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The API operation checks: every published operation has a unique operation id, names the use case it runs, matches the use case's declared HTTP contract, and every declared MCP tool is mounted.</summary>
/// <follows>litespec/core/backend-api#name-the-use-case-on-every-published-operation</follows>
internal static class ApiOperationChecks
{
    /// <summary>Runs the API operation checks.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckOperationIdDuplicate(model, findings);
        CheckOperationUseCaseMismatch(model, findings);
        CheckMcpToolsMounted(model, findings);
    }

    /// <summary>Reports two endpoints declaring one operation id; the host refuses to start with both.</summary>
    /// <param name="model">The loaded project model that holds each scanned endpoint.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOperationIdDuplicate(ProjectModel model, List<Finding> findings)
    {
        var groups = model.Code.Endpoints
            .Where(endpoint => endpoint.Operation.Length > 0)
            .GroupBy(endpoint => endpoint.Operation, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var group in groups)
        {
            foreach (var endpoint in group.Skip(1))
            {
                findings.Add(new Finding(
                    "operation-id-duplicate",
                    $"Operation id '{group.Key}' on '{endpoint.Method} {endpoint.Route}' is declared by another endpoint too.",
                    endpoint.File,
                    endpoint.Line,
                    endpoint.UseCaseId));
            }
        }
    }

    /// <summary>The HTTP method keys a path item holds; other keys carry parameters, not operations.</summary>
    private static readonly HashSet<string> ContractMethods = new(StringComparer.Ordinal)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace",
    };

    /// <summary>Reports a published operation whose use-case extension is missing or names another use case.</summary>
    /// <param name="model">The loaded project model that holds each endpoint and the contract path.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOperationUseCaseMismatch(ProjectModel model, List<Finding> findings)
    {
        var contract = model.Config.ContractPath;

        if (string.IsNullOrEmpty(contract))
        {
            return;
        }

        var uses = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var endpoint in model.Code.Endpoints)
        {
            if (endpoint.Operation.Length == 0 || endpoint.UseCaseId.Length == 0)
            {
                continue;
            }

            if (!uses.TryGetValue(endpoint.Operation, out var set))
            {
                set = new SortedSet<string>(StringComparer.Ordinal);
                uses[endpoint.Operation] = set;
            }

            set.Add(endpoint.UseCaseId);
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(File.ReadAllBytes(contract));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            var detail = exception.Message.Trim().TrimEnd('.');

            findings.Add(new Finding(
                "operation-use-case-drift",
                $"Contract '{PathText.Relative(model.Config.Root, contract)}' is unreadable: {(detail.Length > 0 ? detail : "an unknown error")}.",
                contract,
                1,
                null));
            return;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding(
                    "operation-use-case-drift",
                    $"Contract '{PathText.Relative(model.Config.Root, contract)}' is unreadable: the document is not an object.",
                    contract,
                    1,
                    null));
                return;
            }

            if (!document.RootElement.TryGetProperty(RecordFields.Paths, out var paths))
            {
                return;
            }

            if (paths.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding(
                    "operation-use-case-drift",
                    $"Contract '{PathText.Relative(model.Config.Root, contract)}' is unreadable: the 'paths' value is not an object.",
                    contract,
                    1,
                    null));
                return;
            }

            var contractOperations = new Dictionary<string, List<(string Method, string Path)>>(StringComparer.Ordinal);

            foreach (var pathItem in paths.EnumerateObject())
            {
                if (pathItem.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var method in pathItem.Value.EnumerateObject())
                {
                    if (!ContractMethods.Contains(method.Name)
                        || method.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var extension = method.Value.TryGetProperty("x-litespec-use-case", out var carried)
                        && carried.ValueKind == JsonValueKind.String
                            ? carried.GetString()
                            : null;

                    if (extension is { Length: > 0 })
                    {
                        if (!contractOperations.TryGetValue(extension, out var entries))
                        {
                            entries = [];
                            contractOperations[extension] = entries;
                        }

                        entries.Add((method.Name, pathItem.Name));
                    }

                    if (!method.Value.TryGetProperty("operationId", out var named)
                        || named.ValueKind != JsonValueKind.String
                        || named.GetString() is not { Length: > 0 } operationId
                        || !uses.TryGetValue(operationId, out var useCases))
                    {
                        continue;
                    }

                    foreach (var useCase in useCases)
                    {
                        if (extension is null)
                        {
                            findings.Add(new Finding(
                                "operation-use-case-drift",
                                $"Operation '{operationId}' carries no x-litespec-use-case; its endpoint implements '{useCase}'.",
                                contract,
                                1,
                                useCase));
                        }
                        else if (!string.Equals(extension, useCase, StringComparison.Ordinal))
                        {
                            findings.Add(new Finding(
                                "operation-use-case-drift",
                                $"Operation '{operationId}' names '{extension}' in x-litespec-use-case, but its endpoint implements '{useCase}'.",
                                contract,
                                1,
                                useCase));
                        }
                    }
                }
            }

            CheckOpenApiUseCaseParity(model, contract, contractOperations, findings);
        }
    }

    /// <summary>Reports each use case whose declared http method or route differs from its published OpenAPI operation.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="contract">The absolute OpenAPI contract path.</param>
    /// <param name="contractOperations">The OpenAPI operations grouped by x-litespec-use-case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckOpenApiUseCaseParity(
        ProjectModel model,
        string contract,
        Dictionary<string, List<(string Method, string Path)>> contractOperations,
        List<Finding> findings)
    {
        foreach (var useCase in model.Records
            .Where(record => record.Type == RecordTypes.UseCase
                && record.Raw.ValueKind == JsonValueKind.Object
                && record.Raw.TryGetProperty(RecordFields.Http, out var http)
                && http.ValueKind == JsonValueKind.Object)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var http = useCase.Raw.GetProperty(RecordFields.Http);
            var declaredMethod = http.TryGetProperty(RecordFields.Method, out var methodNode) && methodNode.ValueKind == JsonValueKind.String
                ? methodNode.GetString()
                : null;
            var declaredRoute = http.TryGetProperty(RecordFields.Route, out var routeNode) && routeNode.ValueKind == JsonValueKind.String
                ? routeNode.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(declaredMethod) && string.IsNullOrWhiteSpace(declaredRoute))
            {
                continue;
            }

            if (!contractOperations.TryGetValue(useCase.Id, out var operations) || operations.Count == 0)
            {
                findings.Add(new Finding(
                    "openapi-contract-drift",
                    $"Use case '{useCase.Id}' declares HTTP '{declaredMethod} {declaredRoute}', which is missing from '{PathText.Relative(model.Config.Root, contract)}'.",
                    contract,
                    1,
                    useCase.Id));
                continue;
            }

            var expectedRoute = NormalizeRoute(declaredRoute);

            var matched = operations.Any(op =>
                (string.IsNullOrWhiteSpace(declaredMethod) || string.Equals(op.Method, declaredMethod, StringComparison.OrdinalIgnoreCase))
                && (expectedRoute.Length == 0 || string.Equals(NormalizeRoute(op.Path), expectedRoute, StringComparison.OrdinalIgnoreCase)));

            if (!matched)
            {
                var actual = string.Join(", ", operations.Select(op => $"{op.Method.ToUpperInvariant()} {op.Path}"));
                findings.Add(new Finding(
                    "openapi-contract-drift",
                    $"Use case '{useCase.Id}' declares HTTP '{declaredMethod} {declaredRoute}', but '{PathText.Relative(model.Config.Root, contract)}' publishes {actual}.",
                    contract,
                    1,
                    useCase.Id));
            }
        }
    }

    /// <summary>Normalizes a route template by stripping ASP.NET Core inline parameter constraints and trailing slashes.</summary>
    /// <param name="route">The route template.</param>
    /// <returns>The normalized route template.</returns>
    private static string NormalizeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "";
        }

        var stripped = RouteConstraintPattern().Replace(route.Trim(), "{$1}");
        return stripped.Length > 1 ? stripped.TrimEnd('/') : stripped;
    }

    /// <summary>Matches an inline route parameter constraint such as {id:guid}.</summary>
    /// <returns>The compiled route constraint regex.</returns>
    private static Regex RouteConstraintPattern() =>
        new(@"\{([A-Za-z0-9_]+):[^}]+\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Reports each use case declaring an MCP tool name when the ai-mcp extension is enabled and no backend code mounts that tool.</summary>
    /// <param name="model">The loaded project model that holds every record and backend root.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckMcpToolsMounted(ProjectModel model, List<Finding> findings)
    {
        if (!model.Config.Extensions.Contains("ai-mcp", StringComparer.Ordinal))
        {
            return;
        }

        var declared = new List<(Record UseCase, string Tool)>();

        foreach (var useCase in model.Records
            .Where(record => record.Type == RecordTypes.UseCase
                && record.Raw.ValueKind == JsonValueKind.Object
                && record.Raw.TryGetProperty(RecordFields.Mcp, out var mcp)
                && mcp.ValueKind == JsonValueKind.Object)
            .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var mcp = useCase.Raw.GetProperty(RecordFields.Mcp);

            if (mcp.TryGetProperty("tool", out var toolNode)
                && toolNode.ValueKind == JsonValueKind.String
                && toolNode.GetString() is { Length: > 0 } tool)
            {
                declared.Add((useCase, tool));
            }
        }

        if (declared.Count == 0)
        {
            return;
        }

        var backendRoots = model.Config.ModulesRoots
            .Concat(model.Config.WebApiRoots)
            .Concat(model.Config.WorkerRoots)
            .Append(model.Config.PlatformRoot)
            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (backendRoots.Count == 0)
        {
            return;
        }

        var files = backendRoots
            .SelectMany(root => FileWalk.Walk(root, file =>
                string.Equals(Path.GetExtension(file), ".cs", StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var contents = new List<string>(files.Count);

        foreach (var file in files)
        {
            try
            {
                var text = File.ReadAllText(file);
                if (text.Contains("Mcp", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("mcp", StringComparison.OrdinalIgnoreCase))
                {
                    contents.Add(text);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }

        foreach (var (useCase, tool) in declared)
        {
            var quoted = $"\"{tool}\"";
            var mounted = contents.Any(text => text.Contains(quoted, StringComparison.Ordinal));

            if (!mounted)
            {
                findings.Add(new Finding(
                    "mcp-tool-unmounted",
                    $"Use case '{useCase.Id}' declares MCP tool '{tool}', which no backend MCP endpoint mounts.",
                    useCase.File,
                    useCase.Line,
                    useCase.Id));
            }
        }
    }
}
