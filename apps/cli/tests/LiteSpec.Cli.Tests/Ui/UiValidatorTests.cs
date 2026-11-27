namespace LiteSpec.Cli.Tests.Ui;

/// <summary>
/// Controlled UI pass cases: one test per validator rule, asserting the expected diagnostics.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Baseline.

    /// <summary>Tracked Templates Validate As Shipped.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream</proves>
    [Fact]
    public void TrackedTemplatesValidateAsShipped()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.Run());
    }

    // Allowed construction (litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes).

    /// <summary>Approved Layout Utilities.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ApprovedLayoutUtilities()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<div class=\"grid gap-6 lg:grid-cols-2\">Orders</div>"));
    }

    /// <summary>Semantic Token Classes.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void SemanticTokenClasses()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<div class=\"bg-background text-foreground border-border\">Orders</div>"));
    }

    /// <summary>State And Container Variants.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void StateAndContainerVariants()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<div class=\"data-[state=open]:rotate-180 has-[input:checked]:bg-accent min-[420px]:flex\">Orders</div>"));
    }

    /// <summary>Class Merge Helper.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ClassMergeHelper()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<script>import { cn } from \"cn\";</script>\n<div class={cn(\"flex gap-2\", \"justify-between\")}>Orders</div>"));
    }

    /// <summary>Pinned Icon Family.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void PinnedIconFamily()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<script>import { Check } from \"lucide-svelte\";</script>\n<Check class=\"size-4\" />"));
    }

    /// <summary>The Primitive Library Named By The Package That Owns It.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ThePrimitiveLibraryNamedByThePackageThatOwnsIt()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<script>import { Dialog } from \"bits-ui\";</script>\n<Dialog />"));
    }

    /// <summary>Javascript Negation Is Not An Important Modifier.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void JavascriptNegationIsNotAnImportantModifier()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<script>let o = false;\nlet v = !o;</script>\n<div class=\"p-4\">Orders</div>"));
    }

    /// <summary>A Class Name Inside A Comment Is Prose.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AClassNameInsideACommentIsProse()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<!-- never use bg-blue-500 or w-[37rem] -->\n<div class=\"p-4\">Orders</div>"));
    }

    /// <summary>Composed Functional Variants.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ComposedFunctionalVariants()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "<div class=\"group-data-[state=open]:rotate-180 not-has-[input]:opacity-50 peer-aria-[expanded=true]:block\">Orders</div>"));
    }

    /// <summary>A File Under Tests Is Outside The Controlled Surface.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AFileUnderTestsIsOutsideTheControlledSurface()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/tests/case.test.svelte",
            "<div class=\"bg-red-500\">Orders</div>\n"));
    }

    /// <summary>A File Under The Copied Primitive Boundary Is Unedited Source.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AFileUnderTheCopiedPrimitiveBoundaryIsUneditedSource()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/src/components/extra.svelte",
            "<div class=\"bg-red-500\">Orders</div>\n"));
    }

    /// <summary>A Visual File Keeps Its Own Layout.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AVisualFileKeepsItsOwnLayout()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/modules/seat-map/seat-map.visual.svelte",
            "<div class=\"grid\" style=\"width: 120px\">Map</div>\n"));
    }

    /// <summary>A Visual File Is Still Held To The Token Rule.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AVisualFileIsStillHeldToTheTokenRule()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/seat-map/other.visual.svelte",
                "<div class=\"bg-red-500\">Map</div>\n"),
            "raw Tailwind palette value requires a semantic token 'bg-red-500'");
    }

    /// <summary>Generated Web Asset Is Ignored.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void GeneratedWebAssetIsIgnored()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/android/app/src/main/assets/public/generated.css",
            ".generated { color: red; }\n"));
    }

    /// <summary>A Route Composing One Floorplan.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ARouteComposingOneFloorplan()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.AppCaseWith(
            "<script>import ListPage from \"@fixture/ui/floorplans\";</script>\n<ListPage title=\"Orders\" />\n"));
    }

    // Restricted Tailwind use (litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes).

    /// <summary>Arbitrary Length.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ArbitraryLength()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("<div class=\"w-[37rem]\">Orders</div>;"),
            "arbitrary Tailwind value requires a semantic token or declared variant 'w-[37rem]'");
    }

    /// <summary>Arbitrary Color.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ArbitraryColor()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("<div class=\"bg-[#19324a]\">Orders</div>;"),
            "'bg-[#19324a]'");
    }

    /// <summary>Raw Palette Value.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void RawPaletteValue()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("<div class=\"text-red-500\">Orders</div>;"),
            "raw Tailwind palette value requires a semantic token 'text-red-500'");
    }

    /// <summary>Raw Palette Value With Opacity.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void RawPaletteValueWithOpacity()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("<div class=\"bg-slate-800/40\">Orders</div>;"),
            "'bg-slate-800/40'");
    }

    /// <summary>Important Modifier Prefix.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ImportantModifierPrefix()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("<div class=\"!mt-0\">Orders</div>;"),
            "important modifier requires a declared variant '!mt-0'");
    }

    /// <summary>An Escaped Quote Keeps The Following Literal Readable.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AnEscapedQuoteKeepsTheFollowingLiteralReadable()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("<script>import { cn } from \"cn\";</script>\n<div class={cn(\"a \\\" b\", \"text-red-500\")}>Orders</div>;"),
            "'text-red-500'");
    }

    /// <summary>An Arbitrary Selector Variant In A Drawing.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AnArbitrarySelectorVariantInADrawing()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/seat-map/grid.visual.svelte",
                "<div class=\"[&>svg]:size-4\">Map</div>\n"),
            "arbitrary selector variant requires a declared variant '[&>svg]:size-4'");
    }

    /// <summary>A Runtime Measurement In A Composite.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ARuntimeMeasurementInAComposite()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/src/shell/width.svelte",
            "<div style=\"width: 12px\">Orders</div>\n"));
    }

    /// <summary>Feature Selector In The Token Entry.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void FeatureSelectorInTheTokenEntry()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + ".promo-card{color:red}\n"),
            "selector '.promo-card' is a feature style");
    }

    /// <summary>A Comma Inside A Quoted Attribute Value Is Not A Selector List.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ACommaInsideAQuotedAttributeValueIsNotASelectorList()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CssCase(UiFixture.CleanCss + "[data-token=\",.promo\"]{color:red}\n"));
    }

    /// <summary>A Brace Inside A Quoted Attribute Value Is Not A Block.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ABraceInsideAQuotedAttributeValueIsNotABlock()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CssCase(UiFixture.CleanCss + "[data-token=\"{\"]{color:red}\n"));
    }

    /// <summary>An Unterminated String Is Reported Once.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void AnUnterminatedStringIsReportedOnce()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + "[data-token=\"promo]{color:red}\n"),
            "global CSS has an unterminated string");
    }

    /// <summary>Apply Directive Outside The Generated Base Layer.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void ApplyDirectiveOutsideTheGeneratedBaseLayer()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + "@layer components {\n  .btn { @apply px-4 py-2; }\n}\n"),
            "'@apply' outside the generated '@layer base' block");
    }

    /// <summary>Unapproved Global Import.</summary>
    /// <proves>litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes</proves>
    [Fact]
    public void UnapprovedGlobalImport()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + "@import \"bootstrap/dist/css/bootstrap.css\";\n"),
            "import 'bootstrap/dist/css/bootstrap.css' is outside the approved global CSS surface");
    }
}
