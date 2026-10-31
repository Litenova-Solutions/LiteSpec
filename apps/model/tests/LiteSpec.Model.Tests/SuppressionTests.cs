using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves an approved exception holds its covered findings on its subjects and paths while open.</summary>
public sealed class SuppressionTests
{
    /// <summary>An open exception moves its covered finding to known.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Open_Exception_Covers_Its_Finding()
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
                "exception",
                "exception/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "use-case-unimplemented" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Known);
    }

    /// <summary>An expired exception keeps its finding reported.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Expired_Exception_Keeps_Its_Finding()
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
                "exception",
                "exception/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2020-01-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "use-case-unimplemented" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }

    /// <summary>A draft exception keeps its finding reported.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Draft_Exception_Keeps_Its_Finding()
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
                "exception",
                "exception/list-loans-unimplemented",
                "draft",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "use-case-unimplemented" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }

    /// <summary>An exception holds only the codes it covers, not every finding on its subject.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Exception_Covers_Only_Its_Codes()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "mark-missing",
            "Handler 'ListLoansHandler' has no mark.",
            Path.Combine(root, "src", "ListLoansHandler.cs"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "exception",
                "exception/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "use-case-unimplemented" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }

    /// <summary>A paths glob holds a finding on a matching file, even with no subject.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Paths_Glob_Covers_A_Subjectless_Finding()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "behavior-unrecorded",
            "Handler 'LegacyHandler' has no record.",
            Path.Combine(root, "src", "Shop", "LegacyHandler.cs"),
            3,
            null);
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "exception",
                "exception/legacy-shop",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "behavior-unrecorded" },
                    ["paths"] = new[] { "src/**" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Known);
    }

    /// <summary>A paths glob keeps a finding on a file outside the glob reported.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Paths_Glob_Misses_Another_Folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "behavior-unrecorded",
            "Handler 'LegacyHandler' has no record.",
            Path.Combine(root, "src", "Shop", "LegacyHandler.cs"),
            3,
            null);
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "exception",
                "exception/legacy-docs",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "behavior-unrecorded" },
                    ["paths"] = new[] { "docs/**" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }

    /// <summary>A departure holds the codes its provision governs.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Departure_Covers_Its_Provisions_Codes()
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
                "exception",
                "exception/list-loans-departure",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "departure",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding },
            records,
            root,
            new DateTime(2026, 10, 28),
            _ => null,
            provision => provision == "litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark"
                ? new[] { "use-case-unimplemented" }
                : Array.Empty<string>());

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Known);
    }

    /// <summary>A departure holds nothing when no provision lookup is given.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Departure_Covers_Nothing_Without_A_Lookup()
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
                "exception",
                "exception/list-loans-departure",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "departure",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }

    /// <summary>An expiry finding is never held, even on the exception's own subject.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Expiry_Finding_Is_Never_Held()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "exception-expired",
            "Exception 'exception/list-loans-unimplemented' stands past its end 2020-01-01.",
            Path.Combine(root, "docs", "governance", "exceptions", "list-loans-unimplemented.md"),
            3,
            "exception/list-loans-unimplemented");
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "exception",
                "exception/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "exception-expired" },
                    ["subject"] = new[] { "exception/list-loans-unimplemented" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding }, records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Known);
    }

    /// <summary>An exception naming a use case also holds findings about its scenarios.</summary>
    /// <proves>litespec/core/model#apply-guidance-precedence</proves>
    [Fact]
    public void Use_Case_Subject_Covers_Its_Scenario()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "proves-unreached",
            "Scenario 'scenario/list-loans.listing' is proved by no test.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "scenario/list-loans.listing");
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "exception",
                "exception/list-loans-unproved",
                fields: new Dictionary<string, string>
                {
                    ["kind"] = "gap",
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = new[] { "proves-unreached" },
                    ["subject"] = new[] { "use-case/lending.list-loans" },
                }),
        };

        var partition = Suppression.Partition(
            new[] { finding },
            records,
            root,
            new DateTime(2026, 10, 28),
            home => home == "list-loans" ? "use-case/lending.list-loans" : null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Known);
    }
}
