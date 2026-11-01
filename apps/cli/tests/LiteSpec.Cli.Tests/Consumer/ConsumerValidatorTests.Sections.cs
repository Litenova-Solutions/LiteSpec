using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for the required sections of each page kind.</summary>
public sealed partial class ConsumerValidatorTests
{
    const string BriefPage = "docs/product/brief.yaml";
    const string RequirementPage = "docs/product/pricing.yaml";
    const string RequirementExplanation = "docs/product/pricing.md";
    const string DecisionPage = "docs/product/decisions/0001-launch.yaml";
    const string DecisionExplanation = "docs/product/decisions/0001-launch.md";
    const string ActorPage = "docs/behavior/actors/customer.yaml";
    const string ActorExplanation = "docs/behavior/actors/customer.md";
    const string TutorialPage = "docs/guide/first-run.md";
    const string HowToPage = "docs/guide/add-a-pack.md";
    const string CommandPage = "docs/tools/fixture-up.md";
    const string ReferencePage = "docs/reference/ports.md";
    const string ConfigurationPage = "docs/tools/configuration.md";
    const string UseCasePage = "docs/behavior/modules/orders/cancel-order.yaml";
    const string AggregateExplanation = "docs/behavior/modules/orders/order-claims/aggregate.md";
    const string ModuleExplanation = "docs/behavior/modules/orders/module.md";

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

    /// <summary>Consumer findings carry their level.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact(DisplayName = "consumer findings carry their level")]
    public void ConsumerFindingsCarryTheirLevel()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, BriefPage, meta => meta.Remove("summary"));
        var problems = Problems(RunConsumer(folder.Path));
        Assert.NotEmpty(problems);
        Assert.All(problems, problem => Assert.StartsWith("[G] ", problem));
    }

    /// <summary>Requirement page that omits its example.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact(DisplayName = "requirement page that omits its example")]
    public void RequirementPageThatOmitsItsExample()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateBody(folder.Path, RequirementExplanation, raw => WithoutSection(raw, "Example"));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Example'");
    }

    /// <summary>Requirement page that omits its position.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact(DisplayName = "requirement page that omits its position")]
    public void RequirementPageThatOmitsItsPosition()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, RequirementPage, meta => meta.Remove("position"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'position'");
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

    /// <summary>Significant query without what happens.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-significant-use-cases</proves>
    [Fact(DisplayName = "significant query without what happens")]
    public void SignificantQueryWithoutWhatHappens()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            UseCasePage,
            meta =>
            {
                AsQuery(meta);
                meta.Remove("whatHappens");
            });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Plain use case without goal and what happens.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-significant-use-cases</proves>
    [Fact(DisplayName = "plain use case without goal and what happens")]
    public void PlainUseCaseWithoutGoalAndWhatHappens()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            UseCasePage,
            meta =>
            {
                DropRefusals(meta);
                meta.Remove("purpose");
                meta.Remove("whatHappens");
            });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Retired use case without goal and what happens.</summary>
    /// <proves>litespec/core/use-cases#write-prose-on-significant-use-cases</proves>
    [Fact(DisplayName = "retired use case without goal and what happens")]
    public void RetiredUseCaseWithoutGoalAndWhatHappens()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            UseCasePage,
            meta =>
            {
                meta["status"] = "retired";
                meta.Remove("purpose");
                meta.Remove("whatHappens");
            });
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

    /// <summary>Goal that omits why it matters.</summary>
    /// <proves>litespec/core/intent#state-each-goal-with-its-measure</proves>
    [Fact(DisplayName = "goal that omits why it matters")]
    public void GoalThatOmitsWhyItMatters()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/product/goals/faster-delivery.yaml",
            """{"type":"goal","id":"goal/faster-delivery","title":"Faster Delivery","owner":"fixture","status":"approved","statement":"Orders arrive in two days.","measure":"Median days to arrival.","target":"Two days.","dueBy":"2026-12-31","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'whyItMatters'");
    }

    /// <summary>Problem that omits its situation.</summary>
    /// <proves>litespec/core/intent#state-each-problem-with-its-evidence</proves>
    [Fact(DisplayName = "problem that omits its situation")]
    public void ProblemThatOmitsItsSituation()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/product/problems/slow-delivery.yaml",
            """{"type":"problem","id":"problem/slow-delivery","title":"Slow Delivery","owner":"fixture","status":"approved","statement":"Orders take weeks to arrive.","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'situation'");
    }

    /// <summary>Feature that omits its offering.</summary>
    /// <proves>litespec/core/intent#state-each-feature-with-its-lifecycle</proves>
    [Fact(DisplayName = "feature that omits its offering")]
    public void FeatureThatOmitsItsOffering()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/product/features/approval-reminders.yaml",
            """{"type":"feature","id":"feature/approval-reminders","title":"Approval Reminders","owner":"fixture","status":"approved","lifecycle":"shipped","solves":["problem/slow-approval"],"advances":["goal/faster-delivery"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
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
            "docs/product/constraints/tax-retention.yaml",
            """{"type":"constraint","id":"constraint/tax-retention","title":"Tax Retention","owner":"fixture","status":"approved","authority":"Tax office guidance.","kind":"technical","applies":true,"appliesBecause":"The shop keeps receipts.","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'summary'");
    }

    /// <summary>Research that omits its answer.</summary>
    /// <proves>litespec/core/intent#record-research-with-read-sources</proves>
    [Fact(DisplayName = "research that omits its answer")]
    public void ResearchThatOmitsItsAnswer()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/research/delivery-benchmarks.yaml",
            """{"type":"research","id":"research/delivery-benchmarks","title":"Delivery Benchmarks","owner":"fixture","status":"approved","question":"How fast do rivals deliver?","conclusion":"Rivals deliver in two days.","sources":[{"key":"delivery-benchmarks","url":"https://example.com/benchmarks","readOn":"2026-01-01"}],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        TestRecords.Explain(folder.Path, "docs/research/delivery-benchmarks.md", "# Delivery Benchmarks\n");
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Answer'");
    }

    /// <summary>Journey that omits between the steps.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "journey that omits between the steps")]
    public void JourneyThatOmitsBetweenTheSteps()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/product/journeys/submit-and-reimburse.yaml",
            """{"type":"journey","id":"journey/submit-and-reimburse","title":"Submit and Reimburse","owner":"fixture","status":"approved","actor":"actor/customer","serves":["goal/faster-delivery"],"finishedWhen":"The customer holds the reimbursement.","steps":[{"id":"s1","statement":"The customer submits the order.","actor":"actor/customer"}],"cites":["requirement/pricing.flat-fee"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
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
            "docs/behavior/rules/spending-standards.yaml",
            """{"type":"rule-set","id":"spending-standards","title":"Spending Standards","owner":"fixture","status":"approved"}""");
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
            "docs/behavior/systems/payout-bank.yaml",
            """{"type":"external-system","id":"external-system/payout-bank","title":"Payout Bank","owner":"fixture","status":"reviewed","actor":"actor/customer","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
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
            "docs/runbooks/clear-stuck-reimbursements.yaml",
            """{"type":"runbook","id":"runbook/clear-stuck-reimbursements","title":"Clear Stuck Reimbursements","owner":"fixture","status":"reviewed","trigger":"A reimbursement sits past its date.","impact":"Stuck claims block payment.","prerequisites":"The reader holds the payments role.","procedure":"Clear the stuck claim and rerun payment.","verification":"The claim leaves the stuck list.","recovery":"Restore the claim from its snapshot.","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'escalation'");
    }

    /// <summary>Departure that omits its consequences.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "departure that omits its consequences")]
    public void DepartureThatOmitsItsConsequences()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/governance/exceptions/export-review-bypass.yaml",
            """{"type":"exception","id":"exception/export-review-bypass","title":"Export Review Bypass","owner":"fixture","status":"approved","kind":"departure","covers":"litespec/core/scope#select-a-size-a-shape-and-stacks","subject":["use-case/orders.cancel-order"],"reason":"The export runs once.","expiresOn":"2027-01-01","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'consequences'");
    }

    /// <summary>Gap that omits its exit.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "gap that omits its exit")]
    public void GapThatOmitsItsExit()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/governance/exceptions/export-unimplemented.yaml",
            """{"type":"exception","id":"exception/export-unimplemented","title":"Export Unimplemented","owner":"fixture","status":"approved","kind":"gap","covers":["behavior-unrecorded"],"subject":["use-case/orders.cancel-order"],"reason":"The export ships later.","expiresOn":"2027-01-01","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        AssertProblem(RunConsumer(folder.Path), "missing required property 'exit'");
    }

    /// <summary>Exception carrying its kind's section.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "exception carrying its kind's section")]
    public void ExceptionCarryingItsKindsSection()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            "docs/governance/exceptions/export-review-bypass.yaml",
            """{"type":"exception","id":"exception/export-review-bypass","title":"Export Review Bypass","owner":"fixture","status":"approved","kind":"departure","covers":"litespec/core/scope#select-a-size-a-shape-and-stacks","subject":["use-case/orders.cancel-order"],"reason":"The export runs once.","consequences":"The export runs once outside review.","expiresOn":"2027-01-01","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
        TestRecords.Write(
            folder.Path,
            "docs/governance/exceptions/export-unimplemented.yaml",
            """{"type":"exception","id":"exception/export-unimplemented","title":"Export Unimplemented","owner":"fixture","status":"approved","kind":"gap","covers":["behavior-unrecorded"],"subject":["use-case/orders.cancel-order"],"reason":"The export ships later.","exit":"The export ships before the date.","expiresOn":"2027-01-01","approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}""");
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
            "docs/example-cast.md",
            ConsumerFixture.Page(
                ConsumerFixture.Meta("""{"type":"example-cast","id":"example-cast","title":"Example Cast","owner":"fixture","status":"approved"}"""),
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

    /// <summary>Example longer than the bound.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
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

    /// <summary>Example bound below forty.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact(DisplayName = "example bound below forty")]
    public void ExampleBoundBelowForty()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["exampleWordLimit"] = 20);
        AssertProblem(RunConsumer(folder.Path), "exampleWordLimit '20' is below 40");
    }

    /// <summary>Example bound the project raises.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact(DisplayName = "example bound the project raises")]
    public void ExampleBoundTheProjectRaises()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["exampleWordLimit"] = 50);
        var longExample = string.Join(" ", Enumerable.Repeat("word", 60));
        ConsumerFixture.MutateBody(
            folder.Path,
            RequirementExplanation,
            raw => ReplaceFirst(raw, "Alex orders two books on Monday and pays one fee at checkout.", longExample));
        AssertProblem(RunConsumer(folder.Path), "over the 50 word bound");
    }
}
