namespace LiteSpec.Tests.Shared;

/// <summary>Proves the shared HTML escaping, with and without the apostrophe.</summary>
public sealed class HtmlTextTests
{
    /// <summary>Each of the five metacharacters escapes.</summary>
    [Fact]
    public void Escape_Escapes_Every_Metacharacter()
    {
        Assert.Equal("&amp;", HtmlText.Escape("&"));
        Assert.Equal("&lt;", HtmlText.Escape("<"));
        Assert.Equal("&gt;", HtmlText.Escape(">"));
        Assert.Equal("&quot;", HtmlText.Escape("\""));
        Assert.Equal("&#39;", HtmlText.Escape("'"));
    }

    /// <summary>A mixed span escapes in place and plain text passes through.</summary>
    [Fact]
    public void Escape_Escapes_A_Mixed_Span_And_Leaves_Plain_Text()
    {
        Assert.Equal("a &lt;b&gt; &amp; &quot;c&quot; &#39;d&#39;", HtmlText.Escape("a <b> & \"c\" 'd'"));
        Assert.Equal("plain text", HtmlText.Escape("plain text"));
    }

    /// <summary>A missing or empty value escapes to the empty string.</summary>
    [Fact]
    public void Escape_Maps_Null_And_Empty_To_The_Empty_String()
    {
        Assert.Equal("", HtmlText.Escape(null));
        Assert.Equal("", HtmlText.Escape(""));
        Assert.Equal("", HtmlText.EscapeWithoutApostrophe(null));
    }

    /// <summary>The variant without the apostrophe leaves it as itself and escapes the rest.</summary>
    [Fact]
    public void EscapeWithoutApostrophe_Leaves_The_Apostrophe()
    {
        Assert.Equal("it's &lt;fine&gt; &amp; &quot;ok&quot;", HtmlText.EscapeWithoutApostrophe("it's <fine> & \"ok\""));
    }
}
