using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Tests for the shared refusal-code rule every host calls.</summary>
public sealed class RefusalCodeTests
{
    /// <summary>A rule naming a refusal code returns that code as written.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A rule naming a refusal code returns that code")]
    public void NamedRefusalCodeIsReturned()
    {
        var rule = Rule("spending.receipt-missing");

        Assert.Equal("spending.receipt-missing", RefusalCode.Of(rule));
    }

    /// <summary>A rule with no refusal returns null.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A rule with no refusal returns null")]
    public void MissingRefusalReturnsNull()
    {
        var rule = new FakeRecord("rule", "rule/spending.receipt-required");

        Assert.Null(RefusalCode.Of(rule));
    }

    /// <summary>A rule with an empty refusal returns null.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "A rule with an empty refusal returns null")]
    public void EmptyRefusalReturnsNull()
    {
        var rule = Rule(string.Empty);

        Assert.Null(RefusalCode.Of(rule));
    }

    /// <summary>Of rejects a null rule.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact(DisplayName = "Of rejects a null rule")]
    public void OfRejectsNullRule() =>
        Assert.Throws<ArgumentNullException>(() => RefusalCode.Of(null!));

    private static FakeRecord Rule(string refusal) =>
        new FakeRecord(
            "rule",
            "rule/spending.receipt-required",
            fields: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["refusal"] = refusal,
            });
}
