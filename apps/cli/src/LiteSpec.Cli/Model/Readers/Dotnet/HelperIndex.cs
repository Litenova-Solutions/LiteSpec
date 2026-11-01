using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     Every helper method the configured source roots declare, by namespace-qualified
///     <c>Type.Method</c>, each under its declaring type and under every interface it
///     implements.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class HelperIndex
{
    /// <summary>
    ///     Builds the helper index of one read: every helper method, so a call through a
    ///     held interface resolves to the method behind it. A method without parameters,
    ///     or one making no dispatch, is indexed as nothing to attribute.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="trees">The parsed syntax trees by file path.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    /// <returns>Every helper method by namespace-qualified <c>Type.Method</c>.</returns>
    public static Dictionary<string, List<HelperMethod>> Build(
        ConsumerConfig config, IReadOnlyDictionary<string, SyntaxTree> trees, QualifiedKinds qualified)
    {
        var files = new List<string>();

        foreach (var root in new[] { config.ApplicationRoot, config.ApplicationRoot + ".Abstractions", config.DomainRoot, config.WebApiRoot, config.InfrastructureRoot, config.WorkerRoot })
        {
            files.AddRange(SourceTree.Files(root, ".cs"));
        }

        var merged = new Dictionary<string, List<HelperMethod>>(StringComparer.Ordinal);
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => new List<(HelperMethod Helper, List<string> Aliases)>(),
            (file, _, local) =>
            {
                if (trees.TryGetValue(file, out var tree))
                {
                    CollectHelpers(file, tree, local, qualified);
                }

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    foreach (var (helper, aliases) in local)
                    {
                        foreach (var key in aliases.Prepend($"{helper.QualifiedType}.{helper.Method}"))
                        {
                            if (!merged.TryGetValue(key, out var overloads))
                            {
                                overloads = [];
                                merged[key] = overloads;
                            }

                            overloads.Add(helper);
                        }
                    }
                }
            });

        return merged;
    }

    /// <summary>Collects the dispatching helper methods one cached tree declares.</summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="into">The helpers collected so far, with their aliases.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    public static void CollectHelpers(
        string file, SyntaxTree tree, List<(HelperMethod Helper, List<string> Aliases)> into, QualifiedKinds qualified)
    {
        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (declared is not TypeDeclarationSyntax typed)
            {
                continue;
            }

            var interfaces = typed.BaseList is not null
                ? typed.BaseList.Types.Select(type => DotnetNames.BaseName(type.Type)).ToList()
                : [];
            var aliases = interfaces
                .Select(face => DotnetNames.KindOf(face))
                .Where(face => face.Length > 0)
                .Select(face => qualified.ResolveName(typed, face))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var method in typed.Members.OfType<MethodDeclarationSyntax>())
            {
                var parameters = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var parameter in method.ParameterList.Parameters)
                {
                    if (parameter.Type is not null && parameter.Identifier.ValueText.Length > 0)
                    {
                        parameters.TryAdd(parameter.Identifier.ValueText, DotnetNames.SimpleTypeName(parameter.Type));
                    }
                }

                if (parameters.Count == 0)
                {
                    continue;
                }

                var scope = (SyntaxNode?)method.Body ?? method.ExpressionBody;

                if (scope is null)
                {
                    continue;
                }

                var dispatches = DispatchReader.ScanDispatches(scope, parameters, tree, file, qualified);

                if (dispatches.Count == 0)
                {
                    continue;
                }

                into.Add((
                    new HelperMethod(
                        declared.Identifier.ValueText, method.Identifier.ValueText, parameters, dispatches,
                        DotnetNames.QualifiedNameOf(typed)),
                    aliases.Select(face => $"{face}.{method.Identifier.ValueText}").ToList()));
            }
        }
    }
}
