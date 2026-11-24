using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Screens;

/// <summary>
///     The UI blueprint checks: wireframe references and composition, screen frames and primary
///     actions, screen reachability, wording aliases, refusal sentences, sentence languages and
///     the token limits, each proved on a small model that breaks one rule.
/// </summary>
public sealed class UiBlueprintCheckTests
{
    /// <summary>A wireframe action that names no use case the screen offers reports wireframe-invalid.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeActionNamingAnUnofferedUseCaseReportsWireframeInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action primary "Approve loan"
            ```
            """));

        var findings = WithCode(model, "wireframe-invalid");

        Assert.Single(findings);
        Assert.Contains("Approve loan", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A wireframe action that names an offered use case, matched case-insensitively, reports nothing.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeActionNamingAnOfferedUseCaseReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action primary "list loans"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-invalid"));
    }

    /// <summary>A wireframe local action needs no use case the screen offers.</summary>
    /// <param name="local">The local action copy.</param>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Theory]
    [InlineData("Cancel")]
    [InlineData("Close")]
    [InlineData("Back")]
    [InlineData("Clear")]
    [InlineData("Skip")]
    [InlineData("Show more")]
    [InlineData("Show more lines")]
    public void WireframeLocalActionReportsNothing(string local)
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            LocalExplanation(local)));

        Assert.Empty(WithCode(model, "wireframe-invalid"));
    }

    /// <summary>A wireframe link naming no use case reports nothing: navigation is a link, not an action.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeLinkNamingNoUseCaseReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                link "Open the reports"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-invalid"));
    }

    /// <summary>A wireframe action starting with Open still names a use case: navigation written as an action reports wireframe-invalid.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeActionStartingWithOpenReportsWireframeInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action "Open the reports"
            ```
            """));

        var findings = WithCode(model, "wireframe-invalid");

        Assert.Single(findings);
        Assert.Contains("Open the reports", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A wireframe refusal matching the sentence of a written refusal result of an offered use case reports nothing.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeRefusalMatchingAWrittenSentenceReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","sentence":"That list is not available."}""",
            temp.Path));
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "Loans"
                message "That list is not available." critical
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-invalid"));
    }

    /// <summary>A wireframe refusal matching no result sentence of an offered use case reports wireframe-invalid.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeRefusalMatchingNoSentenceReportsWireframeInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "Loans"
                message "That list is not available." critical
            ```
            """));

        var findings = WithCode(model, "wireframe-invalid");

        Assert.Single(findings);
        Assert.Contains("That list is not available.", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A wireframe refusal matching the sentence of a derived generic refusal result of an offered use case reports nothing.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void WireframeRefusalMatchingADerivedSentenceReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        using var document = JsonDocument.Parse("""{"outcome":"refusal","code":"sign-in-too-old","meaning":"The caller's sign-in is older than the window the use case requires.","sentence":"Sign in again to continue."}""");
        model.Records.Add(new Record
        {
            Type = "result",
            Id = "result/list-loans.sign-in-too-old",
            Title = "Sign in too old",
            File = Path.Combine(temp.Path, "result.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
            Derived = true,
        });
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "Loans"
                message "Sign in again to continue." critical
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-invalid"));
    }

    /// <summary>A screen with a route and no page frame reports screen-pageframe-missing.</summary>
    /// <proves>litespec/frontend/ui#name-the-page-frame-on-every-screen</proves>
    [Fact]
    public void ScreenWithRouteAndNoPageFrameReportsPageframeMissing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], [], pageFramePattern: null));

        var findings = WithCode(model, "screen-pageframe-missing");

        Assert.Single(findings);
        Assert.Contains("screen/app.loans", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A screen with a route and a page frame reports no screen-pageframe-missing.</summary>
    /// <proves>litespec/frontend/ui#name-the-page-frame-on-every-screen</proves>
    [Fact]
    public void ScreenWithRouteAndPageFrameReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        Assert.Empty(WithCode(model, "screen-pageframe-missing"));
    }

    /// <summary>A screen hosted on another screen names no page frame and reports no screen-pageframe-missing.</summary>
    /// <proves>litespec/frontend/ui#name-the-page-frame-on-every-screen</proves>
    [Fact]
    public void ScreenWithHostAndNoPageFrameReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));
        model.Records.Add(new Record
        {
            Type = "screen",
            Id = "screen/app.loan-overlay",
            Title = "Loan overlay",
            File = Path.Combine(temp.Path, "overlay.md"),
            Line = 1,
            Raw = JsonDocument.Parse("""{"entryPoint":"entry-point/app","host":"screen/app.loans","offers":[],"serves":[],"leadsTo":[]}""").RootElement.Clone(),
        });

        Assert.Empty(WithCode(model, "screen-pageframe-missing"));
    }

    /// <summary>A screen offering past the offer limit outside a hub frame reports screen-pageframe-invalid.</summary>
    /// <proves>litespec/frontend/ui#route-a-busy-screen-through-a-hub-frame</proves>
    [Fact]
    public void BusyScreenOutsideHubFrameReportsPageframeInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy", "extend-loan", "return-copy", "place-hold", "cancel-hold");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans",
            ["use-case/lending.list-loans", "use-case/lending.borrow-copy", "use-case/lending.extend-loan", "use-case/lending.return-copy", "use-case/lending.place-hold", "use-case/lending.cancel-hold"],
            [], pageFramePattern: "report-page"));

        var findings = WithCode(model, "screen-pageframe-invalid");

        Assert.Single(findings);
        Assert.Contains("5 commands", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("'report-page'", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A screen offering past the offer limit on a hub frame reports no screen-pageframe-invalid.</summary>
    /// <param name="pattern">The hub page-frame pattern the screen follows.</param>
    /// <proves>litespec/frontend/ui#route-a-busy-screen-through-a-hub-frame</proves>
    [Theory]
    [InlineData("home-page")]
    [InlineData("settings-home")]
    [InlineData("queue")]
    [InlineData("list-page")]
    public void BusyScreenWithHubFrameReportsNothing(string pattern)
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy", "extend-loan", "return-copy", "place-hold", "cancel-hold");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans",
            ["use-case/lending.list-loans", "use-case/lending.borrow-copy", "use-case/lending.extend-loan", "use-case/lending.return-copy", "use-case/lending.place-hold", "use-case/lending.cancel-hold"],
            [], pageFramePattern: pattern));

        Assert.Empty(WithCode(model, "screen-pageframe-invalid"));
    }

    /// <summary>A screen offering exactly the offer limit of commands outside a hub frame, beside the query it reads, reports no screen-pageframe-invalid.</summary>
    /// <proves>litespec/frontend/ui#route-a-busy-screen-through-a-hub-frame</proves>
    [Fact]
    public void ScreenAtOfferLimitOutsideHubFrameReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy", "extend-loan", "return-copy", "place-hold");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans",
            ["use-case/lending.list-loans", "use-case/lending.borrow-copy", "use-case/lending.extend-loan", "use-case/lending.return-copy", "use-case/lending.place-hold"],
            [], pageFramePattern: "report-page"));

        Assert.Empty(WithCode(model, "screen-pageframe-invalid"));
    }

    /// <summary>A project offer limit above the default lets a busier screen stay outside a hub frame.</summary>
    /// <proves>litespec/frontend/ui#route-a-busy-screen-through-a-hub-frame</proves>
    [Fact]
    public void CustomOfferLimitLetsABusierScreenStayOutsideHubFrame()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteProjectWithLimits(temp.Path, "\"offers\": 5");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy", "extend-loan", "return-copy", "place-hold", "cancel-hold");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans",
            ["use-case/lending.list-loans", "use-case/lending.borrow-copy", "use-case/lending.extend-loan", "use-case/lending.return-copy", "use-case/lending.place-hold", "use-case/lending.cancel-hold"],
            [], pageFramePattern: "report-page"));

        Assert.Empty(WithCode(model, "screen-pageframe-invalid"));
    }

    /// <summary>A primary action naming no offered use case reports screen-primary-action-unknown.</summary>
    /// <proves>litespec/frontend/ui#show-one-primary-action-per-view</proves>
    [Fact]
    public void PrimaryActionOutsideOffersReportsUnknown()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        AddUseCases(model, temp.Path, "borrow-copy");
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"], [],
            primaryAction: "use-case/lending.borrow-copy"));

        var findings = WithCode(model, "screen-primary-action-unknown");

        Assert.Single(findings);
        Assert.Contains("use-case/lending.borrow-copy", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A primary action naming an offered use case reports no screen-primary-action-unknown.</summary>
    /// <proves>litespec/frontend/ui#show-one-primary-action-per-view</proves>
    [Fact]
    public void PrimaryActionInOffersReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen(
            "screen/app.loans", "/loans", ["use-case/lending.list-loans"], [],
            primaryAction: "use-case/lending.list-loans"));

        Assert.Empty(WithCode(model, "screen-primary-action-unknown"));
    }

    /// <summary>A card drawn inside a card reports wireframe-nesting-invalid.</summary>
    /// <proves>litespec/frontend/ui#place-no-surface-inside-another</proves>
    [Fact]
    public void WireframeCardInsideCardReportsNestingInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              card "Outside"
                card "Inside"
                  text "Nested"
            ```
            """));

        var findings = WithCode(model, "wireframe-nesting-invalid");

        Assert.Single(findings);
        Assert.Contains("no card", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A card drawn inside a region reports wireframe-nesting-invalid.</summary>
    /// <proves>litespec/frontend/ui#place-no-surface-inside-another</proves>
    [Fact]
    public void WireframeCardInsideRegionReportsNestingInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "List"
                card "Loan"
                  text "EXP-1042"
            ```
            """));

        var findings = WithCode(model, "wireframe-nesting-invalid");

        Assert.Single(findings);
        Assert.Contains("region holds no card", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A field outside every section reports wireframe-surface-missing.</summary>
    /// <proves>litespec/frontend/ui#draw-every-field-on-a-surface</proves>
    [Fact]
    public void WireframeFieldOutsideSectionReportsSurfaceMissing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              field "Search loans" search
              region "Loans"
                list 1
                  item: text "EXP-1042"
            ```
            """));

        var findings = WithCode(model, "wireframe-surface-missing");

        Assert.Single(findings);
        Assert.Contains("a field sits on a surface", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A wireframe keeping every composition rule reports no composition finding.</summary>
    /// <proves>litespec/frontend/ui#draw-every-field-on-a-surface</proves>
    [Fact]
    public void WireframeKeepingEveryCompositionRuleReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action primary "list loans"
              region "Loans"
                field "Search loans" search
                list 1
                  item: text "EXP-1042"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-nesting-invalid"));
        Assert.Empty(WithCode(model, "wireframe-surface-missing"));
        Assert.Empty(WithCode(model, "wireframe-tabs-missing"));
        Assert.Empty(WithCode(model, "wireframe-group-missing"));
        Assert.Empty(WithCode(model, "wireframe-invalid"));
    }

    /// <summary>A page stacking four sections with no tabs reports wireframe-tabs-missing.</summary>
    /// <proves>litespec/frontend/ui#limit-a-page-to-three-stacked-sections</proves>
    [Fact]
    public void WireframeFourSectionsReportsTabsMissing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "One"
                text "First"
              region "Two"
                text "Second"
              region "Three"
                text "Third"
              region "Four"
                text "Fourth"
            ```
            """));

        var findings = WithCode(model, "wireframe-tabs-missing");

        Assert.Single(findings);
        Assert.Contains("no tabs or navigation", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A page stacking three sections reports no wireframe-tabs-missing.</summary>
    /// <proves>litespec/frontend/ui#limit-a-page-to-three-stacked-sections</proves>
    [Fact]
    public void WireframeThreeSectionsReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "One"
                text "First"
              region "Two"
                text "Second"
              region "Three"
                text "Third"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-tabs-missing"));
    }

    /// <summary>Four sections behind tabs report no wireframe-tabs-missing.</summary>
    /// <proves>litespec/frontend/ui#limit-a-page-to-three-stacked-sections</proves>
    [Fact]
    public void WireframeFourSectionsWithTabsReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              tabs "Active" "Archived"
                region "Active"
                  text "First"
              region "Two"
                text "Second"
              region "Three"
                text "Third"
              region "Four"
                text "Fourth"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-tabs-missing"));
    }

    /// <summary>A project section limit above the default lets a longer page stay undivided.</summary>
    /// <proves>litespec/frontend/ui#limit-a-page-to-three-stacked-sections</proves>
    [Fact]
    public void CustomSectionLimitLetsALongerPageStayUndivided()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteProjectWithLimits(temp.Path, "\"sections\": 4");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
              region "One"
                text "First"
              region "Two"
                text "Second"
              region "Three"
                text "Third"
              region "Four"
                text "Fourth"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-tabs-missing"));
    }

    /// <summary>Seven fields in one section report wireframe-group-missing.</summary>
    /// <proves>litespec/frontend/ui#group-a-long-form</proves>
    [Fact]
    public void WireframeLongFormReportsGroupMissing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loan", "/loans/new", ["use-case/lending.list-loans"],
            """
            # New loan

            ```wireframe
            page
              header
                heading "New loan"
              region "The loan"
                field "Title" text
                field "Amount" number
                field "Spender" text
                field "Trip" text
                field "Date" date
                field "Cost centre" select
                field "Note" textarea
            ```
            """));

        var findings = WithCode(model, "wireframe-group-missing");

        Assert.Single(findings);
        Assert.Contains("two regions", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>Seven fields across two sections report no wireframe-group-missing.</summary>
    /// <proves>litespec/frontend/ui#group-a-long-form</proves>
    [Fact]
    public void WireframeLongFormAcrossTwoSectionsReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loan", "/loans/new", ["use-case/lending.list-loans"],
            """
            # New loan

            ```wireframe
            page
              header
                heading "New loan"
              region "The loan"
                field "Title" text
                field "Amount" number
                field "Spender" text
                field "Trip" text
              region "When"
                field "Date" date
                field "Cost centre" select
                field "Note" textarea
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-group-missing"));
    }

    /// <summary>Two dialogs of four fields each are two forms and report no wireframe-group-missing.</summary>
    /// <proves>litespec/frontend/ui#group-a-long-form</proves>
    [Fact]
    public void FieldsInSeparateDialogsCountAsSeparateForms()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loan", "/loans/settings", ["use-case/lending.list-loans"],
            """
            # Loan settings

            ```wireframe
            page
              header
                heading "Loan settings"
              region "Loans"
                fact "Loan period" "21 days"
              dialog
                heading small "Loan period"
                field "Days" number
                field "Renewals" number
                field "Grace days" number
                field "Note" textarea
                action primary "Save"
              dialog
                heading small "Fines"
                field "Fine per day" number
                field "Cap" number
                field "Currency" select
                field "Note" textarea
                action primary "Save"
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-group-missing"));
    }

    /// <summary>Six fields in one section report no wireframe-group-missing.</summary>
    /// <proves>litespec/frontend/ui#group-a-long-form</proves>
    [Fact]
    public void WireframeSixFieldsReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loan", "/loans/new", ["use-case/lending.list-loans"],
            """
            # New loan

            ```wireframe
            page
              header
                heading "New loan"
              region "The loan"
                field "Title" text
                field "Amount" number
                field "Spender" text
                field "Trip" text
                field "Date" date
                field "Cost centre" select
            ```
            """));

        Assert.Empty(WithCode(model, "wireframe-group-missing"));
    }

    /// <summary>Two primary actions in one view report wireframe-invalid.</summary>
    /// <proves>litespec/frontend/ui#show-one-primary-action-per-view</proves>
    [Fact]
    public void WireframeTwoPrimariesReportsWireframeInvalid()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Loans"
                action primary "New loan"
              region "Loans"
                action primary "New loan"
                list 1
                  item: text "EXP-1042"
            ```
            """));

        var findings = WithCode(model, "wireframe-invalid");

        Assert.Single(findings);
        Assert.Contains("one primary action", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A layer matching the surface it sits on reports design-token-limit-failed naming the layer.</summary>
    /// <proves>litespec/frontend/ui#meet-the-limits-of-every-token-role</proves>
    [Fact]
    public void LayerMatchingItsSurfaceReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "layer-1": {"$value": "#ffffff"},
                "text": {"$value": "#111111"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.Contains(findings, finding => finding.Message.Contains("'layer-1'", StringComparison.Ordinal) && finding.Message.Contains("1.06", StringComparison.Ordinal));
    }

    /// <summary>A layer accent matching a layer reports design-token-limit-failed naming the accent and the layer.</summary>
    /// <proves>litespec/frontend/ui#meet-the-limits-of-every-token-role</proves>
    [Fact]
    public void LayerAccentMatchingALayerReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "layer-1": {"$value": "#000000"},
                "layer-2": {"$value": "#ffffff"},
                "layer-accent": {"$value": "#ffffff"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.Contains(findings, finding => finding.Message.Contains("'layer-accent'", StringComparison.Ordinal) && finding.Message.Contains("'layer-2'", StringComparison.Ordinal));
    }

    /// <summary>Separated layers report no layer separation finding.</summary>
    /// <proves>litespec/frontend/ui#meet-the-limits-of-every-token-role</proves>
    [Fact]
    public void SeparatedLayersReportNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "layer-1": {"$value": "#000000"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.DoesNotContain(findings, finding => finding.Message.Contains("'layer-1' reaches", StringComparison.Ordinal));
    }

    /// <summary>A screen no other screen leads to, that is not its entry point's start, reports screen-unreachable.</summary>
    /// <proves>litespec/frontend/ui#link-each-screen-to-its-next-screens</proves>
    [Fact]
    public void ScreenLedToByNoOtherScreenReportsUnreachable()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.home", "/", ["use-case/lending.list-loans"], []));
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        var findings = WithCode(model, "screen-unreachable");

        Assert.Single(findings);
        Assert.Contains("screen/app.loans", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A screen another screen leads to reports no unreachable finding.</summary>
    /// <proves>litespec/frontend/ui#link-each-screen-to-its-next-screens</proves>
    [Fact]
    public void ScreenLedToByAnotherScreenReportsNoUnreachable()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Screen("screen/app.home", "/", ["use-case/lending.list-loans"], [], ["screen/app.loans"]));
        model.Records.Add(Screen("screen/app.loans", "/loans", ["use-case/lending.list-loans"], []));

        Assert.Empty(WithCode(model, "screen-unreachable"));
    }

    /// <summary>A glossary alias in a result sentence reports wording-alias and names the term to use.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void AliasInAResultSentenceReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(new Record
        {
            Type = "term",
            Id = "term/borrowing",
            Title = "Borrowing",
            File = Path.Combine(temp.Path, "term.md"),
            Line = 1,
            Raw = JsonDocument.Parse("""{"meaning":"A copy held by one member.","aliases":["Checkout"]}""").RootElement.Clone(),
        });
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","sentence":"The checkout is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("Checkout", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Borrowing", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>An alias inside a longer glossary term in a result sentence reports no wording-alias.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void AliasInsideALongerTermInASentenceReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","sentence":"The sales invoice is refused."}""",
            temp.Path));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>A bare alias in a result sentence still reports wording-alias when a longer term holds it.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void BareAliasInASentenceStillReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","sentence":"The invoice is refused."}""",
            temp.Path));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("invoice", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Source Document", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>An alias inside a longer glossary label in a result sentence reports no wording-alias.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void AliasInsideALongerLabelInASentenceReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-ledger", "Sales Ledger", """{"meaning":"A record of sales.","labels":{"en":"sales invoice"}}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","sentence":"The sales invoice is refused."}""",
            temp.Path));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>An alias inside a longer glossary term in a wireframe reports no wording-alias.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void AliasInsideALongerTermInAWireframeReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Sales invoice"
            ```
            """));

        Assert.Empty(WithCode(model, "wording-alias"));
    }

    /// <summary>A bare alias in a wireframe still reports wording-alias when a longer term holds it.</summary>
    /// <proves>litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built</proves>
    [Fact]
    public void BareAliasInAWireframeStillReportsWordingAlias()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.Add(Term("term/source-document", "Source Document", """{"meaning":"A paper proving a claim.","aliases":["invoice"]}""", temp.Path));
        model.Records.Add(Term("term/sales-invoice", "Sales Invoice", """{"meaning":"A bill for a sale."}""", temp.Path));
        model.Records.Add(ScreenWithExplanation(
            temp.Path, "screen/app.loans", "/loans", ["use-case/lending.list-loans"],
            """
            # Loans

            ```wireframe
            page
              header
                heading "Invoice"
            ```
            """));

        var findings = WithCode(model, "wording-alias");

        Assert.Single(findings);
        Assert.Contains("invoice", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Source Document", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A token role below its contrast limit reports design-token-limit-failed naming the role.</summary>
    /// <proves>litespec/frontend/ui#meet-the-limits-of-every-token-role</proves>
    [Fact]
    public void TokenRoleBelowItsLimitReportsTokenLimitFailed()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        WriteDesignContract(temp.Path, """
            {
              "light": {
                "page": {"$value": "#ffffff"},
                "text": {"$value": "#c8c8c8"}
              }
            }
            """);
        var model = BaseModel(temp.Path);

        var findings = WithCode(model, "design-token-limit-failed");

        Assert.NotEmpty(findings);
        Assert.Contains(findings, finding => finding.Message.Contains("'text'", StringComparison.Ordinal));
    }

    /// <summary>A written refusal missing a declared language reports sentence-language-missing.</summary>
    /// <proves>litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point</proves>
    [Fact]
    public void RefusalMissingADeclaredLanguageReportsSentenceLanguageMissing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));

        var findings = WithCode(model, "sentence-language-missing");

        Assert.Single(findings);
        Assert.Contains("result/list-loans.refused", findings[0].Message, StringComparison.Ordinal);
        Assert.Contains("'nl'", findings[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A written refusal carrying every declared language reports no sentence-language-missing.</summary>
    /// <proves>litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point</proves>
    [Fact]
    public void RefusalCarryingEveryDeclaredLanguageReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        model.Records.Add(Rec(
            "result", "result/list-loans.refused",
            """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused","sentences":{"nl":"De lijst is geweigerd."}}""",
            temp.Path));

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>The default language needs no sentences entry.</summary>
    /// <proves>litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point</proves>
    [Fact]
    public void DefaultLanguageNeedsNoSentencesEntry()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en"]}""", temp.Path));

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>A derived refusal without sentences reports no sentence-language-missing.</summary>
    /// <proves>litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point</proves>
    [Fact]
    public void DerivedRefusalWithoutSentencesReportsNothing()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "result/list-loans.refused");
        using var document = JsonDocument.Parse("""{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused"}""");
        model.Records.Add(new Record
        {
            Type = "result",
            Id = "result/list-loans.refused",
            Title = "result/list-loans.refused",
            File = Path.Combine(temp.Path, "result.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
            Derived = true,
        });

        Assert.Empty(WithCode(model, "sentence-language-missing"));
    }

    /// <summary>A use case reached through entry points declaring two languages reports one finding naming each missing sentence.</summary>
    /// <proves>litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point</proves>
    [Fact]
    public void TwoEntryPointsRequireEachDeclaredLanguage()
    {
        using var temp = TestCli.Temp("ui-blueprint");
        var model = BaseModel(temp.Path);
        model.Records.RemoveAll(record => record.Id == "entry-point/app");
        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser","languages":["en","nl"]}""", temp.Path));
        model.Records.Add(Rec("entry-point", "entry-point/kiosk", """{"kind":"browser","languages":["en","fr"]}""", temp.Path));
        model.Records.RemoveAll(record => record.Id == "use-case/lending.list-loans");
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app","entry-point/kiosk"]}""", temp.Path));

        var finding = Assert.Single(WithCode(model, "sentence-language-missing"));

        Assert.Contains("'nl'", finding.Message, StringComparison.Ordinal);
        Assert.Contains("'fr'", finding.Message, StringComparison.Ordinal);
    }

    static List<Finding> WithCode(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == code).ToList();

    /// <summary>An explanation whose wireframe holds one action with the copy.</summary>
    /// <param name="action">The action copy.</param>
    /// <returns>The explanation text.</returns>
    static string LocalExplanation(string action) =>
        "# Loans\n\n```wireframe\npage\n  header\n    heading \"Loans\"\n    action \"" + action + "\"\n```\n";

    /// <summary>Adds query use cases of the lending module reached through the app entry point.</summary>
    /// <param name="model">The model holding the records.</param>
    /// <param name="root">The consumer root the record files sit under.</param>
    /// <param name="names">The use-case names to add.</param>
    static void AddUseCases(ProjectModel model, string root, params string[] names)
    {
        foreach (var name in names)
        {
            model.Records.Add(Rec("use-case", $"use-case/lending.{name}", """{"kind":"command","reachedThrough":["entry-point/app"]}""", root));
        }
    }

    /// <summary>Writes a project whose <c>ui.limits</c> carries the limits entries.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="limits">The limits entries as JSON object members.</param>
    static void WriteProjectWithLimits(string root, string limits)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """{"owners": ["lending-team"],"frontends": [{ "name": "web", "path": "apps/web", "platform": "svelte" }],"ui": {"limits": {"""
            + limits + """}},"paths": {}}""");
    }

    /// <summary>Writes a project whose UI package names a design contract with a token file.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="tokens">The token file text, written beside the design contract.</param>
    static void WriteDesignContract(string root, string tokens)
    {
        var package = Path.Combine(root, "packages", "ui");
        Directory.CreateDirectory(Path.Combine(package, "tokens"));
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(Path.Combine(package, "tokens", "fixture.tokens.json"), tokens);
        File.WriteAllText(
            Path.Combine(package, "DESIGN.md"),
            """
            ---
            schemaVersion: 2
            package: "@fixture/ui"
            tokens: tokens/fixture.tokens.json
            pageFrames:
              list-page: ListPage
            ---

            # Fixture design contract
            """);
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ],
              "ui": { "package": { "designContract": "packages/ui/DESIGN.md" } },
              "paths": {}
            }
            """);
    }

    static ProjectModel BaseModel(string root)
    {
        if (!File.Exists(Path.Combine(root, ".litespec", "project.yaml")))
        {
            Directory.CreateDirectory(Path.Combine(root, ".litespec"));
            File.WriteAllText(
                Path.Combine(root, ".litespec", "project.yaml"),
                """
                {
                  "owners": ["lending-team"],
                  "frontends": [
                      { "name": "web", "path": "apps/web", "platform": "svelte" }
                    ], "paths": {
                  }
                }
                """);
        }

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec("entry-point", "entry-point/app", """{"kind":"browser"}""", root));
        model.Records.Add(Rec("use-case", "use-case/lending.list-loans", """{"kind":"query","reachedThrough":["entry-point/app"]}""", root));
        model.Records.Add(Rec("result", "result/list-loans.listed", """{"outcome":"success","meaning":"The loans are listed."}""", root));
        model.Records.Add(Rec("result", "result/list-loans.refused", """{"outcome":"refusal","meaning":"The listing is refused.","refusal":"refusal/lending.refused"}""", root));

        return model;
    }

    static Record Screen(string id, string route, IReadOnlyList<string> offers, IReadOnlyList<string> serves, IReadOnlyList<string>? leadsTo = null, string? pageFramePattern = "list-page", string? primaryAction = null) =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = "screen.md",
            Line = 1,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"route\":" + JsonSerializer.Serialize(route)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":" + JsonSerializer.Serialize(serves)
                + ",\"leadsTo\":" + JsonSerializer.Serialize(leadsTo ?? [])
                + PageFrameJson(pageFramePattern)
                + PrimaryActionJson(primaryAction) + "}").RootElement.Clone(),
        };

    static Record ScreenWithExplanation(string root, string id, string route, IReadOnlyList<string> offers, string explanation, string? pageFramePattern = "list-page") =>
        new()
        {
            Type = "screen",
            Id = id,
            Title = "Loans",
            File = Path.Combine(root, "screen.yaml"),
            Line = 1,
            ExplanationFile = Path.Combine(root, "screen.md"),
            Explanation = explanation,
            Raw = JsonDocument.Parse(
                "{\"entryPoint\":\"entry-point/app\",\"route\":" + JsonSerializer.Serialize(route)
                + ",\"offers\":" + JsonSerializer.Serialize(offers)
                + ",\"serves\":[],\"leadsTo\":[]"
                + PageFrameJson(pageFramePattern) + "}").RootElement.Clone(),
        };

    static string PageFrameJson(string? pattern) =>
        pattern is null
            ? ""
            : ",\"pageFrame\":{\"pattern\":" + JsonSerializer.Serialize(pattern) + ",\"option\":\"search-and-a-state-filter-only\"}";

    static string PrimaryActionJson(string? primary) =>
        primary is null ? "" : ",\"primaryAction\":" + JsonSerializer.Serialize(primary);

    static Record Rec(string type, string id, string fields, string root)
    {
        using var document = JsonDocument.Parse(fields);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = Path.Combine(root, $"{type}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }

    static Record Term(string id, string title, string fields, string root)
    {
        using var document = JsonDocument.Parse(fields);

        return new Record
        {
            Type = "term",
            Id = id,
            Title = title,
            File = Path.Combine(root, $"{id.Replace('/', '-')}.md"),
            Line = 1,
            Raw = document.RootElement.Clone(),
        };
    }
}
