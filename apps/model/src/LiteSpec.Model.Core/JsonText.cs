using System;
using System.Collections.Generic;
using System.Text;

namespace LiteSpec.Model;

/// <summary>
///     The JSON kinds a value carries.
/// </summary>
public enum JsonKind
{
    /// <summary>An object: named values in braces.</summary>
    Object,
    /// <summary>An array: values in brackets.</summary>
    Array,
    /// <summary>A string.</summary>
    String,
    /// <summary>A number, kept as written.</summary>
    Number,
    /// <summary>The literal true.</summary>
    True,
    /// <summary>The literal false.</summary>
    False,
    /// <summary>The literal null.</summary>
    Null,
}

/// <summary>
///     One JSON value with the 1-based line where it starts.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public sealed class JsonText
{
    private readonly List<KeyValuePair<string, JsonText>> properties;
    private readonly string value;

    private JsonText(JsonKind kind, int line, string value, List<KeyValuePair<string, JsonText>> properties, List<JsonText> items)
    {
        Kind = kind;
        Line = line;
        this.value = value;
        this.properties = properties;
        Items = items;
    }

    /// <summary>The value's kind.</summary>
    public JsonKind Kind { get; }

    /// <summary>The 1-based line where the value starts.</summary>
    public int Line { get; }

    /// <summary>The array's items, in order; empty when the value is no array.</summary>
    public IReadOnlyList<JsonText> Items { get; }

    /// <summary>
    ///     Reads one named value from the object. The last entry wins when two share a name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="property">The named value.</param>
    /// <returns>Whether the object carries the name.</returns>
    public bool TryGetProperty(string name, out JsonText property)
    {
        if (Kind == JsonKind.Object)
        {
            for (var index = properties.Count - 1; index >= 0; index--)
            {
                if (properties[index].Key == name)
                {
                    property = properties[index].Value;
                    return true;
                }
            }
        }

        property = null!;
        return false;
    }

    /// <summary>The string's unescaped text, or null when the value is no string.</summary>
    public string? GetString() => Kind == JsonKind.String ? value : null;

    /// <summary>
    ///     Parses the text as one JSON value.
    /// </summary>
    /// <param name="text">The JSON text.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="JsonTextException">The text is no JSON; the message names the line.</exception>
    public static JsonText Parse(string text) => new Parser(text).Parse();

    private sealed class Parser
    {
        private const int MaxDepth = 64;

        private readonly string text;
        private int at;
        private int line = 1;

        public Parser(string text) => this.text = text;

        public JsonText Parse()
        {
            SkipGaps();

            if (at >= text.Length)
            {
                throw Fail("no value");
            }

            var value = ParseValue(0);

            SkipGaps();

            if (at < text.Length)
            {
                throw Fail("one value then the end");
            }

            return value;
        }

        private JsonText ParseValue(int depth)
        {
            if (depth > MaxDepth)
            {
                throw Fail("nested past 64 levels");
            }

            var start = line;
            var peek = text[at];

            if (peek == '{')
            {
                return ParseObject(start, depth);
            }

            if (peek == '[')
            {
                return ParseArray(start, depth);
            }

            if (peek == '"')
            {
                return new JsonText(JsonKind.String, start, ParseString(), NoProperties(), NoItems());
            }

            if (peek == 't')
            {
                ReadLiteral("true");
                return new JsonText(JsonKind.True, start, "", NoProperties(), NoItems());
            }

            if (peek == 'f')
            {
                ReadLiteral("false");
                return new JsonText(JsonKind.False, start, "", NoProperties(), NoItems());
            }

            if (peek == 'n')
            {
                ReadLiteral("null");
                return new JsonText(JsonKind.Null, start, "", NoProperties(), NoItems());
            }

            if (peek == '-' || (peek >= '0' && peek <= '9'))
            {
                return new JsonText(JsonKind.Number, start, ParseNumber(), NoProperties(), NoItems());
            }

            throw Fail("a value");
        }

        private JsonText ParseObject(int start, int depth)
        {
            Next();
            SkipGaps();

            var properties = new List<KeyValuePair<string, JsonText>>();

            if (at < text.Length && text[at] == '}')
            {
                Next();
                return new JsonText(JsonKind.Object, start, "", properties, NoItems());
            }

            while (true)
            {
                SkipGaps();

                if (at >= text.Length || text[at] != '"')
                {
                    throw Fail("a name");
                }

                var name = ParseString();

                SkipGaps();

                if (at >= text.Length || text[at] != ':')
                {
                    throw Fail("a colon");
                }

                Next();
                SkipGaps();

                if (at >= text.Length)
                {
                    throw Fail("a value");
                }

                properties.Add(new KeyValuePair<string, JsonText>(name, ParseValue(depth + 1)));
                SkipGaps();

                if (at >= text.Length)
                {
                    throw Fail("a comma or a closing brace");
                }

                if (text[at] == ',')
                {
                    Next();
                    continue;
                }

                if (text[at] == '}')
                {
                    Next();
                    return new JsonText(JsonKind.Object, start, "", properties, NoItems());
                }

                throw Fail("a comma or a closing brace");
            }
        }

        private JsonText ParseArray(int start, int depth)
        {
            Next();
            SkipGaps();

            var items = new List<JsonText>();

            if (at < text.Length && text[at] == ']')
            {
                Next();
                return new JsonText(JsonKind.Array, start, "", NoProperties(), items);
            }

            while (true)
            {
                SkipGaps();

                if (at >= text.Length)
                {
                    throw Fail("a value");
                }

                items.Add(ParseValue(depth + 1));
                SkipGaps();

                if (at >= text.Length)
                {
                    throw Fail("a comma or a closing bracket");
                }

                if (text[at] == ',')
                {
                    Next();
                    continue;
                }

                if (text[at] == ']')
                {
                    Next();
                    return new JsonText(JsonKind.Array, start, "", NoProperties(), items);
                }

                throw Fail("a comma or a closing bracket");
            }
        }

        private string ParseString()
        {
            Next();

            var built = new StringBuilder();

            while (true)
            {
                if (at >= text.Length)
                {
                    throw Fail("a closing quote");
                }

                var peek = text[at];

                if (peek == '"')
                {
                    Next();
                    return built.ToString();
                }

                if (peek == '\\')
                {
                    built.Append(ParseEscape());
                    continue;
                }

                if (peek < ' ')
                {
                    throw Fail("no raw control character");
                }

                built.Append(peek);
                Next();
            }
        }

        private string ParseEscape()
        {
            Next();

            if (at >= text.Length)
            {
                throw Fail("an escape");
            }

            var peek = text[at];

            Next();

            switch (peek)
            {
                case '"': return "\"";
                case '\\': return "\\";
                case '/': return "/";
                case 'b': return "\b";
                case 'f': return "\f";
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case 'u': return ParseUnicode();
                default: throw Fail("an escape");
            }
        }

        private string ParseUnicode()
        {
            var high = ParseHex4();

            if (high >= 0xD800 && high <= 0xDBFF
                && at + 1 < text.Length
                && text[at] == '\\'
                && text[at + 1] == 'u')
            {
                Next();
                Next();

                var low = ParseHex4();

                if (low >= 0xDC00 && low <= 0xDFFF)
                {
                    return char.ConvertFromUtf32((high - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000);
                }

                throw Fail("a low surrogate");
            }

            return ((char)high).ToString();
        }

        private int ParseHex4()
        {
            if (at + 4 > text.Length)
            {
                throw Fail("four hex digits");
            }

            var value = 0;

            for (var index = 0; index < 4; index++)
            {
                var digit = text[at + index];
                int nibble;

                if (digit >= '0' && digit <= '9')
                {
                    nibble = digit - '0';
                }
                else if (digit >= 'a' && digit <= 'f')
                {
                    nibble = digit - 'a' + 10;
                }
                else if (digit >= 'A' && digit <= 'F')
                {
                    nibble = digit - 'A' + 10;
                }
                else
                {
                    throw Fail("four hex digits");
                }

                value = value * 16 + nibble;
            }

            at += 4;

            return value;
        }

        private string ParseNumber()
        {
            var start = at;

            if (text[at] == '-')
            {
                Next();
            }

            if (at >= text.Length)
            {
                throw Fail("a number");
            }

            if (text[at] == '0')
            {
                Next();
            }
            else if (text[at] >= '1' && text[at] <= '9')
            {
                while (at < text.Length && text[at] >= '0' && text[at] <= '9')
                {
                    Next();
                }
            }
            else
            {
                throw Fail("a number");
            }

            if (at < text.Length && text[at] == '.')
            {
                Next();

                if (at >= text.Length || text[at] < '0' || text[at] > '9')
                {
                    throw Fail("a number");
                }

                while (at < text.Length && text[at] >= '0' && text[at] <= '9')
                {
                    Next();
                }
            }

            if (at < text.Length && (text[at] == 'e' || text[at] == 'E'))
            {
                Next();

                if (at < text.Length && (text[at] == '+' || text[at] == '-'))
                {
                    Next();
                }

                if (at >= text.Length || text[at] < '0' || text[at] > '9')
                {
                    throw Fail("a number");
                }

                while (at < text.Length && text[at] >= '0' && text[at] <= '9')
                {
                    Next();
                }
            }

            return text.Substring(start, at - start);
        }

        private void ReadLiteral(string literal)
        {
            if (at + literal.Length <= text.Length
                && string.Compare(text, at, literal, 0, literal.Length, StringComparison.Ordinal) == 0)
            {
                for (var index = 0; index < literal.Length; index++)
                {
                    Next();
                }

                return;
            }

            throw Fail("a value");
        }

        private void SkipGaps()
        {
            while (at < text.Length)
            {
                var peek = text[at];

                if (peek != ' ' && peek != '\t' && peek != '\n' && peek != '\r')
                {
                    return;
                }

                Next();
            }
        }

        private void Next()
        {
            if (text[at] == '\n')
            {
                line++;
            }

            at++;
        }

        private static List<KeyValuePair<string, JsonText>> NoProperties() =>
            new List<KeyValuePair<string, JsonText>>();

        private static List<JsonText> NoItems() => new List<JsonText>();

        private JsonTextException Fail(string want) =>
            new JsonTextException($"Invalid JSON on line {line}: expected {want}.", line);
    }
}

/// <summary>
///     The parse error a JSON text carries: what was wanted, and the 1-based line.
/// </summary>
public sealed class JsonTextException : Exception
{
    /// <summary>Builds the error for the line.</summary>
    /// <param name="message">What was wanted, naming the line.</param>
    /// <param name="line">The 1-based line.</param>
    public JsonTextException(string message, int line)
        : base(message) => Line = line;

    /// <summary>The 1-based line holding the error.</summary>
    public int Line { get; }
}
