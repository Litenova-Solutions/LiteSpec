using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the YAML reader parses YAML, tracks lines and names errors.</summary>
public sealed class YamlTextTests
{
    /// <summary>CRLF line ends, and a block that ends on its last line's carriage return, read like LF.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ReadsCrlfLineEnds()
    {
        var crlf = YamlText.Parse("type: index\r\ntitle: Loans\r\nnote: |\r\n  First line.\r\n  Second line.\r\n");
        var cut = YamlText.Parse("type: index\r\ntitle: Loans\r");

        Assert.Equal(crlf.ToJson(), YamlText.Parse("type: index\ntitle: Loans\nnote: |\n  First line.\n  Second line.\n").ToJson());
        Assert.Equal("{\"type\":\"index\",\"title\":\"Loans\"}", cut.ToJson());
    }

    /// <summary>A nested mapping reads through objects, arrays and every scalar with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesBlockMappingWithNestedValues()
    {
        var root = YamlText.Parse("loan:\n  states:\n    - Active\n    - Returned\n  limit: 25\n  open: true\n  closed: null\n");

        Assert.Equal(JsonKind.Object, root.Kind);
        Assert.Equal(1, root.Line);
        Assert.True(root.TryGetProperty("loan", out var loan));
        Assert.Equal(JsonKind.Object, loan.Kind);
        Assert.Equal(2, loan.Line);
        Assert.True(loan.TryGetProperty("states", out var states));
        Assert.Equal(JsonKind.Array, states.Kind);
        Assert.Equal(3, states.Line);
        Assert.Equal(2, states.Items.Count);
        Assert.Equal("Active", states.Items[0].GetString());
        Assert.Equal(3, states.Items[0].Line);
        Assert.Equal("Returned", states.Items[1].GetString());
        Assert.Equal(4, states.Items[1].Line);
        Assert.True(loan.TryGetProperty("limit", out var limit));
        Assert.Equal(JsonKind.Number, limit.Kind);
        Assert.Equal(5, limit.Line);
        Assert.Null(limit.GetString());
        Assert.Equal("25", limit.ToJson());
        Assert.True(loan.TryGetProperty("open", out var open));
        Assert.Equal(JsonKind.True, open.Kind);
        Assert.Equal(6, open.Line);
        Assert.True(loan.TryGetProperty("closed", out var closed));
        Assert.Equal(JsonKind.Null, closed.Kind);
        Assert.Equal(7, closed.Line);
        Assert.False(root.TryGetProperty("missing", out _));
        Assert.Null(root.GetString());
        Assert.Empty(root.Items);
        Assert.Equal("{\"loan\":{\"states\":[\"Active\",\"Returned\"],\"limit\":25,\"open\":true,\"closed\":null}}", root.ToJson());

        var crlf = YamlText.Parse("a: 1\r\nb: 2\r\n");
        Assert.Equal("{\"a\":1,\"b\":2}", crlf.ToJson());

        var bom = YamlText.Parse(char.ConvertFromUtf32(0xFEFF) + "a: 1\n");
        Assert.Equal("{\"a\":1}", bom.ToJson());
    }

    /// <summary>A top-level sequence reads scalars and dash-line mappings with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesTopLevelSequence()
    {
        var scalars = YamlText.Parse("- Active\n- Returned\n");

        Assert.Equal(JsonKind.Array, scalars.Kind);
        Assert.Equal(1, scalars.Line);
        Assert.Equal(2, scalars.Items.Count);
        Assert.Equal("Active", scalars.Items[0].GetString());
        Assert.Equal(1, scalars.Items[0].Line);
        Assert.Equal("Returned", scalars.Items[1].GetString());
        Assert.Equal(2, scalars.Items[1].Line);

        var mappings = YamlText.Parse("- type: result\n  id: result/a.b\n- type: result\n  id: result/a.c\n");

        Assert.Equal(JsonKind.Array, mappings.Kind);
        Assert.Equal(2, mappings.Items.Count);
        Assert.Equal(JsonKind.Object, mappings.Items[0].Kind);
        Assert.Equal(1, mappings.Items[0].Line);
        Assert.True(mappings.Items[0].TryGetProperty("id", out var first));
        Assert.Equal("result/a.b", first.GetString());
        Assert.Equal(JsonKind.Object, mappings.Items[1].Kind);
        Assert.Equal(3, mappings.Items[1].Line);
        Assert.True(mappings.Items[1].TryGetProperty("id", out var second));
        Assert.Equal("result/a.c", second.GetString());

        var flat = YamlText.Parse("records:\n- type: result\n  id: result/a.b\n");

        Assert.True(flat.TryGetProperty("records", out var entries));
        Assert.Equal(JsonKind.Array, entries.Kind);
        Assert.Single(entries.Items);
        Assert.Equal(2, entries.Items[0].Line);
    }

    /// <summary>Double-quoted scalars unescape JSON escapes and quoted keys read with spaces.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesDoubleQuotedScalarsWithEscapes()
    {
        var root = YamlText.Parse("text: \"a\\\"b\\\\c\\/d\\b\\f\\n\\r\\t\\u00e9\\uD83D\\uDE00\"\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal(JsonKind.String, text.Kind);
        Assert.Equal("a\"b\\c/d\b\f\n\r\t" + (char)0xE9 + char.ConvertFromUtf32(0x1F600), text.GetString());

        var keyed = YamlText.Parse("\"a key\": value\n");

        Assert.True(keyed.TryGetProperty("a key", out var value));
        Assert.Equal("value", value.GetString());
    }

    /// <summary>Single-quoted scalars read literally with doubled quotes as one quote.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesSingleQuotedScalars()
    {
        var root = YamlText.Parse("text: 'it''s plain'\nempty: ''\nquote: 'a \"b\" c'\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("it's plain", text.GetString());
        Assert.True(root.TryGetProperty("empty", out var empty));
        Assert.Equal(JsonKind.String, empty.Kind);
        Assert.Equal("", empty.GetString());
        Assert.True(root.TryGetProperty("quote", out var quote));
        Assert.Equal("a \"b\" c", quote.GetString());
    }

    /// <summary>A clip literal keeps one trailing line end and blank lines inside.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesLiteralBlockClip()
    {
        var root = YamlText.Parse("text: |\n  line1\n  line2\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("line1\nline2\n", text.GetString());

        var blank = YamlText.Parse("text: |\n  a\n\n  b\n");

        Assert.True(blank.TryGetProperty("text", out var inner));
        Assert.Equal("a\n\nb\n", inner.GetString());
    }

    /// <summary>A strip literal drops the trailing line end.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesLiteralBlockStrip()
    {
        var root = YamlText.Parse("text: |-\n  line1\n  line2\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("line1\nline2", text.GetString());
    }

    /// <summary>A keep literal keeps its content and trailing line ends.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesLiteralBlockKeep()
    {
        var empty = YamlText.Parse("text: |+\n");

        Assert.True(empty.TryGetProperty("text", out var none));
        Assert.Equal("", none.GetString());

        var root = YamlText.Parse("text: |+\n  a\n\n  b\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal(JsonKind.String, text.Kind);
        var value = text.GetString()!;
        Assert.Contains("a\n\nb", value);
        Assert.EndsWith("\n", value);
    }

    /// <summary>Empty brackets read as empty collections.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesEmptyCollections()
    {
        var root = YamlText.Parse("a: []\nb: {}\n");

        Assert.True(root.TryGetProperty("a", out var array));
        Assert.Equal(JsonKind.Array, array.Kind);
        Assert.Equal(1, array.Line);
        Assert.Empty(array.Items);
        Assert.True(root.TryGetProperty("b", out var mapping));
        Assert.Equal(JsonKind.Object, mapping.Kind);
        Assert.Equal(2, mapping.Line);
        Assert.Empty(mapping.Properties);
        Assert.Equal("{\"a\":[],\"b\":{}}", root.ToJson());
    }

    /// <summary>Nulls, booleans and JSON numbers resolve; quoted lookalikes stay strings.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ResolvesCoreSchemaScalars()
    {
        var root = YamlText.Parse("nothing: null\ntilde: ~\ntruthy: true\nfalsy: False\ncount: 25\nprice: 1.5\nname: \"true\"\ncode: \"123\"\n");

        Assert.True(root.TryGetProperty("nothing", out var nothing));
        Assert.Equal(JsonKind.Null, nothing.Kind);
        Assert.True(root.TryGetProperty("tilde", out var tilde));
        Assert.Equal(JsonKind.Null, tilde.Kind);
        Assert.True(root.TryGetProperty("truthy", out var truthy));
        Assert.Equal(JsonKind.True, truthy.Kind);
        Assert.True(root.TryGetProperty("falsy", out var falsy));
        Assert.Equal(JsonKind.False, falsy.Kind);
        Assert.True(root.TryGetProperty("count", out var count));
        Assert.Equal(JsonKind.Number, count.Kind);
        Assert.Equal("25", count.ToJson());
        Assert.True(root.TryGetProperty("price", out var price));
        Assert.Equal(JsonKind.Number, price.Kind);
        Assert.Equal("1.5", price.ToJson());
        Assert.True(root.TryGetProperty("name", out var name));
        Assert.Equal(JsonKind.String, name.Kind);
        Assert.Equal("true", name.GetString());
        Assert.True(root.TryGetProperty("code", out var code));
        Assert.Equal(JsonKind.String, code.Kind);
        Assert.Equal("123", code.GetString());

        Assert.True(YamlText.ResolvesToNonString(""));
        Assert.True(YamlText.ResolvesToNonString("~"));
        Assert.True(YamlText.ResolvesToNonString("null"));
        Assert.True(YamlText.ResolvesToNonString("True"));
        Assert.True(YamlText.ResolvesToNonString("FALSE"));
        Assert.True(YamlText.ResolvesToNonString("123"));
        Assert.True(YamlText.ResolvesToNonString("-1.5e10"));
        Assert.False(YamlText.ResolvesToNonString("hello"));
        Assert.False(YamlText.ResolvesToNonString("true-ish"));
        Assert.False(YamlText.ResolvesToNonString("0123a"));
    }

    /// <summary>Comments are ignored; a comment-only document is empty.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesIgnoringComments()
    {
        var inline = YamlText.Parse("key: value # comment\n");

        Assert.True(inline.TryGetProperty("key", out var value));
        Assert.Equal("value", value.GetString());

        var hash = YamlText.Parse("key: #value\n");

        Assert.True(hash.TryGetProperty("key", out var empty));
        Assert.Equal(JsonKind.Null, empty.Kind);

        var later = YamlText.Parse("a: 1\n# comment\n");

        Assert.Equal("{\"a\":1}", later.ToJson());

        var full = Assert.Throws<JsonTextException>(() => YamlText.Parse("# comment\n"));
        Assert.Equal(1, full.Line);
        Assert.Equal("Expected a value; the document is empty at line 1.", full.Message);
    }

    /// <summary>Plain indicators read as strings; broken anchors and indicator-led values fail with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void HandlesAnchorsAliasesTagsWithLine()
    {
        var anchor = YamlText.Parse("key: &anchor\n");

        Assert.True(anchor.TryGetProperty("key", out var anchored));
        Assert.Equal(JsonKind.Null, anchored.Kind);

        var alias = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: *alias\n"));
        Assert.Equal(1, alias.Line);
        Assert.Equal("Expected valid YAML at line 1.", alias.Message);

        var tag = YamlText.Parse("key: !tag\n");

        Assert.True(tag.TryGetProperty("key", out var tagged));
        Assert.Equal(JsonKind.Null, tagged.Kind);

        var directive = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\nb: %directive\n"));
        Assert.Equal(2, directive.Line);
        Assert.Equal("Expected valid YAML at line 2.", directive.Message);

        var at = YamlText.Parse("key: @doc\n");

        Assert.True(at.TryGetProperty("key", out var atValue));
        Assert.Equal("@doc", atValue.GetString());

        var tick = YamlText.Parse("key: `code`\n");

        Assert.True(tick.TryGetProperty("key", out var tickValue));
        Assert.Equal("`code`", tickValue.GetString());

        var dash = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: - value\n"));
        Assert.Equal(1, dash.Line);
        Assert.Equal("Expected valid YAML at line 1.", dash.Message);

        var thrown = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: ? value\n"));
        Assert.Equal(1, thrown.Line);
        Assert.Equal("Expected valid YAML at line 1.", thrown.Message);

        var colon = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: : value\n"));
        Assert.Equal(1, colon.Line);
        Assert.Equal("Expected valid YAML at line 1.", colon.Message);
    }

    /// <summary>Document markers are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RefusesDocumentMarkersWithLine()
    {
        var start = Assert.Throws<JsonTextException>(() => YamlText.Parse("---\ntype: use-case\n"));
        Assert.Equal(1, start.Line);
        Assert.Equal("Expected one document with no --- or ... markers at line 1.", start.Message);

        var end = Assert.Throws<JsonTextException>(() => YamlText.Parse("...\n"));
        Assert.Equal(1, end.Line);
        Assert.Equal("Expected one document with no --- or ... markers at line 1.", end.Message);

        var later = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\n---\n"));
        Assert.Equal(2, later.Line);
        Assert.Equal("Expected one document with no --- or ... markers at line 2.", later.Message);
    }

    /// <summary>Folded block headers are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RefusesFoldedBlocksWithLine()
    {
        var folded = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: >\n  folded\n"));
        Assert.True(folded.Line == 1 || folded.Line == 2);
        Assert.Equal($"Expected a | block; folded > blocks are not allowed at line {folded.Line}.", folded.Message);

        var later = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\nb: >-\n"));
        Assert.True(later.Line == 2 || later.Line == 3);
        Assert.Equal($"Expected a | block; folded > blocks are not allowed at line {later.Line}.", later.Message);
    }

    /// <summary>Flow collections read with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesFlowCollections()
    {
        var array = YamlText.Parse("key: [a, b]\n");

        Assert.True(array.TryGetProperty("key", out var items));
        Assert.Equal(JsonKind.Array, items.Kind);
        Assert.Equal(1, items.Line);
        Assert.Equal(2, items.Items.Count);
        Assert.Equal("a", items.Items[0].GetString());
        Assert.Equal("b", items.Items[1].GetString());

        var mapping = YamlText.Parse("key: {a: b}\n");

        Assert.True(mapping.TryGetProperty("key", out var nested));
        Assert.Equal(JsonKind.Object, nested.Kind);
        Assert.True(nested.TryGetProperty("a", out var leaf));
        Assert.Equal("b", leaf.GetString());

        var spaced = YamlText.Parse("key: [ ]\n");

        Assert.True(spaced.TryGetProperty("key", out var empty));
        Assert.Equal(JsonKind.Array, empty.Kind);

        var later = YamlText.Parse("a: 1\nb: [x]\n");

        Assert.True(later.TryGetProperty("b", out var second));
        Assert.Equal(JsonKind.Array, second.Kind);
        Assert.Equal(2, second.Line);
        Assert.Single(second.Items);
        Assert.Equal("x", second.Items[0].GetString());
    }

    /// <summary>Continued plain text folds with a space.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ParsesMultilinePlainScalarsFolded()
    {
        var mapping = YamlText.Parse("key: value\n  continued\n");

        Assert.True(mapping.TryGetProperty("key", out var folded));
        Assert.Equal("value continued", folded.GetString());

        var sequence = YamlText.Parse("- a\n  continued\n");

        Assert.Equal(JsonKind.Array, sequence.Kind);
        Assert.Single(sequence.Items);
        Assert.Equal("a continued", sequence.Items[0].GetString());
    }

    /// <summary>Tab-indented lines are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RefusesTabsInIndentationWithLine()
    {
        var nested = Assert.Throws<JsonTextException>(() => YamlText.Parse("key:\n\tchild: 1\n"));
        Assert.Equal(2, nested.Line);
        Assert.Equal("Expected spaces, not tabs, for indentation at line 2.", nested.Message);

        var first = Assert.Throws<JsonTextException>(() => YamlText.Parse("  \tkey: 1\n"));
        Assert.Equal(1, first.Line);
        Assert.Equal("Expected spaces, not tabs, for indentation at line 1.", first.Message);
    }

    /// <summary>A second key in one mapping is refused with its line.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RefusesDuplicateKeysWithLine()
    {
        var top = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\na: 2\n"));
        Assert.Equal(2, top.Line);
        Assert.Equal("Expected no second 'a' key in one mapping at line 2.", top.Message);

        var nested = Assert.Throws<JsonTextException>(() => YamlText.Parse("a:\n  b: 1\n  b: 2\n"));
        Assert.Equal(3, nested.Line);
        Assert.Equal("Expected no second 'b' key in one mapping at line 3.", nested.Message);
    }

    /// <summary>Core-schema numbers JSON cannot hold are refused with their lines; quoted ones stay strings.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RefusesNonJsonNumbersWithLine()
    {
        var hex = Assert.Throws<JsonTextException>(() => YamlText.Parse("count: 0x1F\n"));
        Assert.Equal(1, hex.Line);
        Assert.Equal("Expected a JSON number or a quoted string, not '0x1F' at line 1.", hex.Message);

        var inf = Assert.Throws<JsonTextException>(() => YamlText.Parse("value: .inf\n"));
        Assert.Equal(1, inf.Line);
        Assert.Equal("Expected a JSON number or a quoted string, not '.inf' at line 1.", inf.Message);

        var plus = Assert.Throws<JsonTextException>(() => YamlText.Parse("delta: +1\n"));
        Assert.Equal(1, plus.Line);
        Assert.Equal("Expected a JSON number or a quoted string, not '+1' at line 1.", plus.Message);

        var octal = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\nb: 0o7\n"));
        Assert.Equal(2, octal.Line);
        Assert.Equal("Expected a JSON number or a quoted string, not '0o7' at line 2.", octal.Message);

        var quoted = YamlText.Parse("count: \"0x1F\"\n");
        Assert.True(quoted.TryGetProperty("count", out var kept));
        Assert.Equal(JsonKind.String, kept.Kind);
        Assert.Equal("0x1F", kept.GetString());
    }

    /// <summary>Every value carries the 1-based line where it starts.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ReportsValueLines()
    {
        var root = YamlText.Parse("a: 1\nb:\n  c: x\nd:\n  - one\n  - two\n");

        Assert.Equal(1, root.Line);
        Assert.True(root.TryGetProperty("a", out var first));
        Assert.Equal(1, first.Line);
        Assert.True(root.TryGetProperty("b", out var nested));
        Assert.Equal(3, nested.Line);
        Assert.True(nested.TryGetProperty("c", out var leaf));
        Assert.Equal(3, leaf.Line);
        Assert.Equal("x", leaf.GetString());
        Assert.True(root.TryGetProperty("d", out var sequence));
        Assert.Equal(5, sequence.Line);
        Assert.Equal(2, sequence.Items.Count);
        Assert.Equal(5, sequence.Items[0].Line);
        Assert.Equal(6, sequence.Items[1].Line);
    }

    /// <summary>An empty document, a misplaced start and trailing text are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RejectsEmptyAndMisplacedDocument()
    {
        var empty = Assert.Throws<JsonTextException>(() => YamlText.Parse(""));
        Assert.Equal(1, empty.Line);
        Assert.Equal("Expected a value; the document is empty at line 1.", empty.Message);

        var blank = Assert.Throws<JsonTextException>(() => YamlText.Parse("  \n\n"));
        Assert.Equal(1, blank.Line);
        Assert.Equal("Expected a value; the document is empty at line 1.", blank.Message);

        var indented = Assert.Throws<JsonTextException>(() => YamlText.Parse("  a: 1\n"));
        Assert.Equal(1, indented.Line);
        Assert.Equal("Expected the document to start at the first column at line 1.", indented.Message);

        var trailing = Assert.Throws<JsonTextException>(() => YamlText.Parse("- a\nb: 1\n"));
        Assert.Equal(2, trailing.Line);
        Assert.Equal("Expected valid YAML at line 2.", trailing.Message);
    }

    /// <summary>Unclosed quotes, stray text and bad scalars fail with their lines; a missing key space reads as a string.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RejectsMalformedKeysAndScalars()
    {
        var doubleOpen = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"oops\n"));
        Assert.Equal(1, doubleOpen.Line);
        Assert.Equal("Expected valid YAML at line 1.", doubleOpen.Message);

        var singleOpen = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 'oops\n"));
        Assert.Equal(1, singleOpen.Line);
        Assert.Equal("Expected valid YAML at line 1.", singleOpen.Message);

        var tail = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"x\" tail\n"));
        Assert.Equal(1, tail.Line);
        Assert.Equal("Expected valid YAML at line 1.", tail.Message);

        var nospace = YamlText.Parse("a:1\n");
        Assert.Equal(JsonKind.String, nospace.Kind);
        Assert.Equal("a:1", nospace.GetString());

        var singleKey = Assert.Throws<JsonTextException>(() => YamlText.Parse("'key': value\n"));
        Assert.Equal(1, singleKey.Line);
        Assert.Equal("Expected a 'key: value' line at line 1.", singleKey.Message);

        var colon = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: b: c\n"));
        Assert.Equal(1, colon.Line);
        Assert.Equal("Expected valid YAML at line 1.", colon.Message);

        var header = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: |x\n"));
        Assert.Equal(1, header.Line);
        Assert.Equal("Expected valid YAML at line 1.", header.Message);

        var escape = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"\\q\"\n"));
        Assert.Equal(1, escape.Line);
        Assert.Equal("Expected valid YAML at line 1.", escape.Message);

        var unicode = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"\\u12\"\n"));
        Assert.Equal(1, unicode.Line);
        Assert.Equal("Expected valid YAML at line 1.", unicode.Message);
    }

    /// <summary>Properties keep file order and write as compact JSON.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void KeepsPropertyOrderAndWritesJson()
    {
        var root = YamlText.Parse("b: 1\na: 2\n");

        Assert.Equal(2, root.Properties.Count);
        Assert.Equal("b", root.Properties[0].Key);
        Assert.Equal("a", root.Properties[1].Key);
        Assert.Equal("{\"b\":1,\"a\":2}", root.ToJson());

        var nested = YamlText.Parse("outer:\n  z: 1\n  a: 2\n");

        Assert.True(nested.TryGetProperty("outer", out var inner));
        Assert.Equal("z", inner.Properties[0].Key);
        Assert.Equal("a", inner.Properties[1].Key);
    }
}
