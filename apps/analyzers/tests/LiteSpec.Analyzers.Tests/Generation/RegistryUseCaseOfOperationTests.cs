using System.Reflection;
using System.Text;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the registry's operation-name-to-use-case answer in WebApi projects.</summary>
public sealed class RegistryUseCaseOfOperationTests
{
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "A WebApi project maps each endpoint operation name to its use case")]
    public void WebApiMapsOperationNamesToUseCases()
    {
        var sources = RecordPages.Sources("""
            namespace Shop.Spending;

            public interface IEndpoint;

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseEndpoint : IEndpoint
            {
            }

            /// <implements>use-case/spending.export-expense-report</implements>
            internal sealed class ExportExpenseReportEndpoint : IEndpoint
            {
            }
            """);
        var files = RecordPages.Files(
            "xml",
            ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense")),
            ("export-expense-report.md", RecordPages.UseCasePage("use-case/spending.export-expense-report", null)));
        var run = GeneratorVerify.Run(sources, files, assemblyName: "Shop.WebApi");
        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"ExportExpenseReport\"] = \"use-case/spending.export-expense-report\",", registry, StringComparison.Ordinal);
        Assert.Contains("[\"SubmitExpense\"] = \"use-case/spending.submit-expense\",", registry, StringComparison.Ordinal);
        Assert.Contains("public static string? UseCaseOfOperation(string operationName) =>", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));

        var assembly = Emit(sources, run, "Shop.WebApi");
        var lookup = Lookup(assembly);

        Assert.Equal("use-case/spending.submit-expense", lookup.Invoke(null, new object[] { "SubmitExpense" }));
        Assert.Equal("use-case/spending.export-expense-report", lookup.Invoke(null, new object[] { "ExportExpenseReport" }));
        Assert.Null(lookup.Invoke(null, new object[] { "Unknown" }));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "A WebApi project without marked endpoints maps no operation")]
    public void WebApiWithoutMarkedEndpointsMapsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))),
            assemblyName: "Shop.WebApi");

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("private static readonly System.Collections.Generic.IReadOnlyDictionary<string, string> OperationUseCases = new System.Collections.Generic.Dictionary<string, string>();", registry, StringComparison.Ordinal);
        Assert.Contains("public static string? UseCaseOfOperation(string operationName) =>", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "A non-WebApi registry carries no operation lookup")]
    public void NonWebApiRegistryHasNoOperationLookup()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface IEndpoint;

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseEndpoint : IEndpoint
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"Shop.Spending.SubmitExpenseEndpoint\"] = new string[] { \"use-case/spending.submit-expense\" },", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("UseCaseOfOperation", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("OperationUseCases", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
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

    private static MethodInfo Lookup(Assembly assembly)
    {
        var registry = assembly.GetType("LiteSpec.LiteSpecRegistry");
        Assert.NotNull(registry);
        var lookup = registry.GetMethod("UseCaseOfOperation", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(lookup);

        return lookup;
    }
}
