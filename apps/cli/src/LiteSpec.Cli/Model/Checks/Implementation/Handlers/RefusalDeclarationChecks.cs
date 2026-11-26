using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The refusal declaration check: every refusal code a handler, a root method it calls or a one-hop helper throws is one its use case declares in a result.</summary>
/// <follows>litespec/backend/application#return-only-declared-results-and-refusals</follows>
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
    }

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
