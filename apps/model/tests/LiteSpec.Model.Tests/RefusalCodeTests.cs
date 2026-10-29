using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Tests for the shared refusal-code rule every host calls.</summary>
public sealed class RefusalCodeTests
{
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A refusal naming a refusal result returns its code")]
    public void ResultRefusalWithCodeReturnsCode()
    {
        var rule = Rule("result/submit-expense.receipt-missing");
        var result = new FakeRecord(
            "result",
            "result/submit-expense.receipt-missing",
            fields: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["outcome"] = "refusal",
                ["code"] = "spending.receipt-missing",
            });

        Assert.Equal("spending.receipt-missing", RefusalCode.Of(rule, ById(result)));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A bare refusal code returns as is")]
    public void BareCodeReturnsAsIs()
    {
        var rule = Rule("spending.receipt-missing");

        Assert.Equal("spending.receipt-missing", RefusalCode.Of(rule, ById()));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A rule with no refusal returns null")]
    public void MissingRefusalReturnsNull()
    {
        var rule = new FakeRecord("rule", "rule/spending.receipt-required");

        Assert.Null(RefusalCode.Of(rule, ById()));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A rule with an empty refusal returns null")]
    public void EmptyRefusalReturnsNull()
    {
        var rule = Rule(string.Empty);

        Assert.Null(RefusalCode.Of(rule, ById()));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A refusal naming no record returns null")]
    public void UnresolvedRefusalReturnsNull()
    {
        var rule = Rule("result/submit-expense.ghost");

        Assert.Null(RefusalCode.Of(rule, ById()));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A refusal naming a record of another type returns null")]
    public void RefusalToNonResultReturnsNull()
    {
        var rule = Rule("result/submit-expense.receipt-missing");
        var other = new FakeRecord(
            "use-case",
            "result/submit-expense.receipt-missing",
            fields: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["outcome"] = "refusal",
                ["code"] = "spending.receipt-missing",
            });

        Assert.Null(RefusalCode.Of(rule, ById(other)));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A refusal naming a success result with a code returns null")]
    public void RefusalToSuccessResultReturnsNull()
    {
        var rule = Rule("result/submit-expense.receipt-missing");
        var result = new FakeRecord(
            "result",
            "result/submit-expense.receipt-missing",
            fields: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["outcome"] = "success",
                ["code"] = "spending.receipt-missing",
            });

        Assert.Null(RefusalCode.Of(rule, ById(result)));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A refusal naming a refusal result without a code returns null")]
    public void RefusalToResultWithoutCodeReturnsNull()
    {
        var rule = Rule("result/submit-expense.receipt-missing");
        var result = new FakeRecord(
            "result",
            "result/submit-expense.receipt-missing",
            fields: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["outcome"] = "refusal",
            });

        Assert.Null(RefusalCode.Of(rule, ById(result)));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "Of rejects a null rule")]
    public void OfRejectsNullRule() =>
        Assert.Throws<ArgumentNullException>(() => RefusalCode.Of(null!, ById()));

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "Of rejects a null lookup")]
    public void OfRejectsNullLookup() =>
        Assert.Throws<ArgumentNullException>(() => RefusalCode.Of(Rule("spending.receipt-missing"), null!));

    private static FakeRecord Rule(string refusal) =>
        new FakeRecord(
            "rule",
            "rule/spending.receipt-required",
            fields: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["refusal"] = refusal,
            });

    private static Func<string, IRecordFacts?> ById(params FakeRecord[] records) =>
        id =>
        {
            foreach (var record in records)
            {
                if (record.Id == id)
                {
                    return record;
                }
            }

            return null;
        };
}
