using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Tests.Ui;

/// <summary>
/// The source boundary, shared package, additional registry and source lock cases.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Source boundary (litespec/frontend/ui#select-one-visual-authority).

    /// <summary>Second Visual System In The Workspace Root.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void SecondVisualSystemInTheWorkspaceRoot()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteManifest("dependencies", new JsonObject { ["@mui/material"] = "7.0.0" });
        UiFixture.AssertProblem(
            fixture.Run(),
            "second general-purpose visual dependency '@mui/material' requires an exception");
    }

    /// <summary>Second Visual System In OptionalDependencies.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void SecondVisualSystemInOptionalDependencies()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteManifest("optionalDependencies", new JsonObject { ["bootstrap"] = "5.3.3" });
        UiFixture.AssertProblem(
            fixture.Run(),
            "second general-purpose visual dependency 'bootstrap' requires an exception");
    }

    /// <summary>Second Visual System Pinned Through Pnpm Overrides.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void SecondVisualSystemPinnedThroughPnpmOverrides()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteManifest("pnpm", new JsonObject
        {
            ["overrides"] = new JsonObject { ["@mantine/core"] = "8.0.0" },
        });
        UiFixture.AssertProblem(
            fixture.Run(),
            "second general-purpose visual dependency '@mantine/core' requires an exception");
    }

    /// <summary>Second Visual System Pinned Through Resolutions.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void SecondVisualSystemPinnedThroughResolutions()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteManifest("resolutions", new JsonObject { ["antd"] = "5.0.0" });
        UiFixture.AssertProblem(
            fixture.Run(),
            "second general-purpose visual dependency 'antd' requires an exception");
    }

    /// <summary>UI Exception With An Expired Date.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void UIExceptionWithAnExpiredDate()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteDeparture("ui-authority", "litespec/frontend/ui#select-one-visual-authority", "2020-01-01");
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/frontend/ui#select-one-visual-authority] expiresOn 2020-01-01 has passed; renew the departure or close the gap");
    }

    /// <summary>UI Exception With A Live Date.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void UIExceptionWithALiveDate()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteDeparture("ui-authority", "litespec/frontend/ui#select-one-visual-authority", "2099-01-01");
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>Exception Against Another Area Stays Unread By The UI Validator.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void ExceptionAgainstAnotherAreaStaysUnreadByTheUIValidator()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteDeparture("other-area", "litespec/core/agent#select-task-context", "2020-01-01");
        var problems = UiFixture.Problems(fixture.Run());

        // The model check owns the expiry of every exception, so it reports this
        // one; the UI validator must not read it. The UI expiry line carries the
        // provision in brackets, which the model line never does.
        Assert.DoesNotContain(
            problems,
            problem => problem.Contains("[litespec/core/agent#select-task-context]", StringComparison.Ordinal));
    }

    // Shared package (litespec/frontend/ui#pin-the-baseline-in-the-shared-package).

    /// <summary>A Controlled Frontend With No Shared Package.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void AControlledFrontendWithNoSharedPackage()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateProject(project => project.Remove("ui"));
        Assert.Contains("[project-invalid] missing required property 'ui'", fixture.Run().Output, StringComparison.Ordinal);
    }

    /// <summary>A Shared Package Declaring The Same Export Twice.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void ASharedPackageDeclaringTheSameExportTwice()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateProject(project =>
        {
            ((JsonObject)((JsonObject)project["ui"]!)["package"]!)["publicExports"] =
                new JsonArray("@fixture/ui/floorplans", "@fixture/ui/floorplans");
        });
        UiFixture.AssertProblem(fixture.Run(), "duplicate '@fixture/ui/floorplans'");
    }

    /// <summary>A Source Lock Outside The Package Root.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void ASourceLockOutsideThePackageRoot()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateProject(project =>
        {
            ((JsonObject)((JsonObject)project["ui"]!)["package"]!)["sourceLock"] = ".litespec/ui-source-lock.yaml";
        });
        UiFixture.AssertProblem(fixture.Run(), "sourceLock must live inside the shared package root");
    }

    /// <summary>Components Json Naming An Icon Library The Baseline Does Not Pin.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void ComponentsJsonNamingAnIconLibraryTheBaselineDoesNotPin()
    {
        using var fixture = UiFixture.Create();
        var components = (JsonObject)UiFixture.ReadJson(fixture.ComponentsFile)!;
        components["iconLibrary"] = "heroicons";
        UiFixture.WriteJson(fixture.ComponentsFile, components);
        UiFixture.AssertProblem(fixture.Run(), "iconLibrary must be \"lucide\"");
    }

    /// <summary>Source Lock Describing A Preset The Manifest Does Not Pin.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void SourceLockDescribingAPresetTheManifestDoesNotPin()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateLock(sourceLock => ((JsonObject)sourceLock["preset"]!)["style"] = "fixture-style");
        UiFixture.AssertProblem(fixture.Run(), "preset.style does not match the manifest baseline");
    }

    // Additional registries (litespec/frontend/ui#pin-the-baseline-in-the-shared-package).

    /// <summary>An Additional Registry With The Exception Passes Both Gates.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void AnAdditionalRegistryWithTheExceptionPassesBothGates()
    {
        using var fixture = UiFixture.Create();
        fixture.AddAdditionalRegistry();
        fixture.WriteDeparture("extra-registry", "litespec/frontend/ui#pin-the-baseline-in-the-shared-package", "2099-01-01");
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>The Configuration Gate Refuses Without The Exception.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void TheConfigurationGateRefusesWithoutTheException()
    {
        using var fixture = UiFixture.Create();
        fixture.AddAdditionalRegistry();
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/frontend/ui#pin-the-baseline-in-the-shared-package] an additional registry requires an exception");
    }

    /// <summary>The Source Lock Gate Refuses Without The Exception.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void TheSourceLockGateRefusesWithoutTheException()
    {
        using var fixture = UiFixture.Create();
        fixture.AddAdditionalRegistry();
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/frontend/ui#pin-the-baseline-in-the-shared-package] registry 'fixture' is not the built-in shadcn registry");
    }

    // Source lock (litespec/frontend/web/ui#track-source-changes).

    /// <summary>Changed Copied Source Without A Fork Record.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void ChangedCopiedSourceWithoutAForkRecord()
    {
        using var fixture = UiFixture.Create();
        File.AppendAllText(fixture.ButtonFile, "// local change\n");
        UiFixture.AssertProblem(fixture.Run(), "no longer matches the recorded digest");
    }

    /// <summary>A Dependency The Manifest Does Not Pin.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void ADependencyTheManifestDoesNotPin()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateLock(sourceLock =>
        {
            ((JsonObject)((JsonArray)sourceLock["components"]!)[0]!)["dependencies"] =
                new JsonArray("not-a-pinned-package");
        });
        UiFixture.AssertProblem(
            fixture.Run(),
            "dependency 'not-a-pinned-package' is not pinned in the LiteSpec manifest");
    }

    /// <summary>A Path Escaping The Package Root.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void APathEscapingThePackageRoot()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateLock(sourceLock =>
        {
            ((JsonObject)((JsonArray)sourceLock["components"]!)[0]!)["paths"] =
                new JsonArray("../../../outside/button.tsx");
        });
        UiFixture.AssertProblem(fixture.Run(), "path escapes the shared package root");
    }

    /// <summary>Findings Carry Their Code And Level.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void FindingsCarryTheirCodeAndLevel()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteManifest("dependencies", new JsonObject { ["@mui/material"] = "7.0.0" });
        var problems = UiFixture.Problems(fixture.Run());
        Assert.NotEmpty(problems);
        Assert.All(problems, problem =>
        {
            Assert.Contains("[G]", problem);
            Assert.Contains("[ui-dependency-invalid]", problem);
        });
    }
}
