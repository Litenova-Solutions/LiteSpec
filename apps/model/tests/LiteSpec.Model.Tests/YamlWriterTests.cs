using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the YAML writer emits canonical text that reads back unchanged.</summary>
public sealed class YamlWriterTests
{
    /// <summary>Plain-safe strings write without quotes.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Plain_Scalars_Without_Quotes()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"type\":\"use-case\",\"title\":\"Borrow copy\",\"owner\":\"lending-team\"}"));

        Assert.Equal("type: use-case\ntitle: Borrow copy\nowner: lending-team\n", text);
    }

    /// <summary>Strings that would resolve as non-strings write double-quoted.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Quoted_Scalars_That_Resolve_As_Non_String()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"a\":\"\",\"b\":\"true\",\"c\":\"123\",\"d\":\"a: b\",\"e\":\"a # b\",\"f\":\" leading\",\"g\":\"trailing \",\"h\":\"- dash\",\"i\":\"#hash\"}"));

        Assert.Equal("a: \"\"\nb: \"true\"\nc: \"123\"\nd: \"a: b\"\ne: \"a # b\"\nf: \" leading\"\ng: \"trailing \"\nh: \"- dash\"\ni: \"#hash\"\n", text);
    }

    /// <summary>Multi-line text writes as a literal block with the matching chomp.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Multiline_Text_As_Literal_Blocks()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"a\":\"line1\\nline2\",\"b\":\"line1\\nline2\\n\",\"c\":\"single\"}"));

        Assert.Equal("a: |-\n  line1\n  line2\nb: |\n  line1\n  line2\nc: single\n", text);
    }

    /// <summary>Multi-line text that a block cannot hold writes quoted.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Unsafe_Multiline_Text_Quoted()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"d\":\" leading\\nline2\",\"e\":\"line1 \\nline2\"}"));

        Assert.Equal("d: \" leading\\nline2\"\ne: \"line1 \\nline2\"\n", text);
    }

    /// <summary>Empty collections write inline.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Empty_Collections_Inline()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"emptyArray\":[],\"emptyObject\":{}}"));

        Assert.Equal("emptyArray: []\nemptyObject: {}\n", text);

        Assert.Equal("[]\n", YamlWriter.Write(JsonText.Parse("[]")));
        Assert.Equal("{}\n", YamlWriter.Write(JsonText.Parse("{}")));
    }

    /// <summary>Nested blocks indent two spaces with sequences under their keys.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Nested_Blocks_With_Two_Space_Indentation()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"type\":\"use-case\",\"id\":\"use-case/lending.borrow-copy\",\"title\":\"Borrow copy\",\"owner\":\"lending-team\",\"status\":\"reviewed\",\"records\":[{\"type\":\"result\",\"id\":\"result/borrow-copy.borrowed\",\"title\":\"Borrowed\",\"outcome\":\"success\"},{\"type\":\"result\",\"id\":\"result/borrow-copy.copy-unavailable\",\"title\":\"Copy unavailable\",\"status\":\"draft\"}]}"));

        Assert.Equal("type: use-case\nid: use-case/lending.borrow-copy\ntitle: Borrow copy\nowner: lending-team\nstatus: reviewed\nrecords:\n  - type: result\n    id: result/borrow-copy.borrowed\n    title: Borrowed\n    outcome: success\n  - type: result\n    id: result/borrow-copy.copy-unavailable\n    title: Copy unavailable\n    status: draft\n", text);
    }

    /// <summary>Given keys write first in order; the rest follow in file order.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Keys_In_Given_Order()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"b\":1,\"a\":2,\"c\":3}"), _ => ["a", "c"]);

        Assert.Equal("a: 2\nc: 3\nb: 1\n", text);

        var nested = YamlWriter.Write(JsonText.Parse("{\"outer\":{\"b\":1,\"a\":2}}"), _ => ["a"]);

        Assert.Equal("outer:\n  a: 2\n  b: 1\n", nested);
    }

    /// <summary>Keys that would resolve as non-strings or hold punctuation write quoted.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Quotes_Keys_That_Need_It()
    {
        var text = YamlWriter.Write(JsonText.Parse("{\"123\":\"numeric\",\"a: b\":\"colon\",\"plain\":\"ok\"}"));

        Assert.Equal("\"123\": numeric\n\"a: b\": colon\nplain: ok\n", text);
    }

    /// <summary>Plain safety matches what the reader reads back as the same string.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void IsPlainSafe_Matches_Readback()
    {
        Assert.True(YamlWriter.IsPlainSafe("hello"));
        Assert.True(YamlWriter.IsPlainSafe("Borrow copy"));
        Assert.True(YamlWriter.IsPlainSafe("use-case/lending.borrow-copy"));

        Assert.False(YamlWriter.IsPlainSafe(""));
        Assert.False(YamlWriter.IsPlainSafe(" leading"));
        Assert.False(YamlWriter.IsPlainSafe("trailing "));
        Assert.False(YamlWriter.IsPlainSafe("true"));
        Assert.False(YamlWriter.IsPlainSafe("123"));
        Assert.False(YamlWriter.IsPlainSafe("a: b"));
        Assert.False(YamlWriter.IsPlainSafe("a # b"));
        Assert.False(YamlWriter.IsPlainSafe("- dash"));
        Assert.False(YamlWriter.IsPlainSafe("#hash"));
        Assert.False(YamlWriter.IsPlainSafe("a\nb"));
    }

    /// <summary>Writing then parsing gives the same JSON.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Write_Then_Parse_Round_Trips()
    {
        var cases = new[]
        {
            "{\"type\":\"use-case\",\"title\":\"Borrow copy\",\"count\":25,\"open\":true,\"closed\":null}",
            "{\"type\":\"use-case\",\"records\":[{\"type\":\"result\",\"id\":\"result/a.b\"}]}",
            "{\"a\":\"line1\\nline2\",\"b\":\"line1\\nline2\\n\"}",
            "{\"a\":\"\",\"b\":\"true\",\"c\":\"123\",\"d\":\"a: b\"}",
            "{\"a\":[],\"b\":{}}",
        };

        foreach (var json in cases)
        {
            var expected = JsonText.Parse(json).ToJson();
            var actual = YamlText.Parse(YamlWriter.Write(JsonText.Parse(json))).ToJson();

            Assert.Equal(expected, actual);
        }
    }

    /// <summary>Writing a parsed write gives the same text.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Write_Is_Stable()
    {
        var cases = new[]
        {
            "{\"type\":\"use-case\",\"title\":\"Borrow copy\",\"count\":25}",
            "{\"records\":[{\"type\":\"result\",\"id\":\"result/a.b\"},{\"type\":\"result\",\"id\":\"result/a.c\"}]}",
            "{\"a\":\"line1\\nline2\",\"b\":\"line1\\nline2\\n\"}",
            "{\"a\":\"\",\"b\":\"true\",\"c\":\"a: b\"}",
        };

        foreach (var json in cases)
        {
            var first = YamlWriter.Write(JsonText.Parse(json));
            var second = YamlWriter.Write(YamlText.Parse(first));

            Assert.Equal(first, second);
        }
    }

    /// <summary>Top-level strings write quoted; other scalars write plain.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Writes_Top_Level_Scalars_Quoted()
    {
        Assert.Equal("\"hello\"\n", YamlWriter.Write(JsonText.Parse("\"hello\"")));
        Assert.Equal("\"true\"\n", YamlWriter.Write(JsonText.Parse("\"true\"")));
        Assert.Equal("25\n", YamlWriter.Write(JsonText.Parse("25")));
        Assert.Equal("true\n", YamlWriter.Write(JsonText.Parse("true")));
        Assert.Equal("null\n", YamlWriter.Write(JsonText.Parse("null")));
        Assert.Equal("\"a\\nb\"\n", YamlWriter.Write(JsonText.Parse("\"a\\nb\"")));
    }

    /// <summary>Every write ends with one line end.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Ends_With_One_Line_End()
    {
        var cases = new[]
        {
            "{\"a\":1}",
            "{\"a\":[1,2]}",
            "\"hello\"",
            "{\"a\":\"line1\\nline2\"}",
            "{\"a\":\"line1\\nline2\\n\"}",
        };

        foreach (var json in cases)
        {
            var text = YamlWriter.Write(JsonText.Parse(json));

            Assert.EndsWith("\n", text);
            Assert.False(text.EndsWith("\n\n", StringComparison.Ordinal));
        }
    }
}
