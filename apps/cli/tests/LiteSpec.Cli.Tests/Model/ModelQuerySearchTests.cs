using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>litespec model query --search</c> finds records by text across identifier, title, summary and statement.</summary>
public sealed class ModelQuerySearchTests
{
    /// <summary>Writes three records that each hold the search word in one field only: title, summary and statement.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void WriteSearchRecords(string root)
    {
        TestRecords.Write(root, ".litespec/behavior/modules/lending/use-cases/ledger-review/ledger-review.yaml", """
            {
              "type": "use-case",
              "id": "use-case/lending.ledger-review",
              "title": "Quarry ledger review",
              "kind": "command"
            }
            """);
        TestRecords.Write(root, ".litespec/intent/features/borrowing/requirements/nightly-reconciled/nightly-reconciled.yaml", """
            {
              "type": "requirement",
              "id": "requirement/borrowing.nightly-reconciled",
              "title": "Nightly reconciliation",
              "summary": "The quarry ledger is reconciled nightly."
            }
            """);
        TestRecords.Write(root, ".litespec/intent/features/borrowing/requirements/ledger-close/ledger-close.yaml", """
            {
              "type": "requirement",
              "id": "requirement/borrowing.ledger-close",
              "title": "Ledger close",
              "summary": "The day ends.",
              "statement": "The clerk closes the quarry ledger before midnight."
            }
            """);
    }

    /// <summary>A search ranks title before summary before statement, each line with identifier, title and path.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void SearchRanksTitleSummaryStatementInOrder()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteSearchRecords(fixture.Path);
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--search", "quarry");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "use-case/lending.ledger-review - Quarry ledger review (.litespec/behavior/modules/lending/use-cases/ledger-review/ledger-review.yaml)",
            result.Stdout,
            StringComparison.Ordinal);

        var title = result.Stdout.IndexOf("use-case/lending.ledger-review", StringComparison.Ordinal);
        var summary = result.Stdout.IndexOf("requirement/borrowing.nightly-reconciled", StringComparison.Ordinal);
        var statement = result.Stdout.IndexOf("requirement/borrowing.ledger-close", StringComparison.Ordinal);

        Assert.True(title >= 0 && summary >= 0 && statement >= 0);
        Assert.True(title < summary, "The title match prints before the summary match.");
        Assert.True(summary < statement, "The summary match prints before the statement match.");
    }

    /// <summary>A search matches without regard to letter case.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void SearchMatchesCaseInsensitively()
    {
        using var fixture = ModelFixture.Prepare("small");
        WriteSearchRecords(fixture.Path);
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--search", "QUARRY");

        Assert.Equal(0, result.Status);
        Assert.Contains("use-case/lending.ledger-review", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("requirement/borrowing.nightly-reconciled", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("requirement/borrowing.ledger-close", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A search for an identifier prints the exact record first.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void SearchPrintsExactIdentifierFirst()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--search", "use-case/lending.borrow-copy");

        Assert.Equal(0, result.Status);
        Assert.StartsWith("use-case/lending.borrow-copy - Borrow copy (", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A search with no match prints nothing and exits 0.</summary>
    /// <proves>litespec/core/agent#select-task-context</proves>
    [Fact]
    public void SearchWithNoMatchPrintsNothingAndExits0()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--search", "quarry-zzz-no-such-word");

        Assert.Equal(0, result.Status);
        Assert.Equal("", result.Stdout);
    }

    /// <summary>A query with an id and --search is rejected: it takes one of the two.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void SearchWithAnIdIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "use-case/lending.borrow-copy", "--search", "quarry");

        Assert.Equal(2, result.Status);
        Assert.Contains("Query takes one of <id>, --search <text> or --list <type>.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A search with --module is rejected: --module needs --list.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void SearchWithModuleIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--search", "quarry", "--module", "lending");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --module needs --list.", result.Output, StringComparison.Ordinal);
    }

    /// <summary>A search with no text is rejected as a missing value.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void SearchWithEmptyTextIsRejected()
    {
        using var fixture = ModelFixture.Prepare("small");
        var result = TestCli.Run("model", "query", "--root", fixture.Path, "--search", "");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --search needs a value.", result.Output, StringComparison.Ordinal);
    }
}
