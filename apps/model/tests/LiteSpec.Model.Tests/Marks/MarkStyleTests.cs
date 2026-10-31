using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for mark style parsing and writing.</summary>
public sealed class MarkStyleTests
{
    /// <summary>Parse reads xml.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse reads xml")]
    public void ParseReadsXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse("xml"));

    /// <summary>Parse reads attribute.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse reads attribute")]
    public void ParseReadsAttribute() => Assert.Equal(MarkStyle.Attribute, MarkStyleParser.Parse("attribute"));

    /// <summary>Parse defaults a missing value to xml.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse defaults a missing value to xml")]
    public void ParseDefaultsMissingToXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse(null));

    /// <summary>Parse defaults a blank value to xml.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse defaults a blank value to xml")]
    public void ParseDefaultsBlankToXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse("  "));

    /// <summary>Parse ignores case and whitespace.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse ignores case and whitespace")]
    public void ParseIgnoresCaseAndWhitespace() =>
        Assert.Equal(MarkStyle.Attribute, MarkStyleParser.Parse("  Attribute "));

    /// <summary>Parse rejects an unknown style.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse rejects an unknown style")]
    public void ParseRejectsUnknown() =>
        Assert.Throws<ArgumentException>(() => MarkStyleParser.Parse("yaml"));

    /// <summary>Parse rejects the fixed styles of the other languages.</summary>
    /// <param name="value">The value under test.</param>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory(DisplayName = "Parse rejects the fixed styles of the other languages")]
    [InlineData("typed")]
    [InlineData("comment")]
    public void ParseRejectsFixedStyles(string value) =>
        Assert.Throws<ArgumentException>(() => MarkStyleParser.Parse(value));

    /// <summary>TryParse reads both styles.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "TryParse reads both styles")]
    public void TryParseReadsBothStyles()
    {
        Assert.True(MarkStyleParser.TryParse("xml", out var xml));
        Assert.Equal(MarkStyle.Xml, xml);
        Assert.True(MarkStyleParser.TryParse("attribute", out var attribute));
        Assert.Equal(MarkStyle.Attribute, attribute);
    }

    /// <summary>TryParse defaults a missing value to xml.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "TryParse defaults a missing value to xml")]
    public void TryParseDefaultsMissingToXml()
    {
        Assert.True(MarkStyleParser.TryParse(null, out var style));
        Assert.Equal(MarkStyle.Xml, style);
    }

    /// <summary>TryParse rejects an unknown style.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "TryParse rejects an unknown style")]
    public void TryParseRejectsUnknown() =>
        Assert.False(MarkStyleParser.TryParse("yaml", out _));

    /// <summary>ToProjectValue writes both styles.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "ToProjectValue writes both styles")]
    public void ToProjectValueWritesBothStyles()
    {
        Assert.Equal("xml", MarkStyle.Xml.ToProjectValue());
        Assert.Equal("attribute", MarkStyle.Attribute.ToProjectValue());
    }

    /// <summary>ToProjectValue writes the fixed styles of the other languages.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "ToProjectValue writes the fixed styles of the other languages")]
    public void ToProjectValueWritesFixedStyles()
    {
        Assert.Equal("typed", MarkStyle.Typed.ToProjectValue());
        Assert.Equal("comment", MarkStyle.Comment.ToProjectValue());
    }

    /// <summary>ToProjectValue rejects an unknown style.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "ToProjectValue rejects an unknown style")]
    public void ToProjectValueRejectsUnknown() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((MarkStyle)99).ToProjectValue());

    /// <summary>IsCSharp holds only for the declared C# styles.</summary>
    /// <param name="style">The mark style under test.</param>
    /// <param name="expected">The expected result for the case.</param>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Theory(DisplayName = "IsCSharp holds only for the declared C# styles")]
    [InlineData(MarkStyle.Xml, true)]
    [InlineData(MarkStyle.Attribute, true)]
    [InlineData(MarkStyle.Typed, false)]
    [InlineData(MarkStyle.Comment, false)]
    public void IsCSharpHoldsOnlyForCSharpStyles(MarkStyle style, bool expected) =>
        Assert.Equal(expected, style.IsCSharp());
}
