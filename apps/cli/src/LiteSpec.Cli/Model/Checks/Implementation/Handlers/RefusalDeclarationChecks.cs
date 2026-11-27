using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The refusal declaration check: every refusal code a handler, a root method it calls or a one-hop helper throws is one its use case declares in a result, and every code a module's code throws is one its refusal catalog declares, and conversely.</summary>
/// <follows>litespec/core/backend-application#return-only-declared-results-and-refusals</follows>
internal static class RefusalDeclarationChecks
{
    /// <summary>Reports each refusal code thrown for a use case that no result of the use case declares.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var source = new MessageFieldChecks.MessageSource(model);
        var declaring = DeclaringCodes(model);
        var roots = AggregateRoots(model);

        foreach (var handler in model.Code.Handlers)
        {
            if (!model.ById.TryGetValue(handler.UseCaseId, out var useCase) || useCase.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var declared = DeclaredCodes(model, useCase);
            var produced = new Dictionary<(string Code, string File, int Line), string?>();

            foreach (var (code, file, line) in ProducedCodes(handler.Refusals, handler.Thrown, declaring))
            {
                produced.TryAdd((code, file, line), null);
            }

            foreach (var (code, file, line, place) in CalledCodes(model, source, handler, declaring, roots))
            {
                produced.TryAdd((code, file, line), place);
            }

            foreach (var ((code, file, line), place) in produced
                .OrderBy(entry => entry.Key.Line)
                .ThenBy(entry => entry.Key.Code, StringComparer.Ordinal))
            {
                if (!declared.Contains(code))
                {
                    findings.Add(new Finding(
                        "refusal-undeclared",
                        place is null
                            ? $"Use case '{handler.UseCaseId}' throws '{code}', which no result of the use case declares."
                            : $"Use case '{handler.UseCaseId}' throws '{code}' {place}, which no result of the use case declares.",
                        file,
                        line,
                        handler.UseCaseId));
                }
            }
        }

        CheckCatalogDrift(model, findings);
    }

    /// <summary>
    ///     Reports each refusal code a module's code throws that the module's refusal catalog does not
    ///     declare, and each catalog entry the module's code never throws. Only literals in a declaring
    ///     position state a code of their own; a composed code reads as thrown through its key.
    /// </summary>
    /// <param name="model">The loaded project model that holds every refusal set and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCatalogDrift(ProjectModel model, List<Finding> findings)
    {
        if (model.Config.Layers.Implementation != LayerPractice.LiteSpec)
        {
            return;
        }

        var catalogued = model.RefusalSets
            .GroupBy(entry => entry.Module, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(entry => entry.Code, entry => entry, StringComparer.Ordinal),
                StringComparer.Ordinal);

        var thrown = ThrownCatalogCodes(model);

        foreach (var (code, folder, file, line) in thrown
            .OrderBy(entry => entry.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Line)
            .ThenBy(entry => entry.Code, StringComparer.Ordinal))
        {
            if (IsComposedThrown(model, code))
            {
                continue;
            }

            var module = catalogued.Keys.FirstOrDefault(key => SameName(folder, key));

            if (module is null || !catalogued[module].ContainsKey(code))
            {
                findings.Add(new Finding(
                    "refusal-catalog-drift",
                    $"Module '{folder}' throws refusal '{code}', which its refusal catalog does not declare.",
                    file,
                    line,
                    null));
            }
        }

        var thrownCodes = new HashSet<string>(thrown.Select(entry => entry.Code), StringComparer.Ordinal);

        foreach (var entry in model.RefusalSets
            .OrderBy(set => set.Module, StringComparer.Ordinal)
            .ThenBy(set => set.Code, StringComparer.Ordinal))
        {
            if (!thrownCodes.Contains(entry.Code) && !IsComposedThrown(model, entry.Code))
            {
                findings.Add(new Finding(
                    "refusal-catalog-drift",
                    $"Module '{entry.Module}' declares refusal '{entry.Code}', which its code never throws.",
                    entry.File,
                    entry.Line,
                    null));
            }
        }
    }

    /// <summary>Every dotted refusal code the module code states in a declaring position, with its module, file and line.</summary>
    /// <param name="model">The loaded project model that holds each declaring code literal.</param>
    /// <returns>Each thrown code with the module its file sits in.</returns>
    private static List<(string Code, string Module, string File, int Line)> ThrownCatalogCodes(ProjectModel model)
    {
        var thrown = new List<(string Code, string Module, string File, int Line)>();

        foreach (var literal in model.Code.Literals
            .Where(literal => literal.Declares)
            .OrderBy(literal => literal.File, StringComparer.Ordinal)
            .ThenBy(literal => literal.Line))
        {
            if (!literal.Value.Contains('.'))
            {
                continue;
            }

            if (SourceRoots.ModulePath(model.Config, literal.File) is not { } segments)
            {
                continue;
            }

            thrown.Add((literal.Value, segments[0], literal.File, literal.Line));
        }

        return thrown;
    }

    /// <summary>Whether a folder name and a module name spell one name: case, hyphens and underscores ignored.</summary>
    /// <param name="folder">The module folder name as written on disk.</param>
    /// <param name="name">The module name the refusal set declares.</param>
    /// <returns>True when both names agree with case, hyphens and underscores ignored; otherwise false.</returns>
    private static bool SameName(string folder, string name)
    {
        var left = folder.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var right = name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether a code reads as thrown through a composed refusal template whose key the code declares.</summary>
    /// <param name="model">The loaded project model that holds the composed templates and key literals.</param>
    /// <param name="code">The refusal code under test.</param>
    /// <returns>True when a composed template matches the code with a declared key; otherwise false.</returns>
    private static bool IsComposedThrown(ProjectModel model, string code) =>
        model.Config.RefusalComposed.Any(template =>
            RefusalTemplate.TryMatch(template, code, out var key) && model.Code.KeyLiterals.Contains(key));

    /// <summary>
    ///     Every refusal code a use case declares: its written refusal results plus the derived
    ///     generic codes, which read from the results the builder derived for the use case. A
    ///     derived result carrying a product refusal declares that code with its generic name.
    /// </summary>
    /// <param name="model">The loaded project model that holds every result record.</param>
    /// <param name="useCase">The use case record whose declared refusal codes are collected.</param>
    /// <returns>The refusal codes the use case declares in writing or by derivation.</returns>
    static HashSet<string> DeclaredCodes(ProjectModel model, Record useCase)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);

        if (Identifiers.TrySplit(useCase.Id, out _, out _, out var name))
        {
            foreach (var result in model.RecordsByHome[name])
            {
                if (result.Type != RecordTypes.Result || result.Field(RecordFields.Outcome) != "refusal")
                {
                    continue;
                }

                if (RefusalCode.Of(result) is { } code)
                {
                    declared.Add(code);
                }

                if (result.Derived && result.Field(RecordFields.Refusal) is not null
                    && Identifiers.TrySplit(result.Id, out _, out _, out var generic))
                {
                    declared.Add(generic);
                }
            }
        }

        return declared;
    }

    /// <summary>The simple names of the known aggregate roots: the read aggregate methods with the marked roots.</summary>
    /// <param name="model">The loaded project model that holds every aggregate method and marked element.</param>
    /// <returns>The simple aggregate root names.</returns>
    static HashSet<string> AggregateRoots(ProjectModel model)
    {
        var roots = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in model.Code.AggregateMethods)
        {
            roots.Add(method.Aggregate);
        }

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind == CodeKind.AggregateRoot && element.Class.Length > 0)
            {
                roots.Add(element.Class);
            }
        }

        return roots;
    }

    /// <summary>
    ///     Every refusal code the root methods and helpers a handler calls throw: each constructed
    ///     refusal type mapped through its declaring codes, with the declaring literals the called
    ///     method bodies carry. A root method reads as from its place, a helper as via its place.
    /// </summary>
    /// <param name="model">The loaded project model that holds every aggregate method and literal.</param>
    /// <param name="source">The backend source each called method is read from.</param>
    /// <param name="handler">The handler whose calls are followed one hop.</param>
    /// <param name="declaring">The refusal codes each declaring class carries, by class name.</param>
    /// <param name="roots">The simple names of the known aggregate roots.</param>
    /// <returns>Each thrown code with its site and its place.</returns>
    static List<(string Code, string File, int Line, string Place)> CalledCodes(
        ProjectModel model,
        MessageFieldChecks.MessageSource source,
        CodeHandler handler,
        Dictionary<string, HashSet<string>> declaring,
        HashSet<string> roots)
    {
        var produced = new List<(string Code, string File, int Line, string Place)>();

        if (handler.Class.Length == 0)
        {
            return produced;
        }

        var own = source.TypesIn(handler.File, handler.Class);

        if (own.Count == 0)
        {
            return produced;
        }

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, declared) in own)
        {
            foreach (var call in MethodCalls.OfClass(declared, roots))
            {
                if (!calls.Add(call.Type + "." + call.Method))
                {
                    continue;
                }

                var place = roots.Contains(call.Type)
                    ? $"from '{call.Type}.{call.Method}'"
                    : $"via '{call.Type}.{call.Method}'";

                produced.AddRange(MethodCodes(model, source, handler.File, call.Type, call.Method, declaring, place));
            }
        }

        return produced;
    }

    /// <summary>Every refusal code the named method throws, across its overloads: its constructed refusal types with its declaring literals.</summary>
    /// <param name="model">The loaded project model that holds every aggregate method and literal.</param>
    /// <param name="source">The backend source the method is read from.</param>
    /// <param name="hintFile">The handler file the called type is searched beside.</param>
    /// <param name="type">The simple name of the called type.</param>
    /// <param name="method">The called method name.</param>
    /// <param name="declaring">The refusal codes each declaring class carries, by class name.</param>
    /// <param name="place">The place the message names for the call.</param>
    /// <returns>Each thrown code with its site and its place.</returns>
    static List<(string Code, string File, int Line, string Place)> MethodCodes(
        ProjectModel model,
        MessageFieldChecks.MessageSource source,
        string hintFile,
        string type,
        string method,
        Dictionary<string, HashSet<string>> declaring,
        string place)
    {
        var produced = new List<(string Code, string File, int Line, string Place)>();
        var file = AggregateFile(model, type, method) ?? source.FileOf(type, hintFile);

        if (file is null)
        {
            return produced;
        }

        foreach (var (tree, declared) in source.TypesIn(file, type))
        {
            foreach (var candidate in declared.Members.OfType<MethodDeclarationSyntax>())
            {
                if (candidate.Identifier.ValueText != method)
                {
                    continue;
                }

                var span = tree.GetLineSpan(candidate.Span);

                foreach (var thrown in ThrownRefusals.OfMethod(candidate, tree))
                {
                    if (!declaring.TryGetValue(thrown.Name, out var codes))
                    {
                        continue;
                    }

                    foreach (var code in codes.Order(StringComparer.Ordinal))
                    {
                        produced.Add((code, file, thrown.Line, place));
                    }
                }

                foreach (var literal in model.Code.Literals)
                {
                    if (literal.Declares
                        && string.Equals(literal.File, file, StringComparison.Ordinal)
                        && literal.Line >= span.StartLinePosition.Line + 1
                        && literal.Line <= span.EndLinePosition.Line + 1)
                    {
                        produced.Add((literal.Value, literal.File, literal.Line, place));
                    }
                }
            }
        }

        return produced;
    }

    /// <summary>The file the aggregate root method was read from, or null when no read method matches.</summary>
    /// <param name="model">The loaded project model that holds every aggregate method.</param>
    /// <param name="type">The simple name of the called type.</param>
    /// <param name="method">The called method name.</param>
    /// <returns>The aggregate method file, or null.</returns>
    static string? AggregateFile(ProjectModel model, string type, string method)
    {
        foreach (var read in model.Code.AggregateMethods)
        {
            if (read.Aggregate == type && read.Method == method)
            {
                return read.File;
            }
        }

        return null;
    }

    /// <summary>Refusal codes by the class whose declaring literal carries them.</summary>
    /// <param name="model">The loaded project model that holds each declaring code literal.</param>
    /// <returns>The refusal codes each declaring class carries, by class name.</returns>
    static Dictionary<string, HashSet<string>> DeclaringCodes(ProjectModel model)
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
    static List<(string Code, string File, int Line)> ProducedCodes(
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
}
