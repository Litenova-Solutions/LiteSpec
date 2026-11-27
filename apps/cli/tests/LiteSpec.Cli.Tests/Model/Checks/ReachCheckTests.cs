using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the reach checks against hand-built models.</summary>
public sealed class ReachCheckTests
{
    /// <summary>Problem Without Solver Reports Intent Unreached.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void ProblemWithoutSolverReportsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Problem Solved By Reaching Feature Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void ProblemSolvedByReachingFeaturePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "problems", "ghost-pain", "features", "borrowing", "borrowing.yaml"), """{}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Problem Cited By Reaching Requirement Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void ProblemCitedByReachingRequirementPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["problem/ghost-pain"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Feature Without Reaching Requirement Reports Intent Unreached.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void FeatureWithoutReachingRequirementReportsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "borrowing.yaml"), """{}"""),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Feature With Reaching Requirement Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void FeatureWithReachingRequirementPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "borrowing.yaml"), """{}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Requirement Without Fulfiller Reports Intent Unreached.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void RequirementWithoutFulfillerReportsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Requirement Fulfilled Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void RequirementFulfilledPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Requirement Cited By Flow Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void RequirementCitedByFlowPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            .. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"), """{"steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "A ghost borrows a copy.", "carries": ["requirement/borrowing.max-loans"]}]}""")]);

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Quality Requirement With A Check Reaches Behavior.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void QualityRequirementWithACheckReachesBehavior()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.list-loans-latency", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans", "check": "route-suite"}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Quality Requirement Proved By A Test Reaches Behavior.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void QualityRequirementProvedByATestReachesBehavior()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.list-loans-latency", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans"}"""));
        model.Proves.Add(new ProvesLink("BorrowingStaysFast", "requirement/borrowing.list-loans-latency", true, true, false));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Quality Requirement With No Check And No Proving Test Reports Intent Unreached.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void QualityRequirementWithNoCheckAndNoProvingTestReportsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.list-loans-latency", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans"}"""));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Research Without Citer Reports Intent Unreached.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void ResearchWithoutCiterReportsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/ghost-study", Path.Combine(temp.Path, "ghost-study.yaml"), "{}"));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Research Cited By Reaching Requirement Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void ResearchCitedByReachingRequirementPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/ghost-study", Path.Combine(temp.Path, "ghost-study.yaml"), "{}"),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["research/ghost-study"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Decision Settling An Unreached Record Reports Intent Unreached.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void DecisionSettlingUnreachedRecordReportsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"choice": "Seven days"}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["decision/0001-ghost-days"]}"""));

        Assert.Contains(Checker.Check(model).Findings, finding => finding.Code == "intent-unreached" && finding.Subject == "decision/0001-ghost-days");
    }

    /// <summary>A use case a reaction runs needs no flow of its own.</summary>
    /// <proves>litespec/core/model#connect-every-behavior-record-to-intent</proves>
    [Fact]
    public void UseCaseRunByAReactionSkipsFlowMissing()
    {
        using var temp = TestCli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), """{"owners": ["lending-team"], "size": "large"}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "serves": ["step/borrow.borrow-copy"]}"""),
            TestModel.Record("reaction", "reaction/lending.hold-on-borrow", Path.Combine(temp.Path, "hold.yaml"), """{"triggeredBy": ["domain-event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.hold-copy", "timing": "async"}]}"""),
            TestModel.Record("use-case", "use-case/lending.hold-copy", Path.Combine(temp.Path, "hold-copy.yaml"), """{"kind": "command"}"""));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "use-case-flow-missing" && finding.Subject == "use-case/lending.hold-copy");
        Assert.DoesNotContain(findings, finding => finding.Code == "use-case-flow-missing" && finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>A technical decision under a module skips the Intent reach rule.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void ModuleTechnicalDecisionSkipsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var decisions = Path.Combine(temp.Path, ".litespec", "implementation", "modules", "lending", "decisions");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(decisions, "0001-ghost-days.yaml"), """{"choice": "Seven days"}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["decision/0001-ghost-days"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "intent-unreached" && finding.Subject == "decision/0001-ghost-days");
    }

    /// <summary>Decision Settling A Reaching Record Passes.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void DecisionSettlingReachingRecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"choice": "Seven days"}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["decision/0001-ghost-days"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Decision Settling Nothing Skips Intent Unreached.</summary>
    /// <proves>litespec/core/model#name-what-every-decision-decides</proves>
    [Fact]
    public void DecisionSettlingNothingSkipsIntentUnreached()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), "{}"));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
        Assert.Contains("decision-decides-missing", TestModel.Codes(model));
    }

    /// <summary>Role Without Holder Reports Role Orphan.</summary>
    /// <proves>litespec/core/access#record-every-role-an-actor-holds</proves>
    [Fact]
    public void RoleWithoutHolderReportsRoleOrphan()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"scope": "self"}"""));

        Assert.Contains("role-unused", TestModel.Codes(model));
    }

    /// <summary>Role Named By No Permission Reports Role Unused.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void RoleNamedByNoPermissionReportsRoleUnused()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person", "holds": ["role/reader"]}"""),
            TestModel.Record("role", "role/reader", Path.Combine(temp.Path, "reader-role.yaml"), """{"scope": "self"}"""),
            TestModel.Record("permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"), """{"meaning": "Read own loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "role-unused");

        Assert.Equal("Role 'role/reader' is named by no permission.", finding.Message);
        Assert.Equal("role/reader", finding.Subject);
    }

    /// <summary>Role Held Passes.</summary>
    /// <proves>litespec/core/access#record-every-role-an-actor-holds</proves>
    [Fact]
    public void RoleHeldPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person", "holds": ["role/reader"]}"""),
            TestModel.Record("role", "role/reader", Path.Combine(temp.Path, "reader-role.yaml"), """{"scope": "self"}"""),
            TestModel.Record("permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"), """{"meaning": "Read own loans.", "grantedTo": ["role/reader"]}"""));

        Assert.DoesNotContain("role-unused", TestModel.Codes(model));
    }

    /// <summary>Permission Without Role Reports Permission Orphan.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void PermissionWithoutRoleReportsPermissionOrphan()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("permission", "permission/ghost-read", Path.Combine(temp.Path, "ghost-read.yaml"), "{}"));

        Assert.Contains("permission-unused", TestModel.Codes(model));
    }

    /// <summary>Permission Granted Passes.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void PermissionGrantedPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/reader", Path.Combine(temp.Path, "reader.yaml"), """{"scope": "self"}"""),
            TestModel.Record("permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"), """{"meaning": "Read own loans.", "grantedTo": ["role/reader"]}"""));

        Assert.DoesNotContain("permission-unused", TestModel.Codes(model));
    }

    /// <summary>Two decisions deciding one record pass.</summary>
    /// <proves>litespec/core/model#name-what-every-decision-decides</proves>
    [Fact]
    public void TwoDecisionsDecidingOneRecordPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"choice": "Seven days"}"""),
            TestModel.Record("decision", "decision/0002-ghost-weeks", Path.Combine(temp.Path, "ghost-weeks.yaml"), """{"choice": "Fourteen days"}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["decision/0001-ghost-days", "decision/0002-ghost-weeks"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Intent Unreached Message Names Record.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact]
    public void IntentUnreachedMessageNamesRecord()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "intent-unreached");
        Assert.Equal("Problem 'problem/ghost-pain' reaches no Behavior.", finding.Message);
        Assert.Equal("problem/ghost-pain", finding.Subject);
    }

    /// <summary>Role Orphan Message Names Role.</summary>
    /// <proves>litespec/core/access#record-every-role-an-actor-holds</proves>
    [Fact]
    public void RoleOrphanMessageNamesRole()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"scope": "self"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "role-unused");
        Assert.Equal("Role 'role/ghost' is held by no actor.", finding.Message);
        Assert.Equal("role/ghost", finding.Subject);
    }

    /// <summary>Permission Orphan Message Names Permission.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void PermissionOrphanMessageNamesPermission()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("permission", "permission/ghost-read", Path.Combine(temp.Path, "ghost-read.yaml"), "{}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "permission-unused");
        Assert.Equal("Permission 'permission/ghost-read' is granted by no role.", finding.Message);
        Assert.Equal("permission/ghost-read", finding.Subject);
    }

    /// <summary>Parked Intent Unreached Reports Known.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void ParkedIntentUnreachedReportsKnown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("waiver", "waiver/ghost-pain-parked", Path.Combine(temp.Path, "ghost-pain-parked.yaml"), """{"kind": "gap", "waives": ["intent-unreached"], "appliesTo": ["problem/ghost-pain"], "reason": "Ghost work waits.", "closedBy": ["problem/ghost-pain"], "exitWhen": "The gap closes.", "expiresOn": "2999-01-01"}"""));

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "intent-unreached");
        Assert.Contains(known, finding => finding.Code == "intent-unreached");
    }
}
