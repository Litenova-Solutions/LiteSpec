using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves a known gap covers its finding while open and releases it when expired.</summary>
public sealed class SuppressionTests
{
    /// <summary>An open gap moves its finding to known.</summary>
    /// <proves>litespec/core/model#record-accepted-gaps-as-known-gaps</proves>
    [Fact]
    public void Open_Gap_Covers_Its_Finding()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "use-case-unimplemented",
            "Use case 'use-case/lending.list-loans' has no handler.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "known-gap",
                "known-gap/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["check"] = "use-case-unimplemented",
                    ["endsBy"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Known);
    }

    /// <summary>An expired gap keeps its finding reported.</summary>
    /// <proves>litespec/core/model#record-accepted-gaps-as-known-gaps</proves>
    [Fact]
    public void Expired_Gap_Keeps_Its_Finding()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "use-case-unimplemented",
            "Use case 'use-case/lending.list-loans' has no handler.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "known-gap",
                "known-gap/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["check"] = "use-case-unimplemented",
                    ["endsBy"] = "2020-01-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }
}
