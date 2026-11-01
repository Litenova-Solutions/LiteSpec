using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the JSON reader parses values, tracks lines and names errors.</summary>
public sealed class JsonTextTests
{
    /// <summary>A nested object reads through objects, arrays and every scalar.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Parses_Nested_Values()
    {
        var root = JsonText.Parse("{\"loan\": {\"states\": [\"Active\", \"Returned\"], \"limit\": 25, \"open\": true, \"closed\": null}}");

        Assert.Equal(JsonKind.Object, root.Kind);
        Assert.True(root.TryGetProperty("loan", out var loan));
        Assert.Equal(JsonKind.Object, loan.Kind);
        Assert.True(loan.TryGetProperty("states", out var states));
        Assert.Equal(JsonKind.Array, states.Kind);
        Assert.Equal(2, states.Items.Count);
        Assert.Equal("Active", states.Items[0].GetString());
        Assert.Equal("Returned", states.Items[1].GetString());
        Assert.True(loan.TryGetProperty("limit", out var limit));
        Assert.Equal(JsonKind.Number, limit.Kind);
        Assert.Null(limit.GetString());
        Assert.True(loan.TryGetProperty("open", out var open));
        Assert.Equal(JsonKind.True, open.Kind);
        Assert.True(loan.TryGetProperty("closed", out var closed));
        Assert.Equal(JsonKind.Null, closed.Kind);
        Assert.False(root.TryGetProperty("missing", out _));
        Assert.Null(root.GetString());
        Assert.Empty(root.Items);
    }

    /// <summary>Every escape reads as its character, including unicode and a surrogate pair.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Unescapes_Strings()
    {
        var root = JsonText.Parse("{\"text\": \"a\\\"b\\\\c\\/d\\b\\f\\n\\r\\t\\u00e9\\uD83D\\uDE00\"}");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("a\"b\\c/d\b\f\n\r\t\u00e9\U0001F600", text.GetString());
    }

    /// <summary>Every value carries the 1-based line where it starts.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Reports_Value_Lines()
    {
        var root = JsonText.Parse("{\n  \"a\": 1,\n  \"b\": {\n    \"c\": \"x\"\n  }\n}");

        Assert.Equal(1, root.Line);
        Assert.True(root.TryGetProperty("a", out var first));
        Assert.Equal(2, first.Line);
        Assert.True(root.TryGetProperty("b", out var nested));
        Assert.Equal(3, nested.Line);
        Assert.True(nested.TryGetProperty("c", out var leaf));
        Assert.Equal(4, leaf.Line);
        Assert.Equal("x", leaf.GetString());
    }

    /// <summary>An unexpected token throws with its line, in the message and on the error.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Rejects_Unexpected_Token_Naming_Its_Line()
    {
        var error = Assert.Throws<JsonTextException>(() => JsonText.Parse("{\"a\": 1,\n\"b\": }"));

        Assert.Equal(2, error.Line);
        Assert.Contains("line 2", error.Message);
    }

    /// <summary>An unterminated value, trailing content and an empty text throw on line 1.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Rejects_Truncated_And_Trailing_Text()
    {
        Assert.Equal(1, Assert.Throws<JsonTextException>(() => JsonText.Parse("{\"a\": \"x")).Line);
        Assert.Equal(1, Assert.Throws<JsonTextException>(() => JsonText.Parse("1 2")).Line);
        Assert.Equal(1, Assert.Throws<JsonTextException>(() => JsonText.Parse("")).Line);
        Assert.Equal(1, Assert.Throws<JsonTextException>(() => JsonText.Parse("{\"a\": 1,}")).Line);
    }
}
