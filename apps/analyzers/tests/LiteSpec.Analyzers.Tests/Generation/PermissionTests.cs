using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated permission constants.</summary>
public sealed class PermissionTests
{
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Application generates one constant per permission")]
    public void ApplicationGeneratesPermissionConstants()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("public const string CancelOrder = \"permission/cancel-order\";", permissions, StringComparison.Ordinal);
        Assert.Contains("public const string ViewLedger = \"permission/view-ledger\";", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Infrastructure generates permission constants from its assembly name")]
    public void InfrastructureGeneratesPermissionConstants()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/cancel-order"))),
            assemblyName: "Shop.Infrastructure");

        Assert.True(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source is missing.");
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "WebApi generates permission constants from the declared layer")]
    public void WebApiGeneratesPermissionConstantsFromTheDeclaredLayer()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Core; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/cancel-order"))),
            assemblyName: "Shop.Core",
            layer: "WebApi");

        Assert.True(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source is missing.");
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "Domain generates permission constants for its role types")]
    public void DomainGeneratesPermissionConstants()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/cancel-order"))),
            assemblyName: "Shop.Domain");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("public const string CancelOrder = \"permission/cancel-order\";", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "An unknown layer generates no permission constants")]
    public void UnknownLayerGeneratesNoPermissionConstants()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Core; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/cancel-order"))),
            assemblyName: "Shop.Core");

        Assert.False(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source must not be generated.");
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "A name starting with a digit takes a leading underscore")]
    public void DigitStartTakesALeadingUnderscore()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/3d-view"))),
            assemblyName: "Shop.Application");

        Assert.True(run.Sources.TryGetValue("LiteSpecPermissions.g.cs", out var permissions), "The permissions source is missing.");
        Assert.Contains("public const string _3dView = \"permission/3d-view\";", permissions, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact(DisplayName = "Two permissions producing one name report a diagnostic and emit nothing")]
    public void CollisionReportsAndEmitsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/a-b", "permission/a--b"))),
            assemblyName: "Shop.Application");

        var diagnostic = Assert.Single(run.ReportedDiagnostics);
        Assert.Equal("LS5009", diagnostic.Id);
        Assert.Contains("AB", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("permission/a-b", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("permission/a--b", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.False(run.Sources.ContainsKey("LiteSpecPermissions.g.cs"), "The permissions source must not be generated.");
        Assert.True(run.Sources.ContainsKey("LiteSpecRegistry.g.cs"), "The registry is independent of the collision and stays.");
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }
}
