using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The API operation checks: every published operation has a unique operation id and names the use case it runs.</summary>
/// <follows>litespec/backend/api#name-the-use-case-on-every-published-operation</follows>
internal static class ApiOperationChecks
{
    /// <summary>Runs the API operation checks.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckOperationIdDuplicate(model, findings);
        CheckOperationUseCaseMismatch(model, findings);
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

            foreach (var pathItem in paths.EnumerateObject())
            {
                if (pathItem.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var method in pathItem.Value.EnumerateObject())
                {
                    if (!ContractMethods.Contains(method.Name)
                        || method.Value.ValueKind != JsonValueKind.Object
                        || !method.Value.TryGetProperty("operationId", out var named)
                        || named.ValueKind != JsonValueKind.String
                        || named.GetString() is not { Length: > 0 } operationId
                        || !uses.TryGetValue(operationId, out var useCases))
                    {
                        continue;
                    }

                    var extension = method.Value.TryGetProperty("x-litespec-use-case", out var carried)
                        && carried.ValueKind == JsonValueKind.String
                            ? carried.GetString()
                            : null;

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
        }
    }
}
