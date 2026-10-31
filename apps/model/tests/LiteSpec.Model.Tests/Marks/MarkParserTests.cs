using System;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests.Marks;

/// <summary>Tests for the shared mark text parser.</summary>
public sealed class MarkParserTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Tags read every verb with a trimmed id")]
    public void TagsReadEveryVerb()
    {
        var marks = MarkParser.ReadTags(
            "<implements>  use-case/spending.submit-expense  </implements> " +
            "<proves>scenario/submit-expense.succeeds</proves> " +
            "<follows>decision/0002-store-expenses-as-documents</follows>");

        Assert.Equal(3, marks.Count);
        Assert.Equal(MarkVerb.Implements, marks[0].Verb);
        Assert.Equal("use-case/spending.submit-expense", marks[0].RecordId);
        Assert.False(marks[0].FromAttribute);
        Assert.Equal(MarkVerb.Proves, marks[1].Verb);
        Assert.Equal("scenario/submit-expense.succeeds", marks[1].RecordId);
        Assert.Equal(MarkVerb.Follows, marks[2].Verb);
        Assert.Equal("decision/0002-store-expenses-as-documents", marks[2].RecordId);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Tags in another case read no marks")]
    public void WrongCaseTagsReadNone()
    {
        Assert.Empty(MarkParser.ReadTags("<Implements>use-case/spending.submit-expense</Implements>"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An unclosed, empty, or self-closing tag reads no mark")]
    public void MalformedTagsReadNone()
    {
        Assert.Empty(MarkParser.ReadTags("<implements>use-case/spending.submit-expense"));
        Assert.Empty(MarkParser.ReadTags("<implements></implements>"));
        Assert.Empty(MarkParser.ReadTags("<implements>   </implements>"));
        Assert.Empty(MarkParser.ReadTags("<implements/>"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Attributes read every verb with a trimmed id")]
    public void AttributesReadEveryVerb()
    {
        var marks = MarkParser.ReadAttributes(
            "[Implements(\"  use-case/spending.submit-expense  \")] " +
            "[Proves(\"scenario/submit-expense.succeeds\")] " +
            "[Follows(\"decision/0002-store-expenses-as-documents\")]");

        Assert.Equal(3, marks.Count);
        Assert.Equal(MarkVerb.Implements, marks[0].Verb);
        Assert.Equal("use-case/spending.submit-expense", marks[0].RecordId);
        Assert.True(marks[0].FromAttribute);
        Assert.Equal(MarkVerb.Proves, marks[1].Verb);
        Assert.Equal(MarkVerb.Follows, marks[2].Verb);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Attributes read through the generated qualifier and the Attribute suffix")]
    public void AttributesReadThroughGeneratedQualifier()
    {
        var marks = MarkParser.ReadAttributes(
            "[LiteSpec.Implements(\"use-case/spending.submit-expense\")] " +
            "[global::LiteSpec.ProvesAttribute(\"scenario/submit-expense.succeeds\")]");

        Assert.Equal(2, marks.Count);
        Assert.Equal("use-case/spending.submit-expense", marks[0].RecordId);
        Assert.Equal("scenario/submit-expense.succeeds", marks[1].RecordId);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Attributes qualified with another namespace read no marks")]
    public void DomainQualifiedAttributesReadNone()
    {
        Assert.Empty(MarkParser.ReadAttributes("[Spending.Implements(\"use-case/spending.submit-expense\")]"));
        Assert.Empty(MarkParser.ReadAttributes("[global::Spending.Follows(\"decision/0002-store-expenses-as-documents\")]"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Attributes read several entries of one list")]
    public void AttributesReadSeveralEntries()
    {
        var marks = MarkParser.ReadAttributes("[Implements(\"use-case/spending.submit-expense\"), Proves(\"scenario/submit-expense.succeeds\")]");

        Assert.Equal(2, marks.Count);
        Assert.Equal("use-case/spending.submit-expense", marks[0].RecordId);
        Assert.Equal("scenario/submit-expense.succeeds", marks[1].RecordId);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An attribute without a quoted id reads no mark")]
    public void MalformedAttributesReadNone()
    {
        Assert.Empty(MarkParser.ReadAttributes("[Implements]"));
        Assert.Empty(MarkParser.ReadAttributes("[Implements(42)]"));
        Assert.Empty(MarkParser.ReadAttributes("[Implements(\"\")]"));
        Assert.Empty(MarkParser.ReadAttributes("[Implements(\"  \")]"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "Both forms read in source order with their offsets")]
    public void BothFormsReadInOrder()
    {
        var text = "[Implements(\"use-case/spending.submit-expense\")] <proves>scenario/submit-expense.succeeds</proves>";
        var marks = MarkParser.Read(text);

        Assert.Equal(2, marks.Count);
        Assert.True(marks[0].FromAttribute);
        Assert.False(marks[1].FromAttribute);
        Assert.True(marks[0].Index < marks[1].Index);
        Assert.Equal(1, marks[0].Index);
        Assert.Equal(text.IndexOf('<', StringComparison.Ordinal), marks[1].Index);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A null or empty text reads no marks")]
    public void NullTextReadsNone()
    {
        Assert.Empty(MarkParser.Read(null));
        Assert.Empty(MarkParser.Read(""));
        Assert.Empty(MarkParser.ReadTags(null));
        Assert.Empty(MarkParser.ReadAttributes(null));
    }
}
