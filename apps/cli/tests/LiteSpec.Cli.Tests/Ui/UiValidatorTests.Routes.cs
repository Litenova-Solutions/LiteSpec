namespace LiteSpec.Cli.Tests.Ui;

/// <summary>
/// The route suite coverage cases.
/// </summary>
public sealed partial class UiValidatorTests
{
    // Route suite coverage (litespec/core/ui-verification#run-geometry-sensors-on-every-route).

    /// <summary>A Route With No Route Suite.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ARouteWithNoRouteSuite()
    {
        using var fixture = UiFixture.Create();
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertProblem(
            fixture.Run(),
            "[litespec/core/ui-verification#run-geometry-sensors-on-every-route] frontend 'web': route 'apps/web/src/routes/+page.svelte' has no route suite at 'apps/web/tests/routes/every-route.spec.ts'");
    }

    /// <summary>A Second Route Is Reported Beside The First.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ASecondRouteIsReportedBesideTheFirst()
    {
        using var fixture = UiFixture.Create();
        File.Delete(fixture.SuiteFile);
        var result = fixture.PathCase(
            "apps/web/src/routes/orders/+page.svelte",
            "<h1>Orders</h1>\n");
        UiFixture.AssertProblem(result, "route 'apps/web/src/routes/+page.svelte' has no route suite");
        UiFixture.AssertProblem(result, "route 'apps/web/src/routes/orders/+page.svelte' has no route suite");
    }

    /// <summary>A Frontend With No Routes Needs No Suite.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void AFrontendWithNoRoutesNeedsNoSuite()
    {
        using var fixture = UiFixture.Create();
        File.Delete(fixture.AppCase);
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertClean(fixture.Run());
    }

    /// <summary>A Router Ignored Segment Is Not A Route.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ARouterIgnoredSegmentIsNotARoute()
    {
        using var fixture = UiFixture.Create();
        File.Delete(fixture.AppCase);
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/routes/-components/+page.svelte",
            "<h1>Card</h1>\n"));
    }

    /// <summary>A Suite Outside The Frontend Does Not Cover Its Routes.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ASuiteOutsideTheFrontendDoesNotCoverItsRoutes()
    {
        using var fixture = UiFixture.Create();
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertProblem(
            fixture.PathCase(
                "tests/routes/every-route.spec.ts",
                "// A suite at the consumer root, not under the frontend.\n"),
            "route 'apps/web/src/routes/+page.svelte' has no route suite");
    }

    /// <summary>A Svelte Page With No Route Suite.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ASveltePageWithNoRouteSuite()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertProblem(
            fixture.PathCase(
                "apps/web/src/routes/orders/+page.svelte",
                "<h1>Orders</h1>\n"),
            "[litespec/core/ui-verification#run-geometry-sensors-on-every-route] frontend 'web': route 'apps/web/src/routes/orders/+page.svelte' has no route suite at 'apps/web/tests/routes/every-route.spec.ts'");
    }

    /// <summary>A Svelte Page With Its Suite.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ASveltePageWithItsSuite()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/routes/orders/+page.svelte",
            "<h1>Orders</h1>\n"));
    }

    /// <summary>A Svelte Non Page File Is Not A Route.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ASvelteNonPageFileIsNotARoute()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        File.Delete(fixture.AppCase);
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/routes/bits.svelte",
            "<div>Bits</div>\n"));
    }

    /// <summary>A Svelte Server Endpoint Is Not A Route.</summary>
    /// <proves>litespec/core/ui-verification#run-geometry-sensors-on-every-route</proves>
    [Fact]
    public void ASvelteServerEndpointIsNotARoute()
    {
        using var fixture = UiFixture.Create();
        fixture.AsSvelte();
        File.Delete(fixture.AppCase);
        File.Delete(fixture.SuiteFile);
        UiFixture.AssertClean(fixture.PathCase(
            "apps/web/src/routes/api/+server.ts",
            "export function GET() {\n  return new Response(\"ok\");\n}\n"));
    }
}
