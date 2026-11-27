namespace LiteSpec.Cli.Tests.Ui;

/// <summary>
/// The application boundary, application stylesheet and shared stylesheet cases.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Application boundary (litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives).

    /// <summary>An Application Importing The Primitive Library.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void AnApplicationImportingThePrimitiveLibrary()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.AppCaseWith("<script>\nimport { Dialog } from \"bits-ui\";\n</script>\n<Dialog />\n"),
            "imports the primitive library directly");
    }

    /// <summary>An Application Importing A Table Library.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void AnApplicationImportingATableLibrary()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.AppCaseWith("<script>\nimport { flexRender } from \"@tanstack/svelte-table\";\n</script>\n<h1>Orders</h1>\n"),
            "imports the table library directly");
    }

    /// <summary>A Library Name Inside A Doc Comment Is Prose.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives</proves>
    [Fact]
    public void ALibraryNameInsideADocCommentIsProse()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.AppCaseWith(
            "<!-- Reads a value from `bits-ui` in the composite that wraps it. -->\n<h1>Orders</h1>\n"));
    }

    // Application stylesheet (litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale).

    /// <summary>An Application Stylesheet Importing The Package Tokens.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void AnApplicationStylesheetImportingThePackageTokens()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.AppStylesCase(UiFixture.CleanAppStyles));
    }

    /// <summary>An Application Stylesheet Writing A Value From A Token.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void AnApplicationStylesheetWritingAValueFromAToken()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.AppStylesCase(
            UiFixture.CleanAppStyles + "\n:where(button) {\n  --ring-width: 2px;\n}\n"));
    }

    /// <summary>An Application Stylesheet Declaring A Literal Value.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void AnApplicationStylesheetDeclaringALiteralValue()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.AppStylesCase(UiFixture.CleanAppStyles + "\nbody {\n  background: #ffffff;\n}\n"),
            "declares 'background'");
    }

    /// <summary>An Application Stylesheet Importing Nothing.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void AnApplicationStylesheetImportingNothing()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.AppStylesCase(string.Empty),
            "imports no stylesheet");
    }

    /// <summary>An Application Stylesheet Importing A Third Party Sheet.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void AnApplicationStylesheetImportingAThirdPartySheet()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.AppStylesCase("@import \"bootstrap/dist/css/bootstrap.css\";\n"),
            "import 'bootstrap/dist/css/bootstrap.css' is outside the shared package");
    }

    // Shared stylesheets (litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale).

    /// <summary>A Shared Stylesheet Written From Tokens.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void ASharedStylesheetWrittenFromTokens()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/src/styles/shadows.css",
            "@utility shade {\n  background: radial-gradient(farthest-side, var(--scroll-shade), transparent);\n}\n"));
    }

    /// <summary>A Shared Stylesheet Carrying A Literal Colour.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void ASharedStylesheetCarryingALiteralColour()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "packages/ui/src/styles/shadows.css",
                "@utility shade {\n  background: radial-gradient(farthest-side, oklch(0 0 0 / 0.16), transparent);\n}\n"),
            "literal color 'oklch(' belongs in the token stylesheet");
    }

    /// <summary>A Shared Stylesheet Carrying A Hex Colour.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale</proves>
    [Fact]
    public void ASharedStylesheetCarryingAHexColour()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "packages/ui/src/styles/shadows.css",
                "@utility shade {\n  color: #b9bbc6;\n}\n"),
            "literal color '#b9bbc6' belongs in the token stylesheet");
    }
}
