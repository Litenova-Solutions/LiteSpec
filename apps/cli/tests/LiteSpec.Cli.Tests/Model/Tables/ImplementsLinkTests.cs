using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Tables;

/// <summary>Proves an implements link records a reaction or job without a tag.</summary>
public sealed class ImplementsLinkTests
{
    [Fact]
    public void Reaction_Without_A_Tag_But_With_An_Implements_Link_Passes()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "GhostReaction", Path.Combine(temp.Path, "GhostReaction.cs"), 3, "reaction", null));
        model.Implements.Add(new ImplementsLink("GhostReaction.cs", "use-case/lending.ghost", "reaction"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    [Fact]
    public void Reaction_Implementing_Its_Reaction_Record_Passes()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.send-notice-on-loan-overdue", Path.Combine(temp.Path, "send-notice-on-loan-overdue.md"), """{"on": ["event/loan.overdue"]}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "SendNoticeOnLoanOverdueHandler", Path.Combine(temp.Path, "SendNoticeOnLoanOverdueHandler.cs"), 3, "reaction", null));
        model.Implements.Add(new ImplementsLink("SendNoticeOnLoanOverdueHandler.cs", "reaction/lending.send-notice-on-loan-overdue", "reaction"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    [Fact]
    public void Reaction_Implementing_A_Record_Of_Another_Kind_Reports()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), """{"states": [{"name": "Open", "meaning": "The loan is open."}]}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "SendNoticeOnLoanOverdueHandler", Path.Combine(temp.Path, "SendNoticeOnLoanOverdueHandler.cs"), 3, "reaction", null));
        model.Implements.Add(new ImplementsLink("SendNoticeOnLoanOverdueHandler.cs", "aggregate/lending.loan", "reaction"));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }

    [Fact]
    public void Job_Without_A_Tag_But_With_An_Implements_Link_Passes()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "NightlyJob", Path.Combine(temp.Path, "NightlyJob.cs"), 5, "job", null));
        model.Implements.Add(new ImplementsLink("NightlyJob.cs", "use-case/lending.ghost", "job"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    [Fact]
    public void Implements_Link_To_No_Use_Case_Still_Reports()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        model.Code.Reactions.Add(new ReactionInfo(
            "GhostReaction", Path.Combine(temp.Path, "GhostReaction.cs"), 3, "reaction", null));
        model.Implements.Add(new ImplementsLink("GhostReaction.cs", "use-case/lending.nope", "reaction"));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }

    [Fact]
    public void Implements_Link_On_Another_File_Still_Reports()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Reactions.Add(new ReactionInfo(
            "GhostReaction", Path.Combine(temp.Path, "GhostReaction.cs"), 3, "reaction", null));
        model.Implements.Add(new ImplementsLink("OtherReaction.cs", "use-case/lending.ghost", "reaction"));

        Assert.Contains("behavior-unrecorded", TestModel.Codes(model));
    }
}
