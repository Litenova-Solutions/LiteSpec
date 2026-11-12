using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Tables;

/// <summary>Proves a reaction or job links its record through its mark.</summary>
public sealed class ImplementsLinkTests
{
    /// <summary>Reaction Without A Tag But With An Implements Link Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ReactionWithoutATagButWithAnImplementsLinkPasses()
    {
        using var temp = TestCli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"kind": "command"}"""));
        model.Code.Reactions.Add(new CodeReaction(
            "GhostReaction", Path.Combine(temp.Path, "GhostReaction.cs"), 3, null));
        model.Implements.Add(new ImplementsLink("GhostReaction.cs", "use-case/lending.ghost", "reaction"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Reaction Implementing Its Reaction Record Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void ReactionImplementingItsReactionRecordPasses()
    {
        using var temp = TestCli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-notice-on-loan-overdue", Path.Combine(temp.Path, "send-notice-on-loan-overdue.yaml"), """{"triggeredBy": ["event/loan.overdue"]}"""));
        model.Code.Reactions.Add(new CodeReaction(
            "SendNoticeOnLoanOverdueHandler", Path.Combine(temp.Path, "SendNoticeOnLoanOverdueHandler.cs"), 3, null));
        model.Implements.Add(new ImplementsLink("SendNoticeOnLoanOverdueHandler.cs", "reaction/lending.send-notice-on-loan-overdue", "reaction"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Reaction Marking A Record Of Another Kind Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void ReactionMarkingARecordOfAnotherKindReportsMarkUnresolved()
    {
        using var temp = TestCli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Open", "meaning": "The loan is open."}]}"""));
        var file = Path.Combine(temp.Path, "SendNoticeOnLoanOverdueHandler.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "SendNoticeOnLoanOverdueHandler", null, file, 3,
            [new CodeMark("aggregate/lending.loan", MarkStyle.Xml, MarkVerb.Implements, file, 2)],
            InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-unknown", codes);
    }

    /// <summary>Job Without A Tag But With An Implements Link Passes.</summary>
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void JobWithoutATagButWithAnImplementsLinkPasses()
    {
        using var temp = TestCli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"kind": "command"}"""));
        model.Code.Jobs.Add(new CodeJob(
            "NightlyJob", Path.Combine(temp.Path, "NightlyJob.cs"), 5, null));
        model.Implements.Add(new ImplementsLink("NightlyJob.cs", "use-case/lending.ghost", "job"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <summary>Mark Naming No Record Reports Mark Unresolved.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkNamingNoRecordReportsMarkUnresolved()
    {
        using var temp = TestCli.Temp("tables");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "GhostReaction.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null, file, 3,
            [new CodeMark("reaction/lending.nope", MarkStyle.Xml, MarkVerb.Implements, file, 2)],
            InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-unknown", codes);
    }

    /// <summary>Reaction Without A Mark Reports Mark Missing.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void ReactionWithoutAMarkReportsMarkMissing()
    {
        using var temp = TestCli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.yaml"), """{"kind": "command"}"""));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostReaction", null,
            Path.Combine(temp.Path, "GhostReaction.cs"), 3, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }
}
