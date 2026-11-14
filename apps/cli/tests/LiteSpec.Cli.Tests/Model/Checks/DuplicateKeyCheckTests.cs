using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the duplicate key checks: no two entries of one record share a source key, step id, operation name or field name, at tier G.</summary>
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
        """{"sources": [{"key": "benchmark-report", "url": "https://example.com/a", "readOn": "2026-10-12"}, {"key": "benchmark-report", "url": "https://example.com/b", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
        "Record 'research/loan-period-benchmarks' names source key 'benchmark-report' twice.")]
    [InlineData(
        "journey",
        "journey/loan-desk",
        """{"actor": "actor/librarian", "steps": [{"id": "scan-copy", "statement": "Scan the copy."}, {"id": "scan-copy", "statement": "Scan it again."}]}""",
        "Record 'journey/loan-desk' names step id 'scan-copy' twice.")]
    [InlineData(
        "external-system",
        "external-system/card-gateway",
        """{"operations": [{"name": "charge-card", "direction": "out"}, {"name": "charge-card", "direction": "out"}]}""",
        "Record 'external-system/card-gateway' names operation name 'charge-card' twice.")]
    [InlineData(
        "integration-event",
        "integration-event/copy-returned",
        """{"payload": [{"name": "copyId", "meaning": "The copy."}, {"name": "copyId", "meaning": "The copy again."}]}""",
        "Record 'integration-event/copy-returned' names field name 'copyId' twice.")]
    [InlineData(
        "use-case",
        "use-case/lending.borrow-copy",
        """{"kind": "command", "input": [{"name": "copyId", "meaning": "The copy."}, {"name": "copyId", "meaning": "The copy again."}]}""",
        "Record 'use-case/lending.borrow-copy' names field name 'copyId' twice.")]
    [InlineData(
        "use-case",
        "use-case/lending.borrow-copy",
        """{"kind": "command", "output": [{"name": "loanId", "meaning": "The loan."}, {"name": "loanId", "meaning": "The loan again."}]}""",
        "Record 'use-case/lending.borrow-copy' names field name 'loanId' twice.")]
    [InlineData(
        "use-case",
        "use-case/lending.list-loans",
        """{"kind": "query", "output": {"include": {"summary": {"fields": [{"name": "total", "meaning": "The count."}, {"name": "total", "meaning": "The count again."}]}}}}""",
        "Record 'use-case/lending.list-loans' names field name 'total' twice in output group 'summary'.")]
    public void RepeatedKeyReportsRecordKeyDuplicate(string type, string id, string json, string message)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(type, id, Path.Combine(temp.Path, ".litespec", "record.yaml"), json));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-key-duplicate");
        Assert.Equal("G", finding.Tier);
        Assert.Equal(message, finding.Message);
        Assert.Equal(id, finding.Subject);
    }

    /// <summary>Distinct source keys in one record pass.</summary>
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
                """{"sources": [{"key": "benchmark-report", "url": "https://example.com/a", "readOn": "2026-10-12"}, {"key": "lending-survey", "url": "https://example.com/b", "readOn": "2026-10-12"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}"""));

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
                """{"kind": "query", "output": {"include": {"summary": {"fields": [{"name": "total", "meaning": "The count."}]}, "detail": {"fields": [{"name": "total", "meaning": "The count in detail."}]}}}}"""));

        Assert.DoesNotContain("record-key-duplicate", TestModel.Codes(model));
    }
}
