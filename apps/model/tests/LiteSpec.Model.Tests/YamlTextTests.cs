using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the YAML reader parses the strict subset, tracks lines and names errors.</summary>
public sealed class YamlTextTests
{
    /// <summary>CRLF line ends, and a block that ends on its last line's carriage return, read like LF.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Reads_Crlf_Line_Ends()
    {
        var crlf = YamlText.Parse("type: index\r\ntitle: Loans\r\nnote: |\r\n  First line.\r\n  Second line.\r\n");
        var cut = YamlText.Parse("type: index\r\ntitle: Loans\r");

        Assert.Equal(crlf.ToJson(), YamlText.Parse("type: index\ntitle: Loans\nnote: |\n  First line.\n  Second line.\n").ToJson());
        Assert.Equal("{\"type\":\"index\",\"title\":\"Loans\"}", cut.ToJson());
    }

    /// <summary>A nested mapping reads through objects, arrays and every scalar with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Parses_Block_Mapping_With_Nested_Values()
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
    public void Parses_Top_Level_Sequence()
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
    public void Parses_Double_Quoted_Scalars_With_Escapes()
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
    public void Parses_Single_Quoted_Scalars()
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
    public void Parses_Literal_Block_Clip()
    {
        var root = YamlText.Parse("text: |\n  line1\n  line2\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("line1\nline2\n", text.GetString());
        Assert.Equal(1, text.Line);

        var blank = YamlText.Parse("text: |\n  a\n\n  b\n");

        Assert.True(blank.TryGetProperty("text", out var inner));
        Assert.Equal("a\n\nb\n", inner.GetString());
    }

    /// <summary>A strip literal drops the trailing line end.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Parses_Literal_Block_Strip()
    {
        var root = YamlText.Parse("text: |-\n  line1\n  line2\n");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("line1\nline2", text.GetString());
        Assert.Equal(1, text.Line);
    }

    /// <summary>A keep literal keeps its content and trailing line ends.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Parses_Literal_Block_Keep()
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
    public void Parses_Empty_Collections()
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
    public void Resolves_Core_Schema_Scalars()
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

    /// <summary>A full-line comment, an inline comment and a hash-led value are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Refuses_Comments_With_Line()
    {
        var full = Assert.Throws<JsonTextException>(() => YamlText.Parse("# comment\n"));
        Assert.Equal(1, full.Line);
        Assert.Equal("Expected no comment at line 1.", full.Message);

        var inline = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: value # comment\n"));
        Assert.Equal(1, inline.Line);
        Assert.Equal("Expected no comment; quote a value that holds ' #' at line 1.", inline.Message);

        var hash = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: #value\n"));
        Assert.Equal(1, hash.Line);
        Assert.Equal("Expected no comment at line 1.", hash.Message);

        var later = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\n# comment\n"));
        Assert.Equal(2, later.Line);
        Assert.Equal("Expected no comment at line 2.", later.Message);
    }

    /// <summary>Anchors, aliases, tags, directives and indicator-led plain values are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Refuses_Anchors_Aliases_Tags_With_Line()
    {
        var anchor = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: &anchor\n"));
        Assert.Equal(1, anchor.Line);
        Assert.Equal("Expected no '&': anchors, aliases, tags and directives are not allowed; quote the value at line 1.", anchor.Message);

        var alias = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: *alias\n"));
        Assert.Equal(1, alias.Line);
        Assert.Equal("Expected no '*': anchors, aliases, tags and directives are not allowed; quote the value at line 1.", alias.Message);

        var tag = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: !tag\n"));
        Assert.Equal(1, tag.Line);
        Assert.Equal("Expected no '!': anchors, aliases, tags and directives are not allowed; quote the value at line 1.", tag.Message);

        var directive = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\nb: %directive\n"));
        Assert.Equal(2, directive.Line);
        Assert.Equal("Expected no '%': anchors, aliases, tags and directives are not allowed; quote the value at line 2.", directive.Message);

        var at = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: @doc\n"));
        Assert.Equal(1, at.Line);
        Assert.Equal("Expected no '@': anchors, aliases, tags and directives are not allowed; quote the value at line 1.", at.Message);

        var tick = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: `code`\n"));
        Assert.Equal(1, tick.Line);
        Assert.Equal("Expected no '`': anchors, aliases, tags and directives are not allowed; quote the value at line 1.", tick.Message);

        var dash = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: - value\n"));
        Assert.Equal(1, dash.Line);
        Assert.Equal("Expected a quoted value; a plain value cannot start with '- ' at line 1.", dash.Message);

        var thrown = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: ? value\n"));
        Assert.Equal(1, thrown.Line);
        Assert.Equal("Expected a quoted value; a plain value cannot start with '? ' at line 1.", thrown.Message);

        var colon = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: : value\n"));
        Assert.Equal(1, colon.Line);
        Assert.Equal("Expected a quoted value; a plain value cannot start with ': ' at line 1.", colon.Message);
    }

    /// <summary>Document markers are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Refuses_Document_Markers_With_Line()
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
    public void Refuses_Folded_Blocks_With_Line()
    {
        var folded = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: >\n  folded\n"));
        Assert.Equal(1, folded.Line);
        Assert.Equal("Expected a | block; folded > blocks are not allowed at line 1.", folded.Message);

        var later = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\nb: >-\n"));
        Assert.Equal(2, later.Line);
        Assert.Equal("Expected a | block; folded > blocks are not allowed at line 2.", later.Message);
    }

    /// <summary>Flow collections other than empty are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Refuses_Flow_Collections_With_Line()
    {
        var array = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: [a, b]\n"));
        Assert.Equal(1, array.Line);
        Assert.Equal("Expected block style; flow collections other than [] and {} are not allowed at line 1.", array.Message);

        var mapping = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: {a: b}\n"));
        Assert.Equal(1, mapping.Line);
        Assert.Equal("Expected block style; flow collections other than [] and {} are not allowed at line 1.", mapping.Message);

        var spaced = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: [ ]\n"));
        Assert.Equal(1, spaced.Line);
        Assert.Equal("Expected block style; flow collections other than [] and {} are not allowed at line 1.", spaced.Message);

        var later = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 1\nb: [x]\n"));
        Assert.Equal(2, later.Line);
        Assert.Equal("Expected block style; flow collections other than [] and {} are not allowed at line 2.", later.Message);
    }

    /// <summary>Continued plain text outside a literal block is refused with its line.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Refuses_Multiline_Plain_Scalars_With_Line()
    {
        var mapping = Assert.Throws<JsonTextException>(() => YamlText.Parse("key: value\n  continued\n"));
        Assert.Equal(2, mapping.Line);
        Assert.Equal("Expected a key at the mapping's indentation; continued text belongs in a | block at line 2.", mapping.Message);

        var sequence = Assert.Throws<JsonTextException>(() => YamlText.Parse("- a\n  continued\n"));
        Assert.Equal(2, sequence.Line);
        Assert.Equal("Expected an item at the sequence's indentation; continued text belongs in a | block at line 2.", sequence.Message);
    }

    /// <summary>Tab-indented lines are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Refuses_Tabs_In_Indentation_With_Line()
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
    public void Refuses_Duplicate_Keys_With_Line()
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
    public void Refuses_Non_Json_Numbers_With_Line()
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
    public void Reports_Value_Lines()
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
    public void Rejects_Empty_And_Misplaced_Document()
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
        Assert.Equal("Expected no text after the document's last value at this indentation at line 2.", trailing.Message);
    }

    /// <summary>Unclosed quotes, stray text, missing key space and bad headers are refused with their lines.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Rejects_Malformed_Keys_And_Scalars()
    {
        var doubleOpen = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"oops\n"));
        Assert.Equal(1, doubleOpen.Line);
        Assert.Equal("Expected a closing double quote on the same line at line 1.", doubleOpen.Message);

        var singleOpen = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: 'oops\n"));
        Assert.Equal(1, singleOpen.Line);
        Assert.Equal("Expected a closing single quote on the same line at line 1.", singleOpen.Message);

        var tail = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"x\" tail\n"));
        Assert.Equal(1, tail.Line);
        Assert.Equal("Expected nothing after the closing quote at line 1.", tail.Message);

        var nospace = Assert.Throws<JsonTextException>(() => YamlText.Parse("a:1\n"));
        Assert.Equal(1, nospace.Line);
        Assert.Equal("Expected a 'key: value' line at line 1.", nospace.Message);

        var singleKey = Assert.Throws<JsonTextException>(() => YamlText.Parse("'key': value\n"));
        Assert.Equal(1, singleKey.Line);
        Assert.Equal("Expected a 'key: value' line at line 1.", singleKey.Message);

        var colon = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: b: c\n"));
        Assert.Equal(1, colon.Line);
        Assert.Equal("Expected a quoted value; a plain value cannot hold ': ' at line 1.", colon.Message);

        var header = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: |x\n"));
        Assert.Equal(1, header.Line);
        Assert.Equal("Expected a block header of |, |- or |+ and nothing after it at line 1.", header.Message);

        var escape = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"\\q\"\n"));
        Assert.Equal(1, escape.Line);
        Assert.Equal("Expected a JSON escape, not '\\q' at line 1.", escape.Message);

        var unicode = Assert.Throws<JsonTextException>(() => YamlText.Parse("a: \"\\u12\"\n"));
        Assert.Equal(1, unicode.Line);
        Assert.Equal("Expected four hex digits after \\u at line 1.", unicode.Message);
    }

    /// <summary>Properties keep file order and write as compact JSON.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Keeps_Property_Order_And_Writes_Json()
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
