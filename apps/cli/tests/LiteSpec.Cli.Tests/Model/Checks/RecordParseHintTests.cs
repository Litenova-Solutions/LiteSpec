using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a record file that fails on a plain value holding ': ' reports its file, line, and field with the fix.</summary>
public sealed class RecordParseHintTests
{
    /// <summary>A plain value holding ': ' reports the file, the line, the field, and the fix.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ScanNamesFileLineAndFieldForColonValue()
    {
        var scan = RecordFiles.Scan(
            "/root/aggregate.yaml",
            "type: aggregate\nid: aggregate/lending.loans\nmeaning: The statuses, each with the day it applies from: registered or exempt\n");

        var finding = Assert.Single(scan.Problems);

        Assert.Equal("record-invalid", finding.Code);
        Assert.Equal("/root/aggregate.yaml", finding.Path);
        Assert.Contains($"line {finding.Line}", finding.Message, StringComparison.Ordinal);
        Assert.Contains("'meaning'", finding.Message, StringComparison.Ordinal);
        Assert.Contains("Quote the value or rephrase it", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A quoted value holding ': ' parses, so no finding and no hint appear.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ScanStaysSilentForQuotedColonValue()
    {
        var scan = RecordFiles.Scan(
            "/root/quoted.yaml",
            "type: rule\nid: rule/lending.quoted\ntitle: Quoted\nmeaning: \"a: b\"\n");

        Assert.Empty(scan.Problems);
    }

    /// <summary>A failure on a line holding no plain value keeps the plain message with no hint.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ScanKeepsThePlainMessageWithoutAKeyValueLine()
    {
        var scan = RecordFiles.Scan("/root/bad.yaml", "{{{");

        Assert.NotEmpty(scan.Problems);
        Assert.DoesNotContain("Quote the value", scan.Problems[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A list entry names its field like a mapping line.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ColonHintNamesAListEntryField()
    {
        var hint = YamlText.ColonHint("- meaning: statuses from: today\n", 1) ?? string.Empty;

        Assert.Contains("'meaning'", hint, StringComparison.Ordinal);
    }

    /// <summary>No hint when the line holds no plain value with ': '.</summary>
    /// <param name="text">The YAML text.</param>
    /// <param name="line">The 1-based failing line.</param>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Theory]
    [InlineData("type: rule\nid: x\n", 1)]
    [InlineData("meaning: \"a: b\"\n", 1)]
    [InlineData("meaning: plain # note: here\n", 1)]
    [InlineData("meaning: a: b\n", 0)]
    [InlineData("meaning: a: b\n", 99)]
    public void ColonHintStaysSilentWithoutAPlainColonValue(string text, int line)
    {
        Assert.Null(YamlText.ColonHint(text, line));
    }
}
