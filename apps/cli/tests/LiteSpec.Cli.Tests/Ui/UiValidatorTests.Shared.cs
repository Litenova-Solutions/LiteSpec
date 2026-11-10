using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Ui;

/// <summary>
/// The source boundary, shared package, additional registry and source lock cases.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Source boundary (litespec/frontend/ui#select-one-visual-authority).

    /// <summary>Second Visual System In The Workspace Root.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void Second_Visual_System_In_The_Workspace_Root()
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
    public void Second_Visual_System_In_OptionalDependencies()
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
    public void Second_Visual_System_Pinned_Through_Pnpm_Overrides()
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
    public void Second_Visual_System_Pinned_Through_Resolutions()
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
    public void UI_Exception_With_An_Expired_Date()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteException("ui-authority", "litespec/frontend/ui#select-one-visual-authority", "2020-01-01");
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/frontend/ui#select-one-visual-authority] expiresOn 2020-01-01 has passed; renew the exception");
    }

    /// <summary>UI Exception With A Live Date.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void UI_Exception_With_A_Live_Date()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteException("ui-authority", "litespec/frontend/ui#select-one-visual-authority", "2099-01-01");
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>Exception Against Another Area Stays Unread By The UI Validator.</summary>
    /// <proves>litespec/frontend/ui#select-one-visual-authority</proves>
    [Fact]
    public void Exception_Against_Another_Area_Stays_Unread_By_The_UI_Validator()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteException("other-area", "litespec/core/agent#select-task-context", "2020-01-01");
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
    public void A_Controlled_Frontend_With_No_Shared_Package()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateProject(project => project.Remove("ui"));
        Assert.Contains("[project-invalid] missing required property 'ui'", fixture.Run().Output, StringComparison.Ordinal);
    }

    /// <summary>A Shared Package Declaring The Same Export Twice.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void A_Shared_Package_Declaring_The_Same_Export_Twice()
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
    public void A_Source_Lock_Outside_The_Package_Root()
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
    public void Components_Json_Naming_An_Icon_Library_The_Baseline_Does_Not_Pin()
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
    public void Source_Lock_Describing_A_Preset_The_Manifest_Does_Not_Pin()
    {
        using var fixture = UiFixture.Create();
        fixture.MutateLock(sourceLock => ((JsonObject)sourceLock["preset"]!)["style"] = "fixture-style");
        UiFixture.AssertProblem(fixture.Run(), "preset.style does not match the manifest baseline");
    }

    // Additional registries (litespec/frontend/ui#pin-the-baseline-in-the-shared-package).

    /// <summary>An Additional Registry With The Exception Passes Both Gates.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void An_Additional_Registry_With_The_Exception_Passes_Both_Gates()
    {
        using var fixture = UiFixture.Create();
        fixture.AddAdditionalRegistry();
        fixture.WriteException("extra-registry", "litespec/frontend/ui#pin-the-baseline-in-the-shared-package", "2099-01-01");
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>The Configuration Gate Refuses Without The Exception.</summary>
    /// <proves>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</proves>
    [Fact]
    public void The_Configuration_Gate_Refuses_Without_The_Exception()
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
    public void The_Source_Lock_Gate_Refuses_Without_The_Exception()
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
    public void Changed_Copied_Source_Without_A_Fork_Record()
    {
        using var fixture = UiFixture.Create();
        File.AppendAllText(fixture.ButtonFile, "// local change\n");
        UiFixture.AssertProblem(fixture.Run(), "no longer matches the recorded digest");
    }

    /// <summary>A Dependency The Manifest Does Not Pin.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void A_Dependency_The_Manifest_Does_Not_Pin()
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
    public void A_Path_Escaping_The_Package_Root()
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
    public void Findings_Carry_Their_Code_And_Level()
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
