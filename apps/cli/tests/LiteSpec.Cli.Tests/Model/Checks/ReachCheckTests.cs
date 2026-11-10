using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the reach checks against hand-built models.</summary>
public sealed class ReachCheckTests
{
    /// <summary>Problem Without Solver Reports Intent Unreached.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Problem_Without_Solver_Reports_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Problem Solved By Reaching Feature Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Problem_Solved_By_Reaching_Feature_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "borrowing.yaml"), """{"solves": ["problem/ghost-pain"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Problem Cited By Reaching Requirement Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Problem_Cited_By_Reaching_Requirement_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "cites": ["problem/ghost-pain"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Feature Without Reaching Requirement Reports Intent Unreached.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Feature_Without_Reaching_Requirement_Reports_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "borrowing.yaml"), """{"solves": ["problem/ghost-pain"]}"""),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Feature With Reaching Requirement Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Feature_With_Reaching_Requirement_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(temp.Path, "borrowing.yaml"), """{"solves": ["problem/ghost-pain"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Requirement Without Fulfiller Reports Intent Unreached.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Requirement_Without_Fulfiller_Reports_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Requirement Fulfilled Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Requirement_Fulfilled_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Requirement Cited By Journey Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Requirement_Cited_By_Journey_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"), """{"cites": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Research Without Citer Reports Intent Unreached.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Research_Without_Citer_Reports_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/ghost-study", Path.Combine(temp.Path, "ghost-study.yaml"), "{}"));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Research Cited By Reaching Requirement Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Research_Cited_By_Reaching_Requirement_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/ghost-study", Path.Combine(temp.Path, "ghost-study.yaml"), "{}"),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "cites": ["research/ghost-study"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Research Cited By Served Journey Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Research_Cited_By_Served_Journey_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/ghost-study", Path.Combine(temp.Path, "ghost-study.yaml"), "{}"),
            TestModel.Record("journey", "journey/borrow-and-return", Path.Combine(temp.Path, "borrow-and-return.yaml"), """{"cites": ["research/ghost-study"], "steps": [{"id": "s1", "statement": "Walk."}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"serves": ["journey/borrow-and-return#s1"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Decision Deciding Unreached Record Reports Intent Unreached.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Decision_Deciding_Unreached_Record_Reports_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"decides": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""));

        Assert.Contains("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>A use case another use case runs needs no journey of its own.</summary>
    /// <proves>litespec/core/model#connect-every-behavior-record-to-intent</proves>
    [Fact]
    public void Use_Case_Run_By_Another_Skips_Journey_Missing()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), """{"owners": ["lending-team"], "size": "large"}""");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "serves": ["journey/borrow#s1"], "effects": [{"runs": "use-case/lending.hold-copy", "timing": "inline", "sharesTransaction": true}]}"""),
            TestModel.Record("use-case", "use-case/lending.hold-copy", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command"}"""));

        var findings = Checker.Check(model).Findings;

        Assert.DoesNotContain(findings, finding => finding.Code == "use-case-journey-missing" && finding.Subject == "use-case/lending.hold-copy");
        Assert.DoesNotContain(findings, finding => finding.Code == "use-case-journey-missing" && finding.Subject == "use-case/lending.borrow-copy");
    }

    /// <summary>A technical decision under a module skips the Intent reach rule.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Module_Technical_Decision_Skips_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var decisions = Path.Combine(temp.Path, ".litespec", "implementation", "modules", "lending", "decisions");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(decisions, "0001-ghost-days.yaml"), """{"decides": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "intent-unreached" && finding.Subject == "decision/0001-ghost-days");
    }

    /// <summary>Decision Deciding Reaching Record Passes.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Decision_Deciding_Reaching_Record_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"decides": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
    }

    /// <summary>Decision Without Decides Skips Intent Unreached.</summary>
    /// <proves>litespec/core/model#name-what-every-decision-decides</proves>
    [Fact]
    public void Decision_Without_Decides_Skips_Intent_Unreached()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), "{}"));

        Assert.DoesNotContain("intent-unreached", TestModel.Codes(model));
        Assert.Contains("decision-decides-missing", TestModel.Codes(model));
    }

    /// <summary>Role Without Holder Reports Role Orphan.</summary>
    /// <proves>litespec/core/access#record-every-role-hold-with-its-scope</proves>
    [Fact]
    public void Role_Without_Holder_Reports_Role_Orphan()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"scope": "self", "allows": ["permission/read-loans"]}"""));

        Assert.Contains("role-orphaned", TestModel.Codes(model));
    }

    /// <summary>Role Held Passes.</summary>
    /// <proves>litespec/core/access#record-every-role-hold-with-its-scope</proves>
    [Fact]
    public void Role_Held_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person", "holds": [{"role": "role/reader", "scope": "self"}]}"""),
            TestModel.Record("role", "role/reader", Path.Combine(temp.Path, "reader-role.yaml"), """{"scope": "self", "allows": ["permission/read-loans"]}"""),
            TestModel.Record("permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"), "{}"));

        Assert.DoesNotContain("role-orphaned", TestModel.Codes(model));
    }

    /// <summary>Permission Without Role Reports Permission Orphan.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void Permission_Without_Role_Reports_Permission_Orphan()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("permission", "permission/ghost-read", Path.Combine(temp.Path, "ghost-read.yaml"), "{}"));

        Assert.Contains("permission-orphaned", TestModel.Codes(model));
    }

    /// <summary>Permission Allowed Passes.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void Permission_Allowed_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/reader", Path.Combine(temp.Path, "reader.yaml"), """{"scope": "self", "allows": ["permission/read-loans"]}"""),
            TestModel.Record("permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"), "{}"));

        Assert.DoesNotContain("permission-orphaned", TestModel.Codes(model));
    }

    /// <summary>Two Decisions Deciding One Record Report Decision Conflict.</summary>
    /// <proves>litespec/core/model#follow-the-authority-order</proves>
    [Fact]
    public void Two_Decisions_Deciding_One_Record_Report_Decision_Conflict()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"decides": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("decision", "decision/0002-ghost-weeks", Path.Combine(temp.Path, "ghost-weeks.yaml"), """{"decides": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""));

        var conflicts = Checker.Check(model).Findings.Where(finding => finding.Code == "decision-decides-duplicate").OrderBy(finding => finding.Subject, StringComparer.Ordinal).ToList();

        Assert.Equal(2, conflicts.Count);
        Assert.Equal("Decision 'decision/0001-ghost-days' decides 'requirement/borrowing.max-loans', which another decision also decides.", conflicts[0].Message);
        Assert.Equal("Decision 'decision/0002-ghost-weeks' decides 'requirement/borrowing.max-loans', which another decision also decides.", conflicts[1].Message);
    }

    /// <summary>Single Decision Passes.</summary>
    /// <proves>litespec/core/model#follow-the-authority-order</proves>
    [Fact]
    public void Single_Decision_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-ghost-days", Path.Combine(temp.Path, "ghost-days.yaml"), """{"decides": ["requirement/borrowing.max-loans"]}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""));

        Assert.DoesNotContain("decision-decides-duplicate", TestModel.Codes(model));
    }

    /// <summary>Intent Unreached Message Names Record.</summary>
    /// <proves>litespec/core/model#reach-behavior-from-every-intent-record</proves>
    [Fact]
    public void Intent_Unreached_Message_Names_Record()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "intent-unreached");
        Assert.Equal("Problem 'problem/ghost-pain' reaches no Behavior.", finding.Message);
        Assert.Equal("problem/ghost-pain", finding.Subject);
    }

    /// <summary>Role Orphan Message Names Role.</summary>
    /// <proves>litespec/core/access#record-every-role-hold-with-its-scope</proves>
    [Fact]
    public void Role_Orphan_Message_Names_Role()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"scope": "self", "allows": ["permission/read-loans"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "role-orphaned");
        Assert.Equal("Role 'role/ghost' is held by no actor.", finding.Message);
        Assert.Equal("role/ghost", finding.Subject);
    }

    /// <summary>Permission Orphan Message Names Permission.</summary>
    /// <proves>litespec/core/access#grant-permissions-through-roles</proves>
    [Fact]
    public void Permission_Orphan_Message_Names_Permission()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("permission", "permission/ghost-read", Path.Combine(temp.Path, "ghost-read.yaml"), "{}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "permission-orphaned");
        Assert.Equal("Permission 'permission/ghost-read' is allowed by no role.", finding.Message);
        Assert.Equal("permission/ghost-read", finding.Subject);
    }

    /// <summary>Parked Intent Unreached Reports Known.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void Parked_Intent_Unreached_Reports_Known()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("departure", "departure/ghost-pain-parked", Path.Combine(temp.Path, "ghost-pain-parked.yaml"), """{"covers": ["intent-unreached"], "subject": ["problem/ghost-pain"], "reason": "Ghost work waits.", "exit": "The gap closes.", "expiresOn": "2999-01-01"}"""));

        var (findings, known) = Checker.Check(model);

        Assert.DoesNotContain(findings, finding => finding.Code == "intent-unreached");
        Assert.Contains(known, finding => finding.Code == "intent-unreached");
    }
}
