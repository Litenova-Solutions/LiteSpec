using System.Reflection;
using System.Text;
using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated permission entries with their scope, group and risks.</summary>
public sealed class PermissionMetadataTests
{
    /// <summary>The entry carries scope, group and risks from the record.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "The entry carries scope, group and risks from the record")]
    public void EntryCarriesFactsFromTheRecord()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/read-loans", "tenant", "loans", ["money", "access"])))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("internal sealed record LiteSpecPermission(string Id, string Scope, string? Group, System.Collections.Generic.IReadOnlyList<string> Risks);", permissions, StringComparison.Ordinal);
        Assert.Contains("new LiteSpecPermission(\"permission/read-loans\", \"tenant\", \"loans\", new string[] { \"money\", \"access\" })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>An entry without scope, group and risks reads empty, null and empty.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "An entry without scope, group and risks reads empty, null and empty")]
    public void EntryWithoutFactsReadsDefaults()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/export-report")))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/export-report\", \"\", null, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A permission opening with any verb is emitted and stays silent.</summary>
    /// <proves>litespec/core/access#state-the-scope-of-every-permission</proves>
    [Fact(DisplayName = "A permission opening with any verb is emitted and stays silent")]
    public void AnyOpeningVerbIsEmittedAndStaysSilent()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/frobnicate-widgets", "tenant"),
                RecordPages.PermissionEntry("permission/approve-order", "platform"),
                RecordPages.PermissionEntry("permission/manage-copies", "tenant")))),
            assemblyName: "Shop.Application");

        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/frobnicate-widgets\", \"tenant\", null, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Contains("new LiteSpecPermission(\"permission/approve-order\", \"platform\", null, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Contains("new LiteSpecPermission(\"permission/manage-copies\", \"tenant\", null, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>Of returns the entry for a known id and throws for an unknown id.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Of returns the entry for a known id and throws for an unknown id")]
    public void OfReturnsKnownAndThrowsForUnknown()
    {
        var sources = RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;");
        var files = RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionFactsPage(
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
                generated.Key,
                cancellationToken: TestContext.Current.CancellationToken));
        }

        using var stream = new MemoryStream();
        var emitted = compilation.AddSyntaxTrees(trees).Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, "The generated code does not emit:\n" + string.Join("\n", emitted.Diagnostics));

        var permissions = Assembly.Load(stream.ToArray()).GetType("LiteSpec.LiteSpecPermissions");
        Assert.NotNull(permissions);
        var of = permissions.GetMethod("Of", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(of);

        var entry = of.Invoke(null, ["permission/read-loans"]);
        Assert.NotNull(entry);
        Assert.Equal("permission/read-loans", Property(entry, "Id"));
        Assert.Equal("tenant", Property(entry, "Scope"));
        Assert.Equal("loans", Property(entry, "Group"));
        Assert.Equal(new[] { "money" }, ((System.Collections.IEnumerable)Property(entry, "Risks")!).Cast<string>().ToArray());

        var all = permissions.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(all);
        Assert.Equal(2, ((System.Collections.ICollection)all.GetValue(null)!).Count);

        var thrown = Assert.Throws<TargetInvocationException>(() => of.Invoke(null, ["permission/unknown"]));
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
