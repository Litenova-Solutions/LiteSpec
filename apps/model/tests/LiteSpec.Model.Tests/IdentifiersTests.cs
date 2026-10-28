using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves every identifier pattern matches its shape and rejects the rest.</summary>
public sealed class IdentifiersTests
{
    /// <summary>The named pattern matches the sample exactly when expected.</summary>
    [Theory]
    [InlineData("citation", "see use-case/lending.borrow-copy.", true)]
    [InlineData("citation", "no records here", false)]
    [InlineData("shared", "litespec/core/scope#keep-one-bounded-context", true)]
    [InlineData("shared", "litespec/core/scope", false)]
    [InlineData("event", "event/expense.submitted", true)]
    [InlineData("event", "event/expense", false)]
    [InlineData("refusal", "lending.copy-not-available", true)]
    [InlineData("refusal", "lending", false)]
    [InlineData("implements", "<implements>use-case/lending.borrow-copy</implements>", true)]
    [InlineData("implements", "use-case/lending.borrow-copy", false)]
    [InlineData("proves", "<proves>scenario/borrow-copy.borrowed</proves>", true)]
    [InlineData("proves", "<proves>use-case/lending.borrow-copy</proves>", false)]
    [InlineData("heading", "### Keep Shared UI in One Package", true)]
    [InlineData("heading", "## Standards", false)]
    public void Pattern_Matches_Expected_Samples(string name, string sample, bool expected)
    {
        Regex pattern = name switch
        {
            "citation" => Identifiers.Citation,
            "shared" => Identifiers.SharedCitation(),
            "event" => Identifiers.EventId(),
            "refusal" => Identifiers.RefusalCode(),
            "implements" => Identifiers.ImplementsTag(),
            "proves" => Identifiers.ProvesTag(),
            "heading" => Identifiers.ProvisionHeading(),
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

        Assert.Equal(expected, pattern.IsMatch(sample));
    }
}
