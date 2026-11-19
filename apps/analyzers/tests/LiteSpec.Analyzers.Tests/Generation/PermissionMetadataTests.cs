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
    /// <summary>The entry takes its scope from the role that grants it, and carries group and risks from the record.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    /// <proves>litespec/core/access#scope-every-role-to-its-one-reach</proves>
    [Fact(DisplayName = "The entry takes its scope from the role that grants it and carries group and risks")]
    public void EntryCarriesFactsFromTheRecord()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files(
                "xml",
                ("permissions.yaml", RecordPages.PermissionFactsPage(
                    RecordPages.PermissionEntry("permission/read-loans", "loans", ["money", "access"], ["role/reader"]))),
                ("reader.yaml", RecordPages.RolePage("role/reader", "tenant"))),
            assemblyName: "Shop.Spending");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("internal sealed record LiteSpecPermission(string Id, string Scope, string? Group, System.Collections.Generic.IReadOnlyList<string> Risks);", permissions, StringComparison.Ordinal);
        Assert.Contains("new LiteSpecPermission(\"permission/read-loans\", \"tenant\", \"loans\", new string[] { \"money\", \"access\" })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A permission that roles grant at more than one scope carries an empty scope, because the caller's role decides its reach.</summary>
    /// <proves>litespec/core/access#scope-every-role-to-its-one-reach</proves>
    [Fact(DisplayName = "A permission granted at more than one scope carries an empty scope")]
    public void PermissionGrantedAtSeveralScopesCarriesNoScope()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files(
                "xml",
                ("permissions.yaml", RecordPages.PermissionFactsPage(RecordPages.PermissionEntry("permission/list-expenses", grantedTo: ["role/employee", "role/approver"]))),
                ("self.yaml", RecordPages.RolePage("role/employee", "self")),
                ("team.yaml", RecordPages.RolePage("role/approver", "team"))),
            assemblyName: "Shop.Spending");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/list-expenses\", \"\", null, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>An entry without group and risks, and granted by no role, reads empty scope, null group and empty risks.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "An entry granted by no role reads an empty scope, null group and empty risks")]
    public void EntryWithoutFactsReadsDefaults()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/export-report")))),
            assemblyName: "Shop.Spending");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("new LiteSpecPermission(\"permission/export-report\", \"\", null, new string[] { })", permissions, StringComparison.Ordinal);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A permission opening with any verb is emitted and stays silent.</summary>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Fact(DisplayName = "A permission opening with any verb is emitted and stays silent")]
    public void AnyOpeningVerbIsEmittedAndStaysSilent()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files(
                "xml",
                ("permissions.yaml", RecordPages.PermissionFactsPage(
                    RecordPages.PermissionEntry("permission/frobnicate-widgets", grantedTo: ["role/tenant"]),
                    RecordPages.PermissionEntry("permission/approve-order", grantedTo: ["role/platform"]),
                    RecordPages.PermissionEntry("permission/manage-copies", grantedTo: ["role/tenant"]))),
                ("tenant.yaml", RecordPages.RolePage("role/tenant", "tenant")),
                ("platform.yaml", RecordPages.RolePage("role/platform", "platform"))),
            assemblyName: "Shop.Spending");

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
        var files = RecordPages.Files(
            "xml",
            ("permissions.yaml", RecordPages.PermissionFactsPage(
                RecordPages.PermissionEntry("permission/read-loans", "loans", ["money"], ["role/reader"]),
                RecordPages.PermissionEntry("permission/approve-order", grantedTo: ["role/platform"]))),
            ("reader.yaml", RecordPages.RolePage("role/reader", "tenant")),
            ("platform.yaml", RecordPages.RolePage("role/platform", "platform")));
        var run = GeneratorVerify.Run(sources, files, assemblyName: "Shop.Spending");
        Assert.True(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source is missing.");

        var compilation = GeneratorVerify.CreateCompilation(sources, "Shop.Spending");
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
