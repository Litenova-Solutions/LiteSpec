using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests that an opted-out project gets no generated source and no generator diagnostic.</summary>
public sealed class OptedOutGenerationTests
{
    /// <summary>An explicit None layer generates nothing and reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "An explicit None layer generates nothing and reports nothing")]
    public void ExplicitNoneGeneratesNothingAndReportsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Spending; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.md", RecordPages.PermissionPage("permission/a-b", "permission/a--b"))),
            assemblyName: "Shop.Domain",
            layer: "None");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>An unrecognised assembly name generates nothing and reports nothing.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "An unrecognised assembly name generates nothing and reports nothing")]
    public void UnrecognisedAssemblyGeneratesNothingAndReportsNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Core; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.md", RecordPages.PermissionPage("permission/a-b", "permission/a--b"))),
            assemblyName: "Shop.Core");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>An unknown declared layer generates nothing and reports nothing from the generator.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "An unknown declared layer generates nothing and reports nothing from the generator")]
    public void UnknownDeclaredLayerGeneratesNothingAndReportsNothingFromTheGenerator()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Core; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("permissions.md", RecordPages.PermissionPage("permission/cancel-order"))),
            assemblyName: "Shop.Core",
            layer: "Staging");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }
}
