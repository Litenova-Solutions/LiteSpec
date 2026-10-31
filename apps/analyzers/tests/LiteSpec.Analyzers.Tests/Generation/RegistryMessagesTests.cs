using System.Globalization;
using System.Reflection;
using System.Text;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the registry's message-to-use-case answers.</summary>
public sealed class RegistryMessagesTests
{
    /// <summary>A command handler maps its message to its use case.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A command handler maps its message to its use case")]
    public void CommandHandlerMapsItsMessage()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed record SubmitExpenseCommand(string ExpenseId);

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[typeof(global::Shop.Spending.SubmitExpenseCommand)] = \"use-case/spending.submit-expense\",", registry, StringComparison.Ordinal);
        Assert.Contains("public static string? UseCaseOf(System.Type messageType) =>", registry, StringComparison.Ordinal);
        Assert.Contains("public static string? GrantOf(System.Type messageType) =>", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A query handler maps its message to its use case.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A query handler maps its message to its use case")]
    public void QueryHandlerMapsItsMessage()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface IQueryHandler<TQuery, TResult>;

                public sealed record ViewLedgerQuery(string LedgerId);

                /// <implements>use-case/spending.view-ledger</implements>
                internal sealed class ViewLedgerQueryHandler : IQueryHandler<ViewLedgerQuery, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("view-ledger.md", RecordPages.UseCasePage("use-case/spending.view-ledger", "permission/view-ledger"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[typeof(global::Shop.Spending.ViewLedgerQuery)] = \"use-case/spending.view-ledger\",", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>An unmarked handler adds no message entry.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "An unmarked handler adds no message entry")]
    public void UnmarkedHandlerAddsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed record SubmitExpenseCommand(string ExpenseId);

                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("new System.Collections.Generic.Dictionary<System.Type, string>();", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("SubmitExpenseCommand", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A handler with two use-case marks adds no message entry.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A handler with two use-case marks adds no message entry")]
    public void HandlerWithTwoUseCaseMarksAddsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed record SubmitExpenseCommand(string ExpenseId);

                /// <implements>use-case/spending.submit-expense</implements>
                /// <implements>use-case/spending.approve-expense</implements>
                internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("[\"Shop.Spending.SubmitExpenseCommandHandler\"] = new string[] { \"use-case/spending.submit-expense\", \"use-case/spending.approve-expense\" },", registry, StringComparison.Ordinal);
        Assert.Contains("new System.Collections.Generic.Dictionary<System.Type, string>();", registry, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>Two handlers of one message with different use cases report LS5011 and take no entry.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Two handlers of one message with different use cases report LS5011 and take no entry")]
    public void TwoHandlersOfOneMessageReportLS5011()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Spending;

                public interface ICommandHandler<TCommand, TResult>;

                public sealed record SubmitExpenseCommand(string ExpenseId);

                /// <implements>use-case/spending.submit-expense</implements>
                internal sealed class FirstCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
                {
                }

                /// <implements>use-case/spending.approve-expense</implements>
                internal sealed class SecondCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
                {
                }
                """),
            RecordPages.Files(
                "xml",
                ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense")),
                ("approve-expense.md", RecordPages.UseCasePage("use-case/spending.approve-expense", "permission/approve-expense"))));

        var diagnostic = Assert.Single(run.ReportedDiagnostics);
        Assert.Equal("LS5011", diagnostic.Id);
        Assert.Contains("global::Shop.Spending.SubmitExpenseCommand", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Shop.Spending.FirstCommandHandler", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Shop.Spending.SecondCommandHandler", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.True(run.Sources.TryGetValue("LiteSpecRegistry.g.cs", out var registry), "The registry source is missing.");
        Assert.Contains("new System.Collections.Generic.Dictionary<System.Type, string>();", registry, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>UseCaseOf and GrantOf answer the use case and its grant from the message.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "UseCaseOf and GrantOf answer the use case and its grant from the message")]
    public void MessageLookupsAnswerTheUseCaseAndItsGrant()
    {
        var sources = RecordPages.Sources("""
            namespace Shop.Spending;

            public interface ICommandHandler<TCommand, TResult>;

            public sealed record SubmitExpenseCommand(string ExpenseId);

            public sealed record RenameExpenseCommand(string ExpenseId);

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, object>
            {
            }

            /// <implements>use-case/spending.rename-expense</implements>
            internal sealed class RenameExpenseCommandHandler : ICommandHandler<RenameExpenseCommand, object>
            {
            }
            """);
        var files = RecordPages.Files(
            "xml",
            ("submit-expense.md", RecordPages.UseCasePage("use-case/spending.submit-expense", "permission/submit-expense")),
            ("rename-expense.md", RecordPages.UseCasePage("use-case/spending.rename-expense", null)));
        var run = GeneratorVerify.Run(sources, files, assemblyName: "Shop.Application");
        Assert.True(run.Sources.ContainsKey("LiteSpecRegistry.g.cs"), "The registry source is missing.");
        Assert.Empty(run.ReportedDiagnostics);

        var compilation = GeneratorVerify.CreateCompilation(sources, "Shop.Application");
        var trees = new List<SyntaxTree>();

        foreach (var generated in run.Sources)
        {
            trees.Add(CSharpSyntaxTree.ParseText(
                SourceText.From(generated.Value, Encoding.UTF8),
                new CSharpParseOptions(LanguageVersion.Latest),
                generated.Key,
                cancellationToken: TestContext.Current.CancellationToken));
        }

        using var stream = new MemoryStream();
        var emitted = compilation.AddSyntaxTrees(trees).Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, "The generated code does not emit:\n" + string.Join("\n", emitted.Diagnostics));

        var assembly = Assembly.Load(stream.ToArray());
        var registry = assembly.GetType("LiteSpec.LiteSpecRegistry");
        Assert.NotNull(registry);
        var useCaseOf = registry.GetMethod("UseCaseOf", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(useCaseOf);
        var grantOf = registry.GetMethod("GrantOf", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(grantOf);

        var submit = assembly.GetType("Shop.Spending.SubmitExpenseCommand");
        Assert.NotNull(submit);
        Assert.Equal("use-case/spending.submit-expense", useCaseOf.Invoke(null, [submit]));
        Assert.Equal("permission/submit-expense", grantOf.Invoke(null, [submit]));

        var rename = assembly.GetType("Shop.Spending.RenameExpenseCommand");
        Assert.NotNull(rename);
        Assert.Equal("use-case/spending.rename-expense", useCaseOf.Invoke(null, [rename]));
        Assert.Null(grantOf.Invoke(null, [rename]));

        Assert.Null(useCaseOf.Invoke(null, [typeof(string)]));
        Assert.Null(grantOf.Invoke(null, [typeof(string)]));
    }
}
