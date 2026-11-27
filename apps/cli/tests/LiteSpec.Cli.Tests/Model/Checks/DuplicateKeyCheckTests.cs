using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the duplicate key checks: no two entries of one record share a source name, branch path, operation name or field name, at tier G.</summary>
public sealed class DuplicateKeyCheckTests
{
    /// <summary>Each key kind that repeats within one record reports once, naming the kind and the key.</summary>
    /// <param name="type">The record type that holds the keyed list.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields, with one key written twice.</param>
    /// <param name="message">The finding message the check writes.</param>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Theory]
    [InlineData(
        "research",
        "research/loan-period-benchmarks",
        """{"sources": [{"name": "benchmark-report", "url": "https://example.com/a", "readOn": "2026-10-12"}, {"name": "benchmark-report", "url": "https://example.com/b", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
        "Record 'research/loan-period-benchmarks' names source name 'benchmark-report' twice.")]
    [InlineData(
        "constraint",
        "constraint/library-privacy-law",
        """{"authority": "City ordinance.", "sources": [{"name": "ordinance", "url": "https://example.com/a", "readOn": "2026-01-10"}, {"name": "ordinance", "url": "https://example.com/b", "readOn": "2026-01-10"}], "kind": "legal", "applies": true, "appliesBecause": "We keep reader records."}""",
        "Record 'constraint/library-privacy-law' names source name 'ordinance' twice.")]
    [InlineData(
        "flow",
        "flow/loan-desk",
        """{"actor": "actor/librarian", "steps": [{"id": "step/loan-desk.scan-copy", "title": "Scan Copy", "statement": "Scan the copy.", "branches": [{"when": "The copy is damaged.", "then": "step/loan-desk.shelve-copy"}, {"when": "The copy is on hold.", "then": "step/loan-desk.shelve-copy"}]}]}""",
        "Record 'flow/loan-desk' names the branch 'step/loan-desk.scan-copy to step/loan-desk.shelve-copy' twice.")]
    [InlineData(
        "flow",
        "flow/loan-desk",
        """{"actor": "actor/librarian", "steps": [{"id": "step/loan-desk.scan-copy", "title": "Scan Copy", "statement": "Scan the copy.", "branches": [{"when": "The copy is damaged.", "ends": "The copy is retired."}, {"when": "The copy is lost.", "ends": "The copy is written off."}]}]}""",
        "Record 'flow/loan-desk' names the branch 'step/loan-desk.scan-copy to end' twice.")]
    [InlineData(
        "external-system",
        "external-system/card-gateway",
        """{"operations": [{"name": "charge-card", "direction": "send"}, {"name": "charge-card", "direction": "send"}]}""",
        "Record 'external-system/card-gateway' names operation name 'charge-card' twice.")]
    [InlineData(
        "integration-event",
        "integration-event/copy-returned",
        """{"fields": [{"name": "copyId", "meaning": "The copy."}, {"name": "copyId", "meaning": "The copy again."}]}""",
        "Record 'integration-event/copy-returned' names field name 'copyId' twice.")]
    [InlineData(
        "use-case",
        "use-case/lending.borrow-copy",
        """{"kind": "command", "inputs": [{"name": "copyId", "meaning": "The copy."}, {"name": "copyId", "meaning": "The copy again."}]}""",
        "Record 'use-case/lending.borrow-copy' names field name 'copyId' twice.")]
    [InlineData(
        "use-case",
        "use-case/lending.borrow-copy",
        """{"kind": "command", "outputs": [{"name": "loanId", "meaning": "The loan."}, {"name": "loanId", "meaning": "The loan again."}]}""",
        "Record 'use-case/lending.borrow-copy' names field name 'loanId' twice.")]
    [InlineData(
        "use-case",
        "use-case/lending.list-loans",
        """{"kind": "query", "outputs": {"include": {"summary": {"fields": [{"name": "total", "meaning": "The count."}, {"name": "total", "meaning": "The count again."}]}}}}""",
        "Record 'use-case/lending.list-loans' names field name 'total' twice in output group 'summary'.")]
    public void RepeatedKeyReportsRecordKeyDuplicate(string type, string id, string json, string message)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records(type, id, Path.Combine(temp.Path, ".litespec", "record.yaml"), json)]);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-key-duplicate");
        Assert.Equal("G", finding.Tier);
        Assert.Equal(message, finding.Message);
        Assert.Equal(id, finding.Subject);
    }

    /// <summary>Two branches that leave one step for different steps pass, since one step may hold several paths.</summary>
    /// <proves>litespec/core/intent#branch-a-flow-only-between-its-steps</proves>
    [Fact]
    public void BranchesFromOneStepToDifferentStepsPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            [.. TestModel.Records(
                "flow",
                "flow/loan-desk",
                Path.Combine(temp.Path, ".litespec", "record.yaml"),
                """{"actor": "actor/librarian", "steps": [{"id": "step/loan-desk.scan-copy", "title": "Scan Copy", "statement": "Scan the copy.", "branches": [{"when": "The copy is damaged.", "ends": "The copy is retired."}, {"when": "The copy is on hold.", "then": "step/loan-desk.shelve-copy"}]}, {"id": "step/loan-desk.shelve-copy", "title": "Shelve Copy", "statement": "Shelve the copy."}]}""")]);

        Assert.DoesNotContain("record-key-duplicate", TestModel.Codes(model));
    }

    /// <summary>Distinct source names in one record pass.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact]
    public void DistinctSourceKeysPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                Path.Combine(temp.Path, ".litespec", "record.yaml"),
                """{"sources": [{"name": "benchmark-report", "url": "https://example.com/a", "readOn": "2026-10-12"}, {"name": "lending-survey", "url": "https://example.com/b", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}"""));

        Assert.DoesNotContain("record-key-duplicate", TestModel.Codes(model));
    }

    /// <summary>The same field name in two output groups of one query passes, since each group is its own list.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact]
    public void SameFieldNameInTwoOutputGroupsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "use-case",
                "use-case/lending.list-loans",
                Path.Combine(temp.Path, ".litespec", "record.yaml"),
                """{"kind": "query", "outputs": {"include": {"summary": {"fields": [{"name": "total", "meaning": "The count."}]}, "detail": {"fields": [{"name": "total", "meaning": "The count in detail."}]}}}}"""));

        Assert.DoesNotContain("record-key-duplicate", TestModel.Codes(model));
    }
}
