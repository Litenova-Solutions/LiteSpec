using LiteSpec.Model;
using LiteSpec.Tests.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Tables;

/// <summary>Proves an automation links its record through its mark.</summary>
public sealed class ImplementsLinkTests
{
    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Automation_Without_A_Tag_But_With_An_Implements_Link_Passes()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Automations.Add(new AutomationInfo(
            "GhostAutomation", Path.Combine(temp.Path, "GhostAutomation.cs"), 3, null));
        model.Implements.Add(new ImplementsLink("GhostAutomation.cs", "use-case/lending.ghost", "automation"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Automation_Implementing_Its_Automation_Record_Passes()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-notice-on-loan-overdue", Path.Combine(temp.Path, "send-notice-on-loan-overdue.md"), """{"triggeredBy": {"event": "event/loan.overdue"}}"""));
        model.Code.Automations.Add(new AutomationInfo(
            "SendNoticeOnLoanOverdueHandler", Path.Combine(temp.Path, "SendNoticeOnLoanOverdueHandler.cs"), 3, null));
        model.Implements.Add(new ImplementsLink("SendNoticeOnLoanOverdueHandler.cs", "automation/lending.send-notice-on-loan-overdue", "automation"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Automation_Marking_A_Record_Of_Another_Kind_Reports_Mark_Unresolved()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.md"), """{"states": [{"name": "Open", "meaning": "The loan is open."}]}"""));
        var file = Path.Combine(temp.Path, "SendNoticeOnLoanOverdueHandler.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "SendNoticeOnLoanOverdueHandler", null, file, 3,
            [new CodeMark("aggregate/lending.loan", MarkStyle.Xml, MarkVerb.Implements, file, 2)],
            InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-unknown", codes);
    }

    /// <proves>litespec/core/layers#implement-only-declared-behavior</proves>
    [Fact]
    public void Scheduled_Automation_Without_A_Tag_But_With_An_Implements_Link_Passes()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Automations.Add(new AutomationInfo(
            "NightlyJob", Path.Combine(temp.Path, "NightlyJob.cs"), 5, null));
        model.Implements.Add(new ImplementsLink("NightlyJob.cs", "use-case/lending.ghost", "automation"));

        Assert.DoesNotContain("behavior-unrecorded", TestModel.Codes(model));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Mark_Naming_No_Record_Reports_Mark_Unresolved()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "GhostAutomation.cs");
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostAutomation", null, file, 3,
            [new CodeMark("automation/lending.nope", MarkStyle.Xml, MarkVerb.Implements, file, 2)],
            InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-unknown", codes);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Automation_Without_A_Mark_Reports_Mark_Missing()
    {
        using var temp = Cli.Temp("tables");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.ghost", Path.Combine(temp.Path, "ghost.md"), """{"kind": "command"}"""));
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.EventHandler, "GhostAutomation", null,
            Path.Combine(temp.Path, "GhostAutomation.cs"), 3, [], InTests: false));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("behavior-unrecorded", codes);
        Assert.Contains("mark-missing", codes);
    }
}
