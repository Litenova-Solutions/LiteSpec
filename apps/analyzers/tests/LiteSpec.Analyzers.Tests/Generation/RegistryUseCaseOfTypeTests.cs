using System.Reflection;
using System.Text;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the registry's marked-type-to-use-case answer.</summary>
public sealed class RegistryUseCaseOfTypeTests
{
    /// <summary>UseCaseOfType answers the use case of a marked type and null for an unmarked type.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "UseCaseOfType answers the use case of a marked type and null for an unmarked type")]
    public void UseCaseOfTypeAnswersTheMarkedUseCase()
    {
        var sources = RecordPages.Sources("""
            namespace Shop.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed record SubmitExpenseCommand(string ExpenseId);

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
            {
            }

            internal sealed class UnmarkedHelper
            {
            }
            """);
        var files = RecordPages.Files(
            "xml",
            ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense")));
        var run = GeneratorVerify.Run(sources, files, assemblyName: "Shop.Application");
        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("public static string? UseCaseOfType(System.Type type)", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));

        var assembly = Emit(sources, run, "Shop.Application");
        var lookup = Lookup(assembly, "UseCaseOfType");

        var handler = assembly.GetType("Shop.Spending.SubmitExpenseCommandHandler");
        Assert.NotNull(handler);
        Assert.Equal("use-case/spending.submit-expense", lookup.Invoke(null, [handler]));

        var helper = assembly.GetType("Shop.Spending.UnmarkedHelper");
        Assert.NotNull(helper);
        Assert.Null(lookup.Invoke(null, [helper]));
    }

    /// <summary>UseCaseOfType normalizes the nested-type separator before the lookup.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "UseCaseOfType normalizes the nested-type separator before the lookup")]
    public void UseCaseOfTypeNormalizesNestedTypeNames()
    {
        var sources = RecordPages.Sources("""
            namespace Shop.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed record ApproveExpenseCommand(string ExpenseId);

            internal sealed class ExpenseHandlers
            {
                /// <implements>use-case/spending.approve-expense</implements>
                internal sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, object>
                {
                }
            }
            """);
        var files = RecordPages.Files(
            "xml",
            ("approve-expense.md", RecordPages.UseCasePage("use-case/spending.approve-expense", "permission/approve-expense")));
        var run = GeneratorVerify.Run(sources, files, assemblyName: "Shop.Application");
        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"Shop.Spending.ExpenseHandlers.ApproveExpenseCommandHandler\"] = new string[] { \"use-case/spending.approve-expense\" },", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));

        var assembly = Emit(sources, run, "Shop.Application");
        var lookup = Lookup(assembly, "UseCaseOfType");

        var handler = assembly.GetType("Shop.Spending.ExpenseHandlers+ApproveExpenseCommandHandler");
        Assert.NotNull(handler);
        Assert.Equal("use-case/spending.approve-expense", lookup.Invoke(null, [handler]));
    }

    private static Assembly Emit(
        IReadOnlyList<(string Path, string Source)> sources, GeneratorRun run, string assemblyName)
    {
        var compilation = GeneratorVerify.CreateCompilation(sources, assemblyName);
        var trees = new List<SyntaxTree>();

        foreach (var generated in run.Sources)
        {
            trees.Add(CSharpSyntaxTree.ParseText(
                SourceText.From(generated.Value, Encoding.UTF8),
                new CSharpParseOptions(LanguageVersion.Latest),
                generated.Key));
        }

        using var stream = new MemoryStream();
        var emitted = compilation.AddSyntaxTrees(trees).Emit(stream);
        Assert.True(emitted.Success, "The generated code does not emit:\n" + string.Join("\n", emitted.Diagnostics));

        return Assembly.Load(stream.ToArray());
    }

    private static MethodInfo Lookup(Assembly assembly, string name)
    {
        var registry = assembly.GetType("LiteSpec.LiteSpecRegistry");
        Assert.NotNull(registry);
        var lookup = registry.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(lookup);

        return lookup;
    }
}
