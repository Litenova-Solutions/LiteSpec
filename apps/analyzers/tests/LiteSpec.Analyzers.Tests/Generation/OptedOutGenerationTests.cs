using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests that an opted-out project gets no generated source and no generator diagnostic.</summary>
public sealed class OptedOutGenerationTests
{
    /// <summary>The modules project generates the full surface.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "The modules project generates the full surface")]
    public void ModulesProjectGeneratesTheFullSurface()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Claimwick.Modules; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Claimwick.Modules");

        Assert.NotEmpty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>The Kernel generates no registry, permissions or roles.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "The Kernel generates no registry, permissions or roles")]
    public void KernelGeneratesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Claimwick.Kernel.Domain; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Claimwick.Kernel");

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    /// <summary>A host beside a module project generates no registry, permissions or roles.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-the-registry-permissions-and-roles-from-the-records</proves>
    [Fact(DisplayName = "A host beside a module project generates no registry, permissions or roles")]
    public void ModuleHostGeneratesNoRegistry()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Claimwick.WebApi; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("permissions.yaml", RecordPages.PermissionPage("permission/cancel-order", "permission/view-ledger"))),
            assemblyName: "Claimwick.WebApi");

        Assert.DoesNotContain(run.Sources.Keys, name => name.Contains("Registry", StringComparison.Ordinal) || name.Contains("Permission", StringComparison.Ordinal) || name.Contains("Role", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }
}
