using LiteSpec.Cli.Model;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that the term check does not read a phrase made of reaction-title words.</summary>
public sealed class ReactionTitleTermTests
{
    /// <summary>A phrase repeated in reaction summaries but built from their titles is not reported.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void ReactionTitlePhrasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Reaction(temp.Path, "reaction/lending.on-loan-borrowed", "Loan Borrowed", "Send Due Notice when a loan is borrowed."),
            Reaction(temp.Path, "reaction/finance.on-fine-paid", "Fine Paid", "Send Due Notice when a fine is paid."),
            Reaction(temp.Path, "reaction/spending.on-expense-rejected", "Expense Rejected", "Send Due Notice when an expense is rejected."));

        Assert.DoesNotContain(TermFindings(model), found => found.Message.Contains("Due Notice", StringComparison.Ordinal));
    }

    /// <summary>The same phrase repeated in three use-case summaries, with no reaction title to explain it, reports.</summary>
    /// <proves>litespec/core/intent#define-each-recurring-capitalized-phrase</proves>
    [Fact]
    public void SamePhraseInUseCasesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            UseCase(temp.Path, "use-case/lending.borrow-copy", "Send Due Notice when a copy is borrowed."),
            UseCase(temp.Path, "use-case/finance.record-fine", "Send Due Notice when a fine is recorded."),
            UseCase(temp.Path, "use-case/spending.reimburse-expense", "Send Due Notice when an expense is reimbursed."));

        Assert.Contains(TermFindings(model), found => found.Message.Contains("Due Notice", StringComparison.Ordinal));
    }

    /// <summary>One reaction record whose summary repeats part of its title.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The reaction identifier.</param>
    /// <param name="eventName">The event name that completes its title.</param>
    /// <param name="summary">The summary that carries the title words.</param>
    /// <returns>The reaction record.</returns>
    static LiteSpec.Cli.Model.Records.Record Reaction(string root, string id, string eventName, string summary) =>
        TestModel.Record(
            "reaction",
            id,
            Path.Combine(root, ".litespec", "behavior", "modules", "reactions.yaml"),
            "{\"title\": \"Send Due Notice on " + eventName + "\", \"summary\": \"" + summary + "\"}");

    /// <summary>One use-case record whose summary carries the phrase under test.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The use-case identifier.</param>
    /// <param name="summary">The summary that carries the phrase.</param>
    /// <returns>The use-case record.</returns>
    static LiteSpec.Cli.Model.Records.Record UseCase(string root, string id, string summary) =>
        TestModel.Record(
            "use-case",
            id,
            Path.Combine(root, ".litespec", "behavior", "modules", "use-cases.yaml"),
            "{\"kind\": \"command\", \"summary\": \"" + summary + "\"}");

    /// <summary>The term-missing findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is term-missing.</returns>
    static List<Finding> TermFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "term-missing").ToList();
}
