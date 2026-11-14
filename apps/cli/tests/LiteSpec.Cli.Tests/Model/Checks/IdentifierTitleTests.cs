using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the identifier-title check: every page record takes its identifier name from its title words in title order, and names its record file for it, at tier G.</summary>
public sealed class IdentifierTitleTests
{
    /// <summary>Name Words Inside Title Words Pass.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void NameWordsInsideTitleWordsPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/approval.decided-within-five-days", Path.Combine(temp.Path, ".litespec", "intent", "decided-within-five-days.yaml"), """{"title": "Expense Decided Within Five Business Days"}"""));

        Assert.DoesNotContain("identifier-title-invalid", TestModel.Codes(model));
    }

    /// <summary>Name Words Outside Title Words Report.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void NameWordsOutsideTitleWordsReport()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/approval.no-self-approval", Path.Combine(temp.Path, ".litespec", "intent", "no-self-approval.yaml"), """{"title": "Nobody Approves Their Own Expense"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "identifier-title-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Nobody Approves Their Own Expense 'requirement/approval.no-self-approval' carries an identifier name that is not a word subsequence of its title.", finding.Message);
        Assert.Equal("requirement/approval.no-self-approval", finding.Subject);
    }

    /// <summary>Changed Word Reports.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void ChangedWordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/tax.vat-return-by-hand", Path.Combine(temp.Path, ".litespec", "intent", "vat-return-by-hand.yaml"), """{"title": "VAT Returns Miss Amounts or Use Wrong Boxes"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "identifier-title-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("VAT Returns Miss Amounts or Use Wrong Boxes 'requirement/tax.vat-return-by-hand' carries an identifier name that is not a word subsequence of its title.", finding.Message);
        Assert.Equal("requirement/tax.vat-return-by-hand", finding.Subject);
    }

    /// <summary>Decision Number Stays Out Of Title Words.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void DecisionNumberStaysOutOfTitleWords()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0012-each-feature-under-its-problem", Path.Combine(temp.Path, ".litespec", "intent", "0012-each-feature-under-its-problem.yaml"), """{"title": "Each Feature Under Its Problem"}"""));

        Assert.DoesNotContain("identifier-title-invalid", TestModel.Codes(model));
    }

    /// <summary>Owner Prefix Stays Out Of Title Words.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void OwnerPrefixStaysOutOfTitleWords()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/banking.book-bank-line", Path.Combine(temp.Path, ".litespec", "behavior", "book-bank-line.yaml"), """{"title": "Book Bank Line"}"""));

        Assert.DoesNotContain("identifier-title-invalid", TestModel.Codes(model));
    }

    /// <summary>File Stem Outside Identifier Name Reports.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void FileStemOutsideIdentifierNameReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.hold-pickup-window", Path.Combine(temp.Path, ".litespec", "intent", "holds.yaml"), """{"title": "Hold Pickup Window"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "identifier-title-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Hold Pickup Window 'requirement/borrowing.hold-pickup-window' sits in file 'holds.yaml' but its identifier names 'hold-pickup-window'.", finding.Message);
        Assert.Equal("requirement/borrowing.hold-pickup-window", finding.Subject);
    }

    /// <summary>Entry Inside List File Stays Silent.</summary>
    /// <proves>litespec/core/model#name-each-identifier-from-its-title</proves>
    [Fact]
    public void EntryInsideListFileStaysSilent()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/lending.copy-barcode-unique", Path.Combine(temp.Path, ".litespec", "behavior", "barcodes.yaml"), 4, """{"title": "Nothing Shared Here"}""", isEntry: true));

        Assert.DoesNotContain("identifier-title-invalid", TestModel.Codes(model));
    }
}
