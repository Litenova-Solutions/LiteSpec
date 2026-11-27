using System.Globalization;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated permission constants.</summary>
public sealed class PermissionTests
{
    /// <summary>A module generates one constant per permission.</summary>
    /// <proves>litespec/packs/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A module generates one constant per permission")]
    public void ModuleGeneratesPermissionConstants()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Shop.Spending");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("public const string CancelOrder = \"permission/cancel-order\";", permissions, StringComparison.Ordinal);
        Assert.Contains("public const string ViewLedger = \"permission/view-ledger\";", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A name starting with a digit takes a leading underscore.</summary>
    /// <proves>litespec/packs/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A name starting with a digit takes a leading underscore")]
    public void DigitStartTakesALeadingUnderscore()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionPage("permission/3d-view"))),
            assemblyName: "Shop.Spending");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("public const string _3dView = \"permission/3d-view\";", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>Two permissions producing one name report a diagnostic and emit nothing.</summary>
    /// <proves>litespec/packs/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Two permissions producing one name report a diagnostic and emit nothing")]
    public void CollisionReportsAndEmitsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionPage("permission/a-b", "permission/a--b"))),
            assemblyName: "Shop.Spending");

        var diagnostic = Assert.Single(run.ReportedDiagnostics);
        Assert.Equal("LS5009", diagnostic.Id);
        Assert.Contains("AB", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("permission/a-b", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("permission/a--b", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.False(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source must not be generated.");
        Assert.True(run.Sources.ContainsKey("LiteSpecRegistry.g.cs"), "The registry is independent of the collision and stays.");
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }
}
