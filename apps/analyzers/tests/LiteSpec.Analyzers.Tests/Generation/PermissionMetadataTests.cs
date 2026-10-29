using System.Reflection;
using System.Text;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated permission entries with their scope, group, reads and risks.</summary>
public sealed class PermissionMetadataTests
{
    [Fact(DisplayName = "The entry carries scope, group, reads and risks from the record")]
    public void EntryCarriesFactsFromTheRecord()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/read-loans", "tenant", "loans", ["money", "access"])))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("internal sealed record LiteSpecPermission(string Id, string Scope, string? Group, bool Reads, System.Collections.Generic.IReadOnlyList<string> Risks);", permissions, StringComparison.Ordinal);
        Assert.Contains("new LiteSpecPermission(\"permission/read-loans\", \"tenant\", \"loans\", true, new string[] { \"money\", \"access\" })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "An entry without scope, group and risks reads empty, null and empty")]
    public void EntryWithoutFactsReadsDefaults()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/export-report")))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/export-report\", \"\", null, true, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A changing verb reads false and stays silent")]
    public void ChangingVerbReadsFalse()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/approve-order", "platform")))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/approve-order\", \"platform\", null, false, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "An unknown first word reads false and reports LS5010")]
    public void UnknownFirstWordReportsLS5010()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/frobnicate-widgets", "tenant")))),
            assemblyName: "Shop.Application");

        var diagnostic = Assert.Single(run.ReportedDiagnostics);
        Assert.Equal("LS5010", diagnostic.Id);
        Assert.Contains("permission/frobnicate-widgets", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("frobnicate", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/frobnicate-widgets\", \"tenant\", null, false, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A manage permission changes and stays silent")]
    public void ManagePermissionChangesAndStaysSilent()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/manage-copies", "tenant")))),
            assemblyName: "Shop.Application");

        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/manage-copies\", \"tenant\", null, false, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A declared project verb reads false and stays silent")]
    public void DeclaredProjectVerbStaysSilent()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.FilesWithVocabulary(
                "{\"verbs\": [\"borrow\"]}",
                ("permissions.md", RecordPages.PermissionFactsPage(
                    RecordPages.PermissionEntry("permission/borrow-book", "tenant")))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/borrow-book\", \"tenant\", null, false, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A project query verb reads true and stays silent")]
    public void ProjectQueryVerbReadsTrue()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.FilesWithVocabulary(
                "{\"queryVerbs\": [\"inspect\"]}",
                ("permissions.md", RecordPages.PermissionFactsPage(
                    RecordPages.PermissionEntry("permission/inspect-vault", "platform")))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/inspect-vault\", \"platform\", null, true, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "Of returns the entry for a known id and throws for an unknown id")]
    public void OfReturnsKnownAndThrowsForUnknown()
    {
        var sources = RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;");
        var files = RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionFactsPage(
            RecordPages.PermissionEntry("permission/read-loans", "tenant", "loans", ["money"]),
            RecordPages.PermissionEntry("permission/approve-order", "platform"))));
        var run = GeneratorVerify.Run(sources, files, assemblyName: "Shop.Application");
        Assert.True(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source is missing.");

        var compilation = GeneratorVerify.CreateCompilation(sources, "Shop.Application");
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

        var permissions = Assembly.Load(stream.ToArray()).GetType("LiteSpec.LiteSpecPermissions");
        Assert.NotNull(permissions);
        var of = permissions.GetMethod("Of", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(of);

        var entry = of.Invoke(null, new object[] { "permission/read-loans" });
        Assert.NotNull(entry);
        Assert.Equal("permission/read-loans", Property(entry, "Id"));
        Assert.Equal("tenant", Property(entry, "Scope"));
        Assert.Equal("loans", Property(entry, "Group"));
        Assert.Equal(true, Property(entry, "Reads"));
        Assert.Equal(new[] { "money" }, ((System.Collections.IEnumerable)Property(entry, "Risks")!).Cast<string>().ToArray());

        var all = permissions.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(all);
        Assert.Equal(2, ((System.Collections.ICollection)all.GetValue(null)!).Count);

        var thrown = Assert.Throws<TargetInvocationException>(() => of.Invoke(null, new object[] { "permission/unknown" }));
        var argument = Assert.IsType<ArgumentException>(thrown.InnerException);
        Assert.Equal("id", argument.ParamName);
    }

    private static object? Property(object entry, string name)
    {
        var property = entry.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);

        return property.GetValue(entry);
    }
}
