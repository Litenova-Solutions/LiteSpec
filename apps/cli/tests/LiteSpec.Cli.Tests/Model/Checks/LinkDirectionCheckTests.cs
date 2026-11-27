using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the link direction checks: a link from a higher layer to a lower one, and a flow step naming a system actor no external system represents.</summary>
public sealed class LinkDirectionCheckTests
{
    /// <summary>Intent Record Linking A Behavior Record Reports Direction Invalid.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void IntentRecordLinkingBehaviorRecordReportsDirectionInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["use-case/lending.borrow"]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "link-direction-invalid");

        Assert.Equal("requirement/borrowing.max-loans", finding.Subject);
        Assert.Contains("use-case/lending.borrow", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Behavior Record Linking An Intent Record Passes.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void BehaviorRecordLinkingIntentRecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command", "fulfills": ["requirement/borrowing.max-loans"]}"""));

        Assert.DoesNotContain("link-direction-invalid", TestModel.Codes(model));
    }

    /// <summary>Intent Record Linking An Intent Record Passes.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void IntentRecordLinkingIntentRecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goal.yaml"), "{}"),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(temp.Path, "max-loans.yaml"), """{"kind": "functional", "statement": "A ghost reads a copy.", "basedOn": ["goal/faster-pickup"]}"""));

        Assert.DoesNotContain("link-direction-invalid", TestModel.Codes(model));
    }

    /// <summary>Product Record Linking An Actor Passes: The Product Stands Above The Layers.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void ProductRecordLinkingActorPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("product", "product/library", Path.Combine(temp.Path, "product.yaml"), """{"boundary": "The lending desk only.", "users": ["actor/reader"]}"""),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person"}"""));

        Assert.DoesNotContain("link-direction-invalid", TestModel.Codes(model));
    }

    /// <summary>Permission Granting A Role And System Representing An Actor Pass.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void UpLinksFromBehaviorPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("role", "role/reader", Path.Combine(temp.Path, "reader-role.yaml"), """{"scope": "self"}"""),
            TestModel.Record("permission", "permission/read-loans", Path.Combine(temp.Path, "read-loans.yaml"), """{"meaning": "Read the loans.", "grantedTo": ["role/reader"]}"""),
            TestModel.Record("actor", "actor/mail-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            TestModel.Record("external-system", "external-system/mail-relay", Path.Combine(temp.Path, "mail.yaml"), """{"vendor": "Postlane", "represents": "actor/mail-relay"}"""));

        Assert.DoesNotContain("link-direction-invalid", TestModel.Codes(model));
    }

    /// <summary>Flow Step Naming A System Actor No External System Represents Reports.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void StepNamingUnrepresentedSystemActorReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [TestModel.Record("goal", "goal/notices-sent", Path.Combine(temp.Path, "goal.yaml"), "{}"),
            TestModel.Record("actor", "actor/mail-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            .. TestModel.Records("flow", "flow/notify-borrower", Path.Combine(temp.Path, "notify.yaml"), """{"actor": "actor/mail-relay", "serves": ["goal/notices-sent"], "startedWhen": "A loan falls due.", "finishedWhen": "The notice is sent.", "steps": [{"id": "step/notify-borrower.send-mail", "title": "Send Mail", "statement": "The relay sends the mail.", "actor": "actor/mail-relay"}]}""")]);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "flow/notify-borrower");

        Assert.Contains("actor/mail-relay", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Flow Step Naming A Represented System Actor Passes.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void StepNamingRepresentedSystemActorPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [TestModel.Record("goal", "goal/notices-sent", Path.Combine(temp.Path, "goal.yaml"), "{}"),
            TestModel.Record("actor", "actor/mail-relay", Path.Combine(temp.Path, "relay.yaml"), """{"kind": "system"}"""),
            TestModel.Record("external-system", "external-system/mail-relay", Path.Combine(temp.Path, "mail.yaml"), """{"vendor": "Postlane", "represents": "actor/mail-relay"}"""),
            .. TestModel.Records("flow", "flow/notify-borrower", Path.Combine(temp.Path, "notify.yaml"), """{"actor": "actor/mail-relay", "serves": ["goal/notices-sent"], "startedWhen": "A loan falls due.", "finishedWhen": "The notice is sent.", "steps": [{"id": "step/notify-borrower.send-mail", "title": "Send Mail", "statement": "The relay sends the mail.", "actor": "actor/mail-relay"}]}""")]);

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "flow/notify-borrower");
    }

    /// <summary>Flow Step Naming A Person Actor Passes.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void StepNamingPersonActorPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [TestModel.Record("goal", "goal/faster-pickup", Path.Combine(temp.Path, "goal.yaml"), "{}"),
            TestModel.Record("actor", "actor/reader", Path.Combine(temp.Path, "reader.yaml"), """{"kind": "person"}"""),
            .. TestModel.Records("flow", "flow/borrow-and-return", Path.Combine(temp.Path, "borrow.yaml"), """{"actor": "actor/reader", "serves": ["goal/faster-pickup"], "startedWhen": "The reader is at the desk.", "finishedWhen": "The copy is out.", "steps": [{"id": "step/borrow-and-return.borrow-copy", "title": "Borrow Copy", "statement": "The reader borrows the copy.", "actor": "actor/reader"}]}""")]);

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Subject == "flow/borrow-and-return");
    }
}
