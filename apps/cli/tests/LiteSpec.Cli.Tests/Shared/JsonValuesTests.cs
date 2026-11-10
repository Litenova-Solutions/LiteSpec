using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Tests.Shared;

/// <summary>Proves the shared JSON readings: message text, truthiness and strict equality.</summary>
public sealed class JsonValuesTests
{
    /// <summary>A string prints as itself, and a missing value prints as null.</summary>
    [Fact]
    public void TextPrintsAStringAsItselfAndNullAsNull()
    {
        Assert.Equal("hello", JsonValues.Text(JsonNode.Parse("\"hello\"")));
        Assert.Equal("null", JsonValues.Text(null));
        Assert.Equal("null", JsonValues.Text(JsonNode.Parse("null")));
    }

    /// <summary>Numbers print in invariant form, whole and fractional, and booleans as themselves.</summary>
    [Fact]
    public void TextPrintsNumbersInInvariantFormAndBooleansAsThemselves()
    {
        Assert.Equal("42", JsonValues.Text(JsonNode.Parse("42")));
        Assert.Equal("2.5", JsonValues.Text(JsonNode.Parse("2.5")));
        Assert.Equal("-7", JsonValues.Text(JsonValue.Create(-7)));
        Assert.Equal("true", JsonValues.Text(JsonNode.Parse("true")));
        Assert.Equal("false", JsonValues.Text(JsonNode.Parse("false")));
    }

    /// <summary>Objects and arrays print as compact JSON.</summary>
    [Fact]
    public void TextPrintsObjectsAndArraysAsCompactJson()
    {
        Assert.Equal("{\"a\":1}", JsonValues.Text(JsonNode.Parse("{\"a\": 1}")));
        Assert.Equal("[1,\"x\"]", JsonValues.Text(JsonNode.Parse("[1, \"x\"]")));
    }

    /// <summary>Null, false, zero, NaN and the empty string count as absent.</summary>
    [Fact]
    public void IsTruthyCountsNullFalseZeroNaNAndEmptyStringAsAbsent()
    {
        Assert.False(JsonValues.IsTruthy(null));
        Assert.False(JsonValues.IsTruthy(JsonNode.Parse("false")));
        Assert.False(JsonValues.IsTruthy(JsonNode.Parse("0")));
        Assert.False(JsonValues.IsTruthy(JsonValue.Create(double.NaN)));
        Assert.False(JsonValues.IsTruthy(JsonNode.Parse("\"\"")));
    }

    /// <summary>True, text, a non-zero number and every object and array count as present.</summary>
    [Fact]
    public void IsTruthyCountsTrueTextNumbersObjectsAndArraysAsPresent()
    {
        Assert.True(JsonValues.IsTruthy(JsonNode.Parse("true")));
        Assert.True(JsonValues.IsTruthy(JsonNode.Parse("\"x\"")));
        Assert.True(JsonValues.IsTruthy(JsonNode.Parse("-1")));
        Assert.True(JsonValues.IsTruthy(JsonNode.Parse("{}")));
        Assert.True(JsonValues.IsTruthy(JsonNode.Parse("[]")));
    }

    /// <summary>Primitives compare by type and value, with every number one type.</summary>
    [Fact]
    public void StrictEqualComparesPrimitivesByTypeAndValue()
    {
        Assert.True(JsonValues.StrictEqual(JsonNode.Parse("\"a\""), JsonNode.Parse("\"a\"")));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("\"a\""), JsonNode.Parse("\"b\"")));
        Assert.True(JsonValues.StrictEqual(JsonNode.Parse("1"), JsonNode.Parse("1.0")));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("1"), JsonNode.Parse("\"1\"")));
        Assert.True(JsonValues.StrictEqual(JsonNode.Parse("true"), JsonNode.Parse("true")));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("true"), JsonNode.Parse("false")));
        Assert.True(JsonValues.StrictEqual(null, null));
        Assert.False(JsonValues.StrictEqual(null, JsonNode.Parse("0")));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("0"), null));
    }

    /// <summary>Objects and arrays compare by reference, never by content.</summary>
    [Fact]
    public void StrictEqualComparesObjectsAndArraysByReference()
    {
        var node = JsonNode.Parse("{\"a\":1}");
        Assert.True(JsonValues.StrictEqual(node, node));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("{\"a\":1}"), JsonNode.Parse("{\"a\":1}")));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("[1]"), JsonNode.Parse("[1]")));
        Assert.False(JsonValues.StrictEqual(JsonNode.Parse("1"), JsonNode.Parse("[1]")));
    }
}
