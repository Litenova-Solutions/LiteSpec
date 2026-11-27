using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Tests.Ui;

/// <summary>
/// The Svelte source and Svelte baseline cases.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Svelte sources (litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes).

    /// <summary>A Svelte File Is Scanned For Utility Violations.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteFileIsScannedForUtilityViolations()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "packages/ui/src/floorplans/card.svelte",
                "<div class=\"text-red-500\">Overdue</div>\n"),
            "raw Tailwind palette value requires a semantic token 'text-red-500'");
    }

    /// <summary>A Svelte Visual File Keeps Its Own Layout.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteVisualFileKeepsItsOwnLayout()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/modules/seat-map/seat-map.visual.svelte",
            "<div class=\"grid\">Map</div>\n"));
    }

    /// <summary>A Svelte Visual File Is Still Held To The Token Rule.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteVisualFileIsStillHeldToTheTokenRule()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/seat-map/other.visual.svelte",
                "<div class=\"bg-red-500\">Map</div>\n"),
            "raw Tailwind palette value requires a semantic token 'bg-red-500'");
    }

    /// <summary>A Svelte Class Directive Is Read As One Token.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteClassDirectiveIsReadAsOneToken()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/modules/orders/toggle.svelte",
            "<button class:hidden={closed}>Hide</button>\n"));
    }

    /// <summary>A Svelte Style Attribute Is An Inline Style.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteStyleAttributeIsAnInlineStyle()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/orders/styled.svelte",
                "<div style=\"color: red\">Overdue</div>\n"),
            "an inline style belongs in a shared component");
    }

    /// <summary>A Svelte Style Directive Is An Inline Style.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteStyleDirectiveIsAnInlineStyle()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/orders/styled-directive.svelte",
                "<div style:color={ink}>Overdue</div>\n"),
            "an inline style belongs in a shared component");
    }

    /// <summary>A Svelte Style Block Is Not An Inline Style.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteStyleBlockIsNotAnInlineStyle()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/modules/orders/scoped.svelte",
            "<style>\n  .scoped {\n    color: red;\n  }\n</style>\n<div class=\"scoped\">Overdue</div>\n"));
    }

    /// <summary>Second Svelte Visual System In The Workspace Root.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void SecondSvelteVisualSystemInTheWorkspaceRoot()
    {
        using var fixture = UiFixture.Create();
        fixture.WriteManifest("dependencies", new JsonObject { ["flowbite-svelte"] = "1.33.1" });
        UiFixture.AssertProblem(
            fixture.Run(),
            "second general-purpose visual dependency 'flowbite-svelte' requires an exception");
    }

    // Svelte baseline (litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream,
    // litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream,
    // litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives,
    // litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives).

    /// <summary>A Svelte Package Validates Against The Svelte Baseline.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void ASveltePackageValidatesAgainstTheSvelteBaseline()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>A Svelte Application Importing The Primitive Library.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void ASvelteApplicationImportingThePrimitiveLibrary()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/routes/bits.svelte",
                "<script>\nimport { Dialog } from \"bits-ui\";\n</script>\n<Dialog />\n"),
            "[litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives] imports the primitive library directly");
    }

    /// <summary>A Svelte Application Importing The React Primitive Library Is Outside Its Boundary.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void ASvelteApplicationImportingTheReactPrimitiveLibraryIsOutsideItsBoundary()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/routes/shim.svelte",
            "<script>\nimport { Dialog } from \"@base-ui/react/dialog\";\n</script>\n"));
    }

    /// <summary>A Svelte Application Importing A Table Library.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void ASvelteApplicationImportingATableLibrary()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/routes/table.svelte",
                "<script>\nimport { flexRender } from \"@tanstack/svelte-table\";\n</script>\n"),
            "imports the table library directly");
    }

    /// <summary>A Svelte Utility Violation Cites The Svelte Provision.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ASvelteUtilityViolationCitesTheSvelteProvision()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "packages/ui/src/floorplans/card.svelte",
                "<div class=\"text-red-500\">Overdue</div>\n"),
            "[litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes] raw Tailwind palette value requires a semantic token 'text-red-500'");
    }

    /// <summary>A Svelte Source Lock Describing A Preset The Manifest Does Not Pin.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void ASvelteSourceLockDescribingAPresetTheManifestDoesNotPin()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        fixture.MutateLock(sourceLock => ((JsonObject)sourceLock["preset"]!)["baseColor"] = "slate");
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream] preset.baseColor does not match the manifest baseline");
    }

    /// <summary>A Svelte Source Lock Naming The React CLI Pin.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void ASvelteSourceLockNamingTheReactCLIPin()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        fixture.MutateLock(sourceLock => sourceLock["cli"] = "4.21.5");
        UiFixture.AssertProblem(
            fixture.Run(),
            "cli must match manifest shadcn-svelte pin '1.7.0'");
    }

    /// <summary>A Svelte Source Lock Naming Another Registry.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void ASvelteSourceLockNamingAnotherRegistry()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        fixture.MutateLock(sourceLock =>
        {
            sourceLock["registry"] = new JsonObject { ["name"] = "fixture", ["url"] = "https://blocks.fixture.test" };
        });
        UiFixture.AssertProblem(
            fixture.Run(),
            "registry 'fixture' is not the built-in shadcn-svelte registry");
    }

    /// <summary>Svelte Components Json Naming A Base Colour The Baseline Does Not Pin.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void SvelteComponentsJsonNamingABaseColourTheBaselineDoesNotPin()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        var components = (JsonObject)UiFixture.ReadJson(fixture.ComponentsFile)!;
        ((JsonObject)components["tailwind"]!)["baseColor"] = "slate";
        UiFixture.WriteJson(fixture.ComponentsFile, components);
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream] tailwind.baseColor must be 'neutral'");
    }

    /// <summary>Svelte Components Json Naming Another Registry.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void SvelteComponentsJsonNamingAnotherRegistry()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        var components = (JsonObject)UiFixture.ReadJson(fixture.ComponentsFile)!;
        components["registry"] = "https://blocks.fixture.test";
        UiFixture.WriteJson(fixture.ComponentsFile, components);
        UiFixture.AssertProblem(
            fixture.Run(),
            "registry 'https://blocks.fixture.test' is not the built-in shadcn-svelte registry");
    }

    /// <summary>A Svelte Registry Exception Carries The Frontend Provision.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void ASvelteRegistryExceptionCarriesTheFrontendProvision()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        var components = (JsonObject)UiFixture.ReadJson(fixture.ComponentsFile)!;
        components["registry"] = "https://blocks.fixture.test";
        UiFixture.WriteJson(fixture.ComponentsFile, components);
        fixture.WriteWaiver("extra-registry", "litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream", "2099-01-01");
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>A Svelte UI Exception With An Expired Date.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void ASvelteUIExceptionWithAnExpiredDate()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        fixture.WriteWaiver("ui-authority", "litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives", "2020-01-01");
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives] expiresOn 2020-01-01 has passed; renew the waiver or close the gap");
    }

}
