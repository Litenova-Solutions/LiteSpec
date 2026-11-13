using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for the required sections of each page kind.</summary>
public sealed partial class ConsumerValidatorTests
{
    const string BriefPage = ".litespec/product/brief.yaml";
    const string RequirementPage = ".litespec/product/pricing.yaml";
    const string RequirementExplanation = ".litespec/product/pricing.md";
    const string DecisionPage = ".litespec/product/decisions/0001-launch.yaml";
    const string DecisionExplanation = ".litespec/product/decisions/0001-launch.md";
    const string ActorPage = ".litespec/behavior/actors/customer.yaml";
    const string ActorExplanation = ".litespec/behavior/actors/customer.md";
    const string TutorialPage = ".litespec/guide/first-run.md";
    const string HowToPage = ".litespec/guide/add-a-pack.md";
    const string CommandPage = ".litespec/tools/fixture-up.md";
    const string ReferencePage = ".litespec/reference/ports.md";
    const string ConfigurationPage = ".litespec/tools/configuration.md";
    const string UseCasePage = ".litespec/behavior/modules/orders/cancel-order.yaml";
    const string AggregateExplanation = ".litespec/behavior/modules/orders/order-claims/aggregate.md";
    const string ModuleExplanation = ".litespec/behavior/modules/orders/module.md";

    static string ReplaceFirst(string raw, string oldValue, string newValue)
    {
        var index = raw.IndexOf(oldValue, StringComparison.Ordinal);
        return string.Concat(raw.AsSpan(0, index), newValue, raw.AsSpan(index + oldValue.Length));
    }

    static string WithoutSection(string raw, string name)
    {
        return ReplaceFirst(raw, $"## {name}", "## Notes");
    }

    /// <summary>Product brief that omits its summary.</summary>
    /// <proves>litespec/core/intent#state-one-product</proves>
    [Fact(DisplayName = "product brief that omits its summary")]
    public void ProductBriefThatOmitsItsSummary()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, BriefPage, meta => meta.Remove("summary"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>Consumer findings carry their level and code.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact(DisplayName = "consumer findings carry their level and code")]
    public void ConsumerFindingsCarryTheirLevelAndCode()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, BriefPage, meta => meta.Remove("summary"));
        var problems = Problems(RunConsumer(folder.Path));
        Assert.NotEmpty(problems);
        Assert.All(problems, problem =>
        {
            Assert.Contains("[G]", problem);
            Assert.Contains("[record-invalid]", problem);
        });
    }

    /// <summary>Feature explanation without an example stays clean.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-offering</proves>
    [Fact(DisplayName = "feature explanation without an example stays clean")]
    public void FeatureExplanationWithoutAnExampleStaysClean()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, RequirementExplanation, raw => WithoutSection(raw, "Example"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Pricing feature that omits its offering.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-offering</proves>
    [Fact(DisplayName = "pricing feature that omits its offering")]
    public void PricingFeatureThatOmitsItsOffering()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, RequirementPage, meta => meta.Remove("offering"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'offering'");
    }

    /// <summary>Decision that omits its context.</summary>
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact(DisplayName = "decision that omits its context")]
    public void DecisionThatOmitsItsContext()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, DecisionExplanation, raw => WithoutSection(raw, "Context"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Context'");
    }

    /// <summary>Decision that omits its consequences.</summary>
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact(DisplayName = "decision that omits its consequences")]
    public void DecisionThatOmitsItsConsequences()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, DecisionPage, meta => meta.Remove("consequences"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'consequences'");
    }

    /// <summary>Decision without an options section.</summary>
    /// <proves>litespec/core/intent#shape-each-decision-page</proves>
    [Fact(DisplayName = "decision without an options section")]
    public void DecisionWithoutAnOptionsSection()
    {
        using var folder = ConsumerFixture.Create();
        AssertAbsent(RunConsumer(folder.Path), "requires an H2 'Options'");
    }

    /// <summary>Actor that omits its summary.</summary>
    /// <proves>litespec/core/access#write-a-summary-on-every-actor-and-role-page</proves>
    [Fact(DisplayName = "actor that omits its summary")]
    public void ActorThatOmitsItsSummary()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, ActorPage, meta => meta.Remove("summary"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>Aggregate without transitions and data sections.</summary>
    /// <proves>litespec/core/domain-model#connect-every-state-through-transitions</proves>
    [Fact(DisplayName = "aggregate without transitions and data sections")]
    public void AggregateWithoutTransitionsAndDataSections()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            AggregateExplanation,
            raw => WithoutSection(WithoutSection(raw, "Transitions"), "Data"));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Makes the page a query with an immediate output group.</summary>
    /// <param name="meta">The page record fields to mutate.</param>
    static void AsQuery(JsonObject meta)
    {
        meta["kind"] = "query";
        meta["output"] = ConsumerFixture.Meta("""{"include":{"rows":{"fields":[{"name":"claims","meaning":"One row per order claim."}],"classification":"internal","reads":["aggregate/orders.order-claim"],"freshness":{"mode":"immediate"}}}}""");
    }

    /// <summary>Drops every refusal result from the page's records, leaving successes.</summary>
    /// <param name="meta">The page record fields to mutate.</param>
    static void DropRefusals(JsonObject meta)
    {
        if (meta["records"] is not JsonArray records)
        {
            return;
        }

        for (var index = records.Count - 1; index >= 0; index--)
        {
            if (records[index] is JsonObject entry
                && (string?)entry["type"] == "result"
                && (string?)entry["outcome"] == "refusal")
            {
                records.RemoveAt(index);
            }
        }
    }

    /// <summary>Problem that omits its summary.</summary>
    /// <proves>litespec/core/intent#state-each-problem-with-its-evidence</proves>
    [Fact(DisplayName = "problem that omits its summary")]
    public void ProblemThatOmitsItsSummary()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/problems/slow-delivery.yaml",
            """{"type":"problem","id":"problem/slow-delivery","title":"Slow Delivery","statement":"Orders take weeks to arrive."}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>Feature that omits its offering.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-offering</proves>
    [Fact(DisplayName = "feature that omits its offering")]
    public void FeatureThatOmitsItsOffering()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/features/approval-reminders.yaml",
            """{"type":"feature","id":"feature/approval-reminders","title":"Approval Reminders","solves":["problem/slow-approval"],"advances":["goal/faster-delivery"]}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'offering'");
    }

    /// <summary>Constraint that omits its summary.</summary>
    /// <proves>litespec/core/intent#answer-every-constraint</proves>
    [Fact(DisplayName = "constraint that omits its summary")]
    public void ConstraintThatOmitsItsSummary()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/constraints/tax-retention.yaml",
            """{"type":"constraint","id":"constraint/tax-retention","title":"Tax Retention","authority":"Tax office guidance.","kind":"technical","applies":true,"appliesBecause":"The shop keeps receipts."}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>Research that omits its findings.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact(DisplayName = "research that omits its findings")]
    public void ResearchThatOmitsItsFindings()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/research/delivery-benchmarks.yaml",
            """{"type":"research","id":"research/delivery-benchmarks","title":"Delivery Benchmarks","question":"How fast do rivals deliver?","conclusion":"Rivals deliver in two days.","sources":[{"key":"delivery-benchmarks","url":"https://example.com/benchmarks","readOn":"2026-01-01"}]}""");
        TestRecords.Explain(folder.Path, ".litespec/research/delivery-benchmarks.md", "# Delivery Benchmarks\n");
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Findings'");
    }

    /// <summary>Journey that omits between the steps.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "journey that omits between the steps")]
    public void JourneyThatOmitsBetweenTheSteps()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/product/journeys/submit-and-reimburse.yaml",
            """{"type":"journey","id":"journey/submit-and-reimburse","title":"Submit and Reimburse","actor":"actor/customer","serves":["goal/faster-delivery"],"summary":"A customer submits a claim and is reimbursed.","finishedWhen":"The customer holds the reimbursement.","steps":[{"id":"s1","statement":"The customer submits the order.","actor":"actor/customer"}],"cites":["requirement/pricing.flat-fee"]}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'betweenSteps'");
    }

    /// <summary>Rule set that omits its summary.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "rule set that omits its summary")]
    public void RuleSetThatOmitsItsSummary()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/behavior/rules/spending-standards.yaml",
            """{"type":"rule-set","id":"spending-standards","title":"Spending Standards"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>External system that omits its summary.</summary>
    /// <proves>litespec/core/access#write-a-summary-on-every-external-system-page</proves>
    [Fact(DisplayName = "external system that omits its summary")]
    public void ExternalSystemThatOmitsItsSummary()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/behavior/systems/payout-bank.yaml",
            """{"type":"external-system","id":"external-system/payout-bank","title":"Payout Bank"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>Runbook that omits its escalation.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "runbook that omits its escalation")]
    public void RunbookThatOmitsItsEscalation()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/runbooks/clear-stuck-reimbursements.yaml",
            """{"type":"runbook","id":"runbook/clear-stuck-reimbursements","title":"Clear Stuck Reimbursements","trigger":"A reimbursement sits past its date.","impact":"Stuck claims block payment.","prerequisites":"The reader holds the payments role.","procedure":"Clear the stuck claim and rerun payment.","verification":"The claim leaves the stuck list.","recovery":"Restore the claim from its snapshot."}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'escalation'");
    }
    /// <summary>Lasting departure that omits its reason.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "departure that omits its reason")]
    public void DepartureThatOmitsItsReason()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/governance/departures/export-review-bypass.yaml",
            """{"type":"departure","id":"departure/export-review-bypass","title":"Export Review Bypass","covers":["litespec/core/scope#select-a-size-a-shape-and-stacks"],"subject":["use-case/orders.cancel-order"]}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'reason'");
    }

    /// <summary>Gap departure that omits its exit.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "gap departure that omits its exit")]
    public void GapDepartureThatOmitsItsExit()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/governance/departures/export-unimplemented.yaml",
            """{"type":"departure","id":"departure/export-unimplemented","title":"Export Unimplemented","covers":["behavior-unrecorded"],"subject":["use-case/orders.cancel-order"],"reason":"The export ships later.","expiresOn":"2027-01-01"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'exit'");
    }

    /// <summary>Gap departure carrying its exit has no H2 requirement.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "gap departure carrying its exit")]
    public void GapDepartureCarryingItsExit()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            ".litespec/governance/departures/export-review-bypass.yaml",
            """{"type":"departure","id":"departure/export-review-bypass","title":"Export Review Bypass","covers":["litespec/core/scope#select-a-size-a-shape-and-stacks"],"subject":["use-case/orders.cancel-order"],"reason":"The export runs once.","consequences":"The export runs once outside review.","expiresOn":"2027-01-01","exit":"The export moves into review."}""");
        TestRecords.Write(
            folder.Path,
            ".litespec/governance/departures/export-unimplemented.yaml",
            """{"type":"departure","id":"departure/export-unimplemented","title":"Export Unimplemented","covers":["behavior-unrecorded"],"subject":["use-case/orders.cancel-order"],"reason":"The export ships later.","exit":"The export ships before the date.","expiresOn":"2027-01-01"}""");
        AssertAbsent(RunConsumer(folder.Path), "requires an H2");
    }

    /// <summary>Example cast that omits its people.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "example cast that omits its people")]
    public void ExampleCastThatOmitsItsPeople()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            ".litespec/example-cast.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"example-cast","id":"example-cast","title":"Example Cast"}"""),
                "# Example Cast\n\n## The Organization\n\nThe shop sells printed books.\n\n## The Occasion\n\nAlex orders on Monday morning.\n\n## The Amounts\n\nEvery order carries one fee.\n\n## Recurring Situations\n\nShipments leave each weekday.\n"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'The People'");
    }

    /// <summary>Tutorial that omits what the reader built.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "tutorial that omits what the reader built")]
    public void TutorialThatOmitsWhatTheReaderBuilt()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, TutorialPage, raw => WithoutSection(raw, "What You Built"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'What You Built'");
    }

    /// <summary>How-to with no check.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "how-to with no check")]
    public void HowToWithNoCheck()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, HowToPage, raw => WithoutSection(raw, "Check"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Check'");
    }

    /// <summary>Command page hiding its mechanism.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "command page hiding its mechanism")]
    public void CommandPageHidingItsMechanism()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(
            folder.Path,
            CommandPage,
            raw => raw.Substring(0, raw.IndexOf("## Underneath", StringComparison.Ordinal)));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Underneath'");
    }

    /// <summary>Reference page with no reference section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "reference page with no reference section")]
    public void ReferencePageWithNoReferenceSection()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ReferencePage, raw => ReplaceFirst(raw, "## Reference", "## Procedure"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Reference'");
    }

    /// <summary>Configuration page with no precedence.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "configuration page with no precedence")]
    public void ConfigurationPageWithNoPrecedence()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ConfigurationPage, raw => ReplaceFirst(raw, "## Precedence", "## Notes"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Precedence'");
    }

    /// <summary>Module page keeps free body prose.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "module page keeps free body prose")]
    public void ModulePageKeepsFreeBodyProse()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ModuleExplanation, raw => $"{raw}\nA note with no heading.\n");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Page title outside Title Case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "page title outside Title Case")]
    public void PageTitleOutsideTitleCase()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ActorExplanation, raw => ReplaceFirst(raw, "# Customer", "# customer"));
        AssertProblem(RunConsumer(folder.Path), "H1 'customer' must use Title Case");
    }

    /// <summary>Section heading outside Title Case.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "section heading outside Title Case")]
    public void SectionHeadingOutsideTitleCase()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ModuleExplanation, raw => $"{raw}\n## known limits\n\nThe module stops at the shop edge.\n");
        AssertProblem(RunConsumer(folder.Path), "H2 'known limits' must use Title Case");
    }

    /// <summary>Small word capitalized in the middle.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "small word capitalized in the middle")]
    public void SmallWordCapitalizedInTheMiddle()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ActorExplanation, raw => ReplaceFirst(raw, "# Customer", "# Customer Of The Shop"));
        AssertProblem(RunConsumer(folder.Path), "must use Title Case");
    }

    /// <summary>Page headings carry their line.</summary>
    /// <proves>litespec/core/authoring#use-controlled-capitalization</proves>
    [Fact(DisplayName = "page headings carry their line")]
    public void PageHeadingsCarryTheirLine()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, ModuleExplanation, raw => $"{raw}\n## known limits\n\nThe module stops at the shop edge.\n");
        var problems = Problems(RunConsumer(folder.Path));
        Assert.Contains(problems, problem => problem.Contains(".litespec/behavior/modules/orders/module.md:", StringComparison.Ordinal)
            && problem.Contains("[page-heading-invalid]", StringComparison.Ordinal));
    }

    /// <summary>Example longer than the bound.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "example longer than the bound")]
    public void ExampleLongerThanTheBound()
    {
        using var folder = ConsumerFixture.Create();
        var longExample = string.Join(" ", Enumerable.Repeat("word", 121));
        ConsumerFixture.MutateBody(
            folder.Path,
            RequirementExplanation,
            raw => ReplaceFirst(raw, "Alex orders two books on Monday and pays one fee at checkout.", longExample));
        AssertProblem(RunConsumer(folder.Path), "over the 120 word bound");
    }
}
