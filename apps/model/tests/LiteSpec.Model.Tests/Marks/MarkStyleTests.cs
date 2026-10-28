using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for mark style parsing and writing.</summary>
public sealed class MarkStyleTests
{
    [Fact(DisplayName = "Parse reads xml")]
    public void ParseReadsXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse("xml"));

    [Fact(DisplayName = "Parse reads attribute")]
    public void ParseReadsAttribute() => Assert.Equal(MarkStyle.Attribute, MarkStyleParser.Parse("attribute"));

    [Fact(DisplayName = "Parse defaults a missing value to xml")]
    public void ParseDefaultsMissingToXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse(null));

    [Fact(DisplayName = "Parse defaults a blank value to xml")]
    public void ParseDefaultsBlankToXml() => Assert.Equal(MarkStyle.Xml, MarkStyleParser.Parse("  "));

    [Fact(DisplayName = "Parse ignores case and whitespace")]
    public void ParseIgnoresCaseAndWhitespace() =>
        Assert.Equal(MarkStyle.Attribute, MarkStyleParser.Parse("  Attribute "));

    [Fact(DisplayName = "Parse rejects an unknown style")]
    public void ParseRejectsUnknown() =>
        Assert.Throws<ArgumentException>(() => MarkStyleParser.Parse("yaml"));

    [Fact(DisplayName = "TryParse reads both styles")]
    public void TryParseReadsBothStyles()
    {
        Assert.True(MarkStyleParser.TryParse("xml", out var xml));
        Assert.Equal(MarkStyle.Xml, xml);
        Assert.True(MarkStyleParser.TryParse("attribute", out var attribute));
        Assert.Equal(MarkStyle.Attribute, attribute);
    }

    [Fact(DisplayName = "TryParse defaults a missing value to xml")]
    public void TryParseDefaultsMissingToXml()
    {
        Assert.True(MarkStyleParser.TryParse(null, out var style));
        Assert.Equal(MarkStyle.Xml, style);
    }

    [Fact(DisplayName = "TryParse rejects an unknown style")]
    public void TryParseRejectsUnknown() =>
        Assert.False(MarkStyleParser.TryParse("yaml", out _));

    [Fact(DisplayName = "ToProjectValue writes both styles")]
    public void ToProjectValueWritesBothStyles()
    {
        Assert.Equal("xml", MarkStyle.Xml.ToProjectValue());
        Assert.Equal("attribute", MarkStyle.Attribute.ToProjectValue());
    }

    [Fact(DisplayName = "ToProjectValue rejects an unknown style")]
    public void ToProjectValueRejectsUnknown() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((MarkStyle)99).ToProjectValue());
}
