using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the exception checks against hand-built models.</summary>
public sealed class ExceptionCheckTests
{
    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Exception_Past_Its_Expires_On_Reports_Expired()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.md"), """{"status": "approved", "kind": "gap", "covers": ["use-case-unimplemented"], "subject": ["use-case/lending.list-loans"], "reason": "Listing waits for the search index.", "expiresOn": "2000-01-01"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "exception-expired");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Exception 'exception/list-loans-unimplemented' stands past its expiresOn 2000-01-01.", finding.Message);
        Assert.Equal("exception/list-loans-unimplemented", finding.Subject);
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Exception_Before_Its_Expires_On_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.md"), """{"status": "approved", "kind": "gap", "covers": ["use-case-unimplemented"], "subject": ["use-case/lending.list-loans"], "reason": "Listing waits for the search index.", "expiresOn": "2999-01-01"}"""));

        Assert.DoesNotContain("exception-expired", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Gap_Covering_An_Unknown_Code_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.md"), """{"status": "approved", "kind": "gap", "covers": ["no-such-finding"], "subject": ["use-case/lending.list-loans"], "reason": "Listing waits for the search index.", "expiresOn": "2999-01-01"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "exception-finding-unknown");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Exception 'exception/list-loans-unimplemented' names finding code 'no-such-finding', which no check emits.", finding.Message);
        Assert.Equal("exception/list-loans-unimplemented", finding.Subject);
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Gap_Covering_Emitted_Codes_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.md"), """{"status": "approved", "kind": "gap", "covers": ["use-case-unimplemented", "use-case-endpoint-missing"], "subject": ["use-case/lending.list-loans"], "reason": "Listing waits for the search index.", "expiresOn": "2999-01-01"}"""));

        Assert.DoesNotContain("exception-finding-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Departure_Covering_A_Provision_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/inline-bank-answer", Path.Combine(temp.Path, "inline-bank-answer.md"), """{"status": "approved", "kind": "departure", "covers": "litespec/backend/application#send-messages-after-commit", "subject": ["use-case/spending.reimburse-expense"], "reason": "The spender sees the bank's answer at once.", "expiresOn": "2999-01-01"}"""));

        Assert.DoesNotContain("exception-finding-unknown", TestModel.Codes(model));
        Assert.DoesNotContain("field-invalid", TestModel.Codes(model));
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Departure_Covering_No_Provision_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/inline-bank-answer", Path.Combine(temp.Path, "inline-bank-answer.md"), """{"status": "approved", "kind": "departure", "covers": "use-case-unimplemented", "subject": ["use-case/spending.reimburse-expense"], "reason": "The spender sees the bank's answer at once.", "expiresOn": "2999-01-01"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "field-invalid");
        Assert.Equal("Exception 'exception/inline-bank-answer' names 'use-case-unimplemented', which is not a shared provision.", finding.Message);
        Assert.Equal("exception/inline-bank-answer", finding.Subject);
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Exception_Naming_Neither_Subject_Nor_Paths_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/list-loans-unimplemented", Path.Combine(temp.Path, "list-loans-unimplemented.md"), """{"status": "approved", "kind": "gap", "covers": ["use-case-unimplemented"], "reason": "Listing waits for the search index.", "expiresOn": "2999-01-01"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("names neither subject nor paths", StringComparison.Ordinal));
        Assert.Equal("Exception 'exception/list-loans-unimplemented' names neither subject nor paths.", finding.Message);
        Assert.Equal("exception/list-loans-unimplemented", finding.Subject);
    }

    /// <proves>litespec/core/model#record-accepted-gaps-as-exceptions</proves>
    [Fact]
    public void Exception_Naming_Paths_Passes_Scope()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("exception", "exception/ghost-route", Path.Combine(temp.Path, "ghost-route.md"), """{"status": "approved", "kind": "gap", "covers": ["mark-missing"], "paths": ["src/GhostReaction.cs"], "reason": "Infrastructure.", "expiresOn": "2999-01-01"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("names neither subject nor paths", StringComparison.Ordinal));
    }
}
