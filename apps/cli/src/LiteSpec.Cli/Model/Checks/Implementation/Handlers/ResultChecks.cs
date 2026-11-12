using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The result check: every result and refusal a handler, reaction or job produces is one its record declares.</summary>
/// <follows>litespec/backend/application#return-only-declared-results-and-refusals</follows>
internal static class ResultChecks
{
    /// <summary>Reports each refusal code a handler or host produces that its use cases leave undeclared.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var refusals = GenericRefusals(model);
        var declaring = DeclaringCodes(model);

        foreach (var handler in model.Code.Handlers)
        {
            if (!model.ById.TryGetValue(handler.UseCaseId, out var useCase) || useCase.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var declared = DeclaredCodes(model, useCase, refusals);

            foreach (var (code, file, line) in ProducedCodes(handler.Refusals, handler.Thrown, declaring))
            {
                if (!declared.Contains(code))
                {
                    findings.Add(new Finding(
                        "result-undeclared",
                        $"Use case '{handler.UseCaseId}' produces '{code}', which no result of the use case declares.",
                        file,
                        line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var host in HostRow.All(model))
        {
            if (host.RecordId is null
                || !model.ById.TryGetValue(host.RecordId, out var record)
                || record.Type is not (RecordTypes.Reaction or RecordTypes.Job))
            {
                continue;
            }

            var runs = RunsUseCases(model, record);

            if (runs.Count == 0)
            {
                continue;
            }

            var declared = new HashSet<string>(StringComparer.Ordinal);

            foreach (var useCase in runs)
            {
                declared.UnionWith(DeclaredCodes(model, useCase, refusals));
            }

            var label = record.Type == RecordTypes.Job ? "Job" : "Reaction";

            foreach (var (code, file, line) in ProducedCodes(host.Refusals, host.Thrown, declaring))
            {
                if (!declared.Contains(code))
                {
                    findings.Add(new Finding(
                        "result-undeclared",
                        $"{label} '{record.Id}' produces '{code}', which its runs use cases declare none of.",
                        file,
                        line,
                        record.Id));
                }
            }
        }
    }

    /// <summary>
    ///     Every refusal code a use case declares: its written refusal results plus the derived
    ///     generic codes, which a permission grant and a validated input each derive.
    /// </summary>
    /// <param name="model">The loaded project model that holds every result record.</param>
    /// <param name="useCase">The use case record whose declared refusal codes are collected.</param>
    /// <param name="refusals">The generic refusal codes a granted permission and a validated input derive.</param>
    /// <returns>The refusal codes the use case declares in writing or by derivation.</returns>
    private static HashSet<string> DeclaredCodes(
        ProjectModel model, Record useCase, (string Forbidden, string InvalidInput) refusals)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        if (Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            foreach (var result in model.RecordsByHome[name])
            {
                if (result.Type != RecordTypes.Result || result.Derived || result.Field(RecordFields.Outcome) != "refusal")
                {
                    continue;
                }

                if (result.Field(RecordFields.Code) is { } code)
                {
                    declared.Add(code);
                }
            }
        }

        if (ModelBuilder.GrantedPermission(useCase) is not null)
        {
            declared.Add(refusals.Forbidden);
        }

        if (ModelBuilder.HasValidatedInput(useCase))
        {
            declared.Add(refusals.InvalidInput);
        }

        return declared;
    }

    /// <summary>The generic refusal codes from .litespec/project.yaml refusals, or the defaults.</summary>
    /// <param name="model">The loaded project model that supplies the consumer root for the project file.</param>
    /// <returns>The forbidden and invalid-input codes the project declares, or the shared defaults.</returns>
    private static (string Forbidden, string InvalidInput) GenericRefusals(ProjectModel model)
    {
        const string forbidden = "authorization.forbidden";
        const string invalidInput = "request.validation-failed";

        try
        {
            var file = ConsumerRoot.ProjectFile(model.Config.Root);

            if (!File.Exists(file))
            {
                return (forbidden, invalidInput);
            }

            using var document = JsonDocument.Parse(File.ReadAllBytes(file));

            if (!document.RootElement.TryGetProperty("refusals", out var refusals)
                || refusals.ValueKind != JsonValueKind.Object)
            {
                return (forbidden, invalidInput);
            }

            return (
                refusals.TryGetProperty("forbidden", out var denied)
                    && denied.ValueKind == JsonValueKind.String
                    && denied.GetString() is { Length: > 0 } customForbidden
                        ? customForbidden
                        : forbidden,
                refusals.TryGetProperty("invalidInput", out var invalid)
                    && invalid.ValueKind == JsonValueKind.String
                    && invalid.GetString() is { Length: > 0 } customInvalid
                        ? customInvalid
                        : invalidInput);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return (forbidden, invalidInput);
        }
    }

    /// <summary>Refusal codes by the class whose declaring literal carries them.</summary>
    /// <param name="model">The loaded project model that holds each declaring code literal.</param>
    /// <returns>The refusal codes each declaring class carries, by class name.</returns>
    private static Dictionary<string, HashSet<string>> DeclaringCodes(ProjectModel model)
    {
        var declaring = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var literal in model.Code.Literals)
        {
            if (!literal.Declares || literal.Class is null)
            {
                continue;
            }

            if (!declaring.TryGetValue(literal.Class, out var codes))
            {
                codes = new HashSet<string>(StringComparer.Ordinal);
                declaring[literal.Class] = codes;
            }

            codes.Add(literal.Value);
        }

        return declaring;
    }

    /// <summary>
    ///     Every refusal code a class produces: its declaring literals plus the codes of the
    ///     exception and failure types it constructs, earliest site first.
    /// </summary>
    /// <param name="refusals">The refusal literals the class produces, or null when it produces none directly.</param>
    /// <param name="thrown">The exception and failure constructions of the class, or null when it constructs none.</param>
    /// <param name="declaring">The refusal codes each declaring class carries, by class name.</param>
    /// <returns>Each produced code with its earliest site, in line order.</returns>
    private static List<(string Code, string File, int Line)> ProducedCodes(
        IReadOnlyList<CodeRefusal>? refusals,
        IReadOnlyList<CodeThrown>? thrown,
        Dictionary<string, HashSet<string>> declaring)
    {
        var produced = new Dictionary<string, (string File, int Line)>(StringComparer.Ordinal);

        if (refusals is not null)
        {
            foreach (var refusal in refusals)
            {
                produced.TryAdd(refusal.Code, (refusal.File, refusal.Line));
            }
        }

        if (thrown is not null)
        {
            foreach (var construction in thrown)
            {
                if (!declaring.TryGetValue(construction.Type, out var codes))
                {
                    continue;
                }

                foreach (var code in codes.Order(StringComparer.Ordinal))
                {
                    produced.TryAdd(code, (construction.File, construction.Line));
                }
            }
        }

        return [.. produced
            .OrderBy(entry => entry.Value.Line)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => (entry.Key, entry.Value.File, entry.Value.Line))];
    }

    /// <summary>The use cases a reaction or job runs.</summary>
    /// <param name="model">The loaded project model that resolves each runs target.</param>
    /// <param name="record">The reaction or job record whose runs edges are read.</param>
    /// <returns>The use case records the holder runs.</returns>
    private static List<Record> RunsUseCases(ProjectModel model, Record record)
    {
        var found = new List<Record>();

        foreach (var id in record.EdgeIds(RecordFields.Runs))
        {
            if (model.ById.TryGetValue(id, out var useCase) && useCase.Type == RecordTypes.UseCase)
            {
                found.Add(useCase);
            }
        }

        return found;
    }
}
