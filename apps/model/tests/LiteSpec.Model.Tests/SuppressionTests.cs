using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves an departure holds its covered findings on its subjects and paths while it is live.</summary>
public sealed class SuppressionTests
{
    /// <summary>An open departure moves its covered finding to known.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void OpenDepartureCoversItsFinding()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "use-case-unimplemented",
            "Use case 'use-case/lending.list-loans' has no handler.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["use-case-unimplemented"],
                    ["subject"] = ["use-case/lending.list-loans"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Covered);
    }

    /// <summary>An expired departure keeps its finding reported.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void ExpiredDepartureKeepsItsFinding()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "use-case-unimplemented",
            "Use case 'use-case/lending.list-loans' has no handler.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2020-01-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["use-case-unimplemented"],
                    ["subject"] = ["use-case/lending.list-loans"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Covered);
    }

    /// <summary>An departure holds only the codes it covers, not every finding on its subject.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void DepartureCoversOnlyItsCodes()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "mark-missing",
            "Handler 'ListLoansHandler' has no mark.",
            Path.Combine(root, "src", "ListLoansHandler.cs"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["use-case-unimplemented"],
                    ["subject"] = ["use-case/lending.list-loans"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Covered);
    }

    /// <summary>A paths glob holds a finding on a matching file, even with no subject.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void PathsGlobCoversASubjectlessFinding()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "behavior-unrecorded",
            "Handler 'LegacyHandler' has no record.",
            Path.Combine(root, "src", "Shop", "LegacyHandler.cs"),
            3,
            null);
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/legacy-shop",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["behavior-unrecorded"],
                    ["paths"] = ["src/**"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Covered);
    }

    /// <summary>A paths glob keeps a finding on a file outside the glob reported.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void PathsGlobMissesAnotherFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "behavior-unrecorded",
            "Handler 'LegacyHandler' has no record.",
            Path.Combine(root, "src", "Shop", "LegacyHandler.cs"),
            3,
            null);
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/legacy-docs",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["behavior-unrecorded"],
                    ["paths"] = ["docs/**"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Covered);
    }

    /// <summary>A departure holds the codes its provision governs.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void DepartureCoversItsProvisionsCodes()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "use-case-unimplemented",
            "Use case 'use-case/lending.list-loans' has no handler.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-departure",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark"],
                    ["subject"] = ["use-case/lending.list-loans"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding],
            records,
            root,
            new DateTime(2026, 10, 28),
            _ => null,
            provision => provision == "litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark"
                ? ["use-case-unimplemented"]
                : Array.Empty<string>());

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Covered);
    }

    /// <summary>A departure holds nothing when no provision lookup is given.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void DepartureCoversNothingWithoutALookup()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "use-case-unimplemented",
            "Use case 'use-case/lending.list-loans' has no handler.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "use-case/lending.list-loans");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-departure",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark"],
                    ["subject"] = ["use-case/lending.list-loans"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Covered);
    }

    /// <summary>An expiry finding is never held, even on the departure's own subject.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void ExpiryFindingIsNeverHeld()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "departure-expired",
            "Departure 'departure/list-loans-unimplemented' stands past its end 2020-01-01.",
            Path.Combine(root, "docs", "governance", "departures", "list-loans-unimplemented.md"),
            3,
            "departure/list-loans-unimplemented");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-unimplemented",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["departure-expired"],
                    ["subject"] = ["departure/list-loans-unimplemented"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding], records, root, new DateTime(2026, 10, 28), _ => null);

        Assert.Single(partition.Findings);
        Assert.Empty(partition.Covered);
    }

    /// <summary>An departure naming a use case also holds findings about its scenarios.</summary>
    /// <proves>litespec/core/agent#apply-guidance-precedence</proves>
    [Fact]
    public void UseCaseSubjectCoversItsScenario()
    {
        var root = Path.Combine(Path.GetTempPath(), "litespec-suppression-test");
        var finding = new Finding(
            "proves-unreached",
            "Scenario 'scenario/list-loans.listing' is proved by no test.",
            Path.Combine(root, "docs", "list-loans.md"),
            3,
            "scenario/list-loans.listing");
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord(
                "departure",
                "departure/list-loans-unproved",
                fields: new Dictionary<string, string>
                {
                    ["expiresOn"] = "2035-06-01",
                },
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["covers"] = ["proves-unreached"],
                    ["subject"] = ["use-case/lending.list-loans"],
                }),
        ];

        var partition = Suppression.Partition(
            [finding],
            records,
            root,
            new DateTime(2026, 10, 28),
            home => home == "list-loans" ? "use-case/lending.list-loans" : null);

        Assert.Empty(partition.Findings);
        Assert.Single(partition.Covered);
    }
}
