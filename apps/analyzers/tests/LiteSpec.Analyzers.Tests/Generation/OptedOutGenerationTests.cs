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
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/a-b", "permission/a--b"))),
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
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/a-b", "permission/a--b"))),
            assemblyName: "Shop.Core",
            layout: "layer-projects");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>The modules project generates the full surface.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "The modules project generates the full surface")]
    public void ApplicationProjectGeneratesTheFullSurface()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Claimwick.Modules; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Claimwick.Modules",
            layout: "modules");

        Assert.NotEmpty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>The shared kernel generates no registry, permissions or roles.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "The shared kernel generates no registry, permissions or roles")]
    public void SharedKernelGeneratesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Claimwick.SharedKernel; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Claimwick.SharedKernel",
            layout: "modules");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A host beside a module-layout application project generates no registry, permissions or roles.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "A host beside a module-layout application project generates no registry, permissions or roles")]
    public void ModuleHostGeneratesNoRegistry()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Claimwick.WebApi; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Claimwick.WebApi",
            layout: "modules");

        Assert.DoesNotContain(run.Sources.Keys, name => name.Contains("Registry", StringComparison.Ordinal) || name.Contains("Permission", StringComparison.Ordinal) || name.Contains("Role", StringComparison.Ordinal));
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
            RecordPages.Files("xml", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order"))),
            assemblyName: "Shop.Core",
            layer: "Staging");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }
}
