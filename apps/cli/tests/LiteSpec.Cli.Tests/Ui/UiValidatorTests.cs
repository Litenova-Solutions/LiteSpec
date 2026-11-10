namespace LiteSpec.Cli.Tests.Ui;

/// <summary>
/// Controlled UI pass cases: one test per validator rule, asserting the expected diagnostics.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Baseline.

    /// <summary>Tracked Templates Validate As Shipped.</summary>
    /// <proves>litespec/frontend/web/ui#track-source-changes</proves>
    [Fact]
    public void TrackedTemplatesValidateAsShipped()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.Run());
    }

    // Allowed construction (litespec/frontend/web/ui#restrict-css-decisions).

    /// <summary>Approved Layout Utilities.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ApprovedLayoutUtilities()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "export const C = () => <div className=\"grid gap-6 lg:grid-cols-2\" />;"));
    }

    /// <summary>Semantic Token Classes.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void SemanticTokenClasses()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "export const C = () => <div className=\"bg-background text-foreground border-border\" />;"));
    }

    /// <summary>State And Container Variants.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void StateAndContainerVariants()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "export const C = () => <div className=\"data-[state=open]:rotate-180 has-[input:checked]:bg-accent min-[420px]:flex\" />;"));
    }

    /// <summary>Class Merge Helper.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ClassMergeHelper()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "import { cn } from \"cn\";\nexport const C = ({ x }: { x?: string }) => <div className={cn(\"flex gap-2\", x && \"justify-between\")} />;"));
    }

    /// <summary>Pinned Icon Family.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void PinnedIconFamily()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "import { Check } from \"lucide-react\";\nexport const C = () => <Check className=\"size-4\" />;"));
    }

    /// <summary>The Primitive Library Named By The Package That Owns It.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ThePrimitiveLibraryNamedByThePackageThatOwnsIt()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "import { Dialog } from \"@base-ui/react/dialog\";\nexport const C = () => <Dialog />;"));
    }

    /// <summary>Javascript Negation Is Not An Important Modifier.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void JavascriptNegationIsNotAnImportantModifier()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "export const C = ({ o }: { o: boolean }) => {\n  const v = !o;\n  return v ? null : <div className=\"p-4\" />;\n};"));
    }

    /// <summary>A Class Name Inside A Comment Is Prose.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AClassNameInsideACommentIsProse()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "export const C = () => {\n  // never use bg-blue-500 or w-[37rem]\n  return <div className=\"p-4\" />;\n};"));
    }

    /// <summary>Composed Functional Variants.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ComposedFunctionalVariants()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CompositeCase(
            "export const C = () => <div className=\"group-data-[state=open]:rotate-180 not-has-[input]:opacity-50 peer-aria-[expanded=true]:block\" />;"));
    }

    /// <summary>A File Under Tests Is Outside The Controlled Surface.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AFileUnderTestsIsOutsideTheControlledSurface()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/tests/case.test.tsx",
            "export const C = () => <div className=\"bg-red-500\" />;\n"));
    }

    /// <summary>A File Under The Copied Primitive Boundary Is Unedited Source.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AFileUnderTheCopiedPrimitiveBoundaryIsUneditedSource()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/src/components/extra.tsx",
            "export const C = () => <div className=\"bg-red-500\" />;\n"));
    }

    /// <summary>A Visual File Keeps Its Own Layout.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AVisualFileKeepsItsOwnLayout()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/modules/seat-map/seat-map.visual.tsx",
            "export const C = () => <div className=\"grid\" style={{ width: 120 }} />;\n"));
    }

    /// <summary>A Visual File Is Still Held To The Token Rule.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AVisualFileIsStillHeldToTheTokenRule()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/seat-map/other.visual.tsx",
                "export const C = () => <div className=\"bg-red-500\" />;\n"),
            "raw Tailwind palette value requires a semantic token 'bg-red-500'");
    }

    /// <summary>Generated Web Asset Is Ignored.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void GeneratedWebAssetIsIgnored()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/android/app/src/main/assets/public/generated.css",
            ".generated { color: red; }\n"));
    }

    /// <summary>A Route Composing One Floorplan.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ARouteComposingOneFloorplan()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.AppCaseWith(
            "import { ListPage } from \"@fixture/ui/floorplans\";\nexport function Case() {\n  return <ListPage title=\"Orders\" />;\n}\n"));
    }

    // Restricted Tailwind use (litespec/frontend/web/ui#restrict-css-decisions).

    /// <summary>Arbitrary Length.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ArbitraryLength()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("export const C = () => <div className=\"w-[37rem]\" />;"),
            "arbitrary Tailwind value requires a semantic token or declared variant 'w-[37rem]'");
    }

    /// <summary>Arbitrary Color.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ArbitraryColor()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("export const C = () => <div className=\"bg-[#19324a]\" />;"),
            "'bg-[#19324a]'");
    }

    /// <summary>Raw Palette Value.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void RawPaletteValue()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("export const C = () => <div className=\"text-red-500\" />;"),
            "raw Tailwind palette value requires a semantic token 'text-red-500'");
    }

    /// <summary>Raw Palette Value With Opacity.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void RawPaletteValueWithOpacity()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("export const C = () => <div className=\"bg-slate-800/40\" />;"),
            "'bg-slate-800/40'");
    }

    /// <summary>Important Modifier Prefix.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ImportantModifierPrefix()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("export const C = () => <div className=\"!mt-0\" />;"),
            "important modifier requires a declared variant '!mt-0'");
    }

    /// <summary>An Escaped Quote Keeps The Following Literal Readable.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AnEscapedQuoteKeepsTheFollowingLiteralReadable()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CompositeCase("import { cn } from \"cn\";\nexport const C = () => <div className={cn(\"a \\\" b\", \"text-red-500\")} />;"),
            "'text-red-500'");
    }

    /// <summary>An Arbitrary Selector Variant In A Drawing.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AnArbitrarySelectorVariantInADrawing()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/modules/seat-map/grid.visual.tsx",
                "export const C = () => <div className=\"[&>svg]:size-4\" />;\n"),
            "arbitrary selector variant requires a declared variant '[&>svg]:size-4'");
    }

    /// <summary>A Runtime Measurement In A Composite.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ARuntimeMeasurementInAComposite()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.PathCase(
            "packages/ui/src/shell/width.tsx",
            "export const C = () => <div style={{ width: 12 }} />;\n"));
    }

    /// <summary>Feature Selector In The Token Entry.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void FeatureSelectorInTheTokenEntry()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + ".promo-card{color:red}\n"),
            "selector '.promo-card' is a feature style");
    }

    /// <summary>A Comma Inside A Quoted Attribute Value Is Not A Selector List.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ACommaInsideAQuotedAttributeValueIsNotASelectorList()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CssCase(UiFixture.CleanCss + "[data-token=\",.promo\"]{color:red}\n"));
    }

    /// <summary>A Brace Inside A Quoted Attribute Value Is Not A Block.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ABraceInsideAQuotedAttributeValueIsNotABlock()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertClean(fixture.CssCase(UiFixture.CleanCss + "[data-token=\"{\"]{color:red}\n"));
    }

    /// <summary>An Unterminated String Is Reported Once.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void AnUnterminatedStringIsReportedOnce()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + "[data-token=\"promo]{color:red}\n"),
            "global CSS has an unterminated string");
    }

    /// <summary>Apply Directive Outside The Generated Base Layer.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void ApplyDirectiveOutsideTheGeneratedBaseLayer()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + "@layer components {\n  .btn { @apply px-4 py-2; }\n}\n"),
            "'@apply' outside the generated '@layer base' block");
    }

    /// <summary>Unapproved Global Import.</summary>
    /// <proves>litespec/frontend/web/ui#restrict-css-decisions</proves>
    [Fact]
    public void UnapprovedGlobalImport()
    {
        using var fixture = UiFixture.Create();
        UiFixture.AssertProblem(
            fixture.CssCase(UiFixture.CleanCss + "@import \"bootstrap/dist/css/bootstrap.css\";\n"),
            "import 'bootstrap/dist/css/bootstrap.css' is outside the approved global CSS surface");
    }
}
