using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for mark style parsing and writing.</summary>
public sealed class MarkStyleTests
{
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse reads xml")]
    public void ParseReadsXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse("xml"));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse reads attribute")]
    public void ParseReadsAttribute() => Assert.Equal(MarkStyle.Attribute, MarkStyleParser.Parse("attribute"));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse defaults a missing value to xml")]
    public void ParseDefaultsMissingToXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse(null));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse defaults a blank value to xml")]
    public void ParseDefaultsBlankToXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse("  "));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse ignores case and whitespace")]
    public void ParseIgnoresCaseAndWhitespace() =>
        Assert.Equal(MarkStyle.Attribute, MarkStyleParser.Parse("  Attribute "));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "Parse rejects an unknown style")]
    public void ParseRejectsUnknown() =>
        Assert.Throws<ArgumentException>(() => MarkStyleParser.Parse("yaml"));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "TryParse reads both styles")]
    public void TryParseReadsBothStyles()
    {
        Assert.True(MarkStyleParser.TryParse("xml", out var xml));
        Assert.Equal(MarkStyle.Xml, xml);
        Assert.True(MarkStyleParser.TryParse("attribute", out var attribute));
        Assert.Equal(MarkStyle.Attribute, attribute);
    }

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "TryParse defaults a missing value to xml")]
    public void TryParseDefaultsMissingToXml()
    {
        Assert.True(MarkStyleParser.TryParse(null, out var style));
        Assert.Equal(MarkStyle.Xml, style);
    }

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "TryParse rejects an unknown style")]
    public void TryParseRejectsUnknown() =>
        Assert.False(MarkStyleParser.TryParse("yaml", out _));

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "ToProjectValue writes both styles")]
    public void ToProjectValueWritesBothStyles()
    {
        Assert.Equal("xml", MarkStyle.Xml.ToProjectValue());
        Assert.Equal("attribute", MarkStyle.Attribute.ToProjectValue());
    }

    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "ToProjectValue rejects an unknown style")]
    public void ToProjectValueRejectsUnknown() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((MarkStyle)99).ToProjectValue());
}
