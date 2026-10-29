using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for reading marks in both styles from syntax.</summary>
public sealed class MarkReaderTests
{
    [Fact(DisplayName = "Both styles read the same id")]
    public void BothStylesReadTheSameId()
    {
        var xml = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var attribute = TestTrees.First<ClassDeclarationSyntax>("""
            [Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler;
            """);

        var fromXml = Assert.Single(MarkReader.Read(xml));
        var fromAttribute = Assert.Single(MarkReader.Read(attribute));

        Assert.Equal("use-case/spending.submit-expense", fromXml.RecordId);
        Assert.Equal(fromXml.RecordId, fromAttribute.RecordId);
        Assert.Equal(MarkStyle.Xml, fromXml.Style);
        Assert.Equal(MarkStyle.Attribute, fromAttribute.Style);
        Assert.Equal(MarkVerb.Implements, fromXml.Verb);
        Assert.Equal(MarkVerb.Implements, fromAttribute.Verb);
    }

    [Fact(DisplayName = "Proves marks read the scenario in both styles")]
    public void ProvesMarksReadTheScenario()
    {
        var xml = TestTrees.First<MethodDeclarationSyntax>("""
            internal sealed class SubmitExpenseTests
            {
                /// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>
                public void SubmittingWithAReceiptSucceeds() { }
            }
            """);
        var attribute = TestTrees.First<MethodDeclarationSyntax>("""
            internal sealed class SubmitExpenseTests
            {
                [Proves("scenario/submit-expense.submitting-with-a-receipt-succeeds")]
                public void SubmittingWithAReceiptSucceeds() { }
            }
            """);

        var fromXml = Assert.Single(MarkReader.Read(xml));
        var fromAttribute = Assert.Single(MarkReader.Read(attribute));

        Assert.Equal("scenario/submit-expense.submitting-with-a-receipt-succeeds", fromXml.RecordId);
        Assert.Equal(fromXml.RecordId, fromAttribute.RecordId);
        Assert.Equal(MarkVerb.Proves, fromXml.Verb);
        Assert.Equal(MarkVerb.Proves, fromAttribute.Verb);
    }

    [Fact(DisplayName = "A tag split over lines reads the id")]
    public void TagSplitOverLinesReadsTheId()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>
            /// use-case/spending.submit-expense
            /// </implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal("use-case/spending.submit-expense", mark.RecordId);
    }

    [Fact(DisplayName = "A tag split over lines reads the id without documentation mode")]
    public void TagSplitOverLinesReadsTheIdWithoutDocumentationMode()
    {
        var tree = TestTrees.ParseWithoutDocumentation("""
            /// <implements>
            /// use-case/spending.submit-expense
            /// </implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var member = TestTrees.First<ClassDeclarationSyntax>(tree);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal("use-case/spending.submit-expense", mark.RecordId);
    }

    [Fact(DisplayName = "A tree parsed without documentation mode reads marks from comment text")]
    public void TreeWithoutDocumentationModeReadsMarks()
    {
        var tree = TestTrees.ParseWithoutDocumentation("""
            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var member = TestTrees.First<ClassDeclarationSyntax>(tree);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal("use-case/spending.submit-expense", mark.RecordId);
        Assert.Equal(MarkStyle.Xml, mark.Style);
    }

    [Fact(DisplayName = "Two marks on one method read in order")]
    public void TwoMarksOnOneMethodReadInOrder()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            internal sealed class SubmitExpenseTests
            {
                /// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>
                /// <proves>scenario/submit-expense.submitting-twice-fails</proves>
                public void SubmittingExpenses() { }
            }
            """);

        var marks = MarkReader.Read(method);

        Assert.Equal(2, marks.Count);
        Assert.Equal("scenario/submit-expense.submitting-with-a-receipt-succeeds", marks[0].RecordId);
        Assert.Equal("scenario/submit-expense.submitting-twice-fails", marks[1].RecordId);
    }

    [Fact(DisplayName = "An attribute with a qualified name reads the id")]
    public void QualifiedAttributeReadsTheId()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            [LiteSpec.Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var global = TestTrees.First<ClassDeclarationSyntax>("""
            [global::LiteSpec.ImplementsAttribute("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler;
            """);

        var mark = Assert.Single(MarkReader.Read(member));
        var fromGlobal = Assert.Single(MarkReader.Read(global));

        Assert.Equal("use-case/spending.submit-expense", mark.RecordId);
        Assert.Equal("use-case/spending.submit-expense", fromGlobal.RecordId);
    }

    [Fact(DisplayName = "An attribute with the Attribute suffix reads the id")]
    public void AttributeSuffixReadsTheId()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            [ImplementsAttribute("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler;
            """);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal("use-case/spending.submit-expense", mark.RecordId);
        Assert.Equal(MarkStyle.Attribute, mark.Style);
    }

    [Fact(DisplayName = "An unclosed tag reads as a mark with an empty id")]
    public void UnclosedTagReadsAsEmptyMark()
    {
        var tree = TestTrees.ParseWithoutDocumentation("""
            /// <implements>use-case/spending.submit-expense
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var member = TestTrees.First<ClassDeclarationSyntax>(tree);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal(string.Empty, mark.RecordId);
        Assert.False(mark.HasRecordId);
        Assert.Equal(MarkVerb.Implements, mark.Verb);
    }

    [Fact(DisplayName = "An empty tag reads as a mark with an empty id")]
    public void EmptyTagReadsAsEmptyMark()
    {
        foreach (var code in new[]
        {
            """
            /// <implements></implements>
            internal sealed class SubmitExpenseCommandHandler;
            """,
            """
            /// <implements>   </implements>
            internal sealed class SubmitExpenseCommandHandler;
            """,
            """
            /// <implements/>
            internal sealed class SubmitExpenseCommandHandler;
            """,
        })
        {
            var member = TestTrees.First<ClassDeclarationSyntax>(code);

            var mark = Assert.Single(MarkReader.Read(member));

            Assert.Equal(string.Empty, mark.RecordId);
            Assert.False(mark.HasRecordId);
        }
    }

    [Fact(DisplayName = "A member without marks reads no marks")]
    public void MemberWithoutMarksReadsNone()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            /// <summary>Reimburses an approved expense.</summary>
            internal sealed class SubmitExpenseCommandHandler;
            """);

        Assert.Empty(MarkReader.Read(member));
    }

    [Fact(DisplayName = "A tag in another case reads no marks")]
    public void WrongCaseTagReadsNone()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            /// <Implements>use-case/spending.submit-expense</Implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);

        Assert.Empty(MarkReader.Read(member));
    }

    [Fact(DisplayName = "A plain comment tag is ignored when documentation is present")]
    public void PlainCommentTagIgnoredWithDocumentation()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            /// <summary>Reimburses an approved expense.</summary>
            // <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);

        Assert.Empty(MarkReader.Read(member));
    }

    [Fact(DisplayName = "An attribute with a const reference reads as a mark with an empty id")]
    public void ConstReferenceAttributeReadsAsEmptyMark()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseCommandHandler
            {
                private const string UseCase = "use-case/spending.submit-expense";
            }
            """);
        var marked = TestTrees.First<ClassDeclarationSyntax>("""
            [Implements(RecordIds.SubmitExpense)]
            internal sealed class SubmitExpenseCommandHandler;
            """);

        Assert.Empty(MarkReader.Read(member));
        var mark = Assert.Single(MarkReader.Read(marked));
        Assert.Equal(string.Empty, mark.RecordId);
        Assert.Equal(MarkStyle.Attribute, mark.Style);
    }

    [Fact(DisplayName = "An attribute without a string literal reads as a mark with an empty id")]
    public void NonLiteralAttributeReadsAsEmptyMark()
    {
        foreach (var code in new[]
        {
            """
            [Implements]
            internal sealed class SubmitExpenseCommandHandler;
            """,
            """
            [Implements(42)]
            internal sealed class SubmitExpenseCommandHandler;
            """,
            """
            [Implements("")]
            internal sealed class SubmitExpenseCommandHandler;
            """,
        })
        {
            var member = TestTrees.First<ClassDeclarationSyntax>(code);

            var mark = Assert.Single(MarkReader.Read(member));

            Assert.Equal(string.Empty, mark.RecordId);
        }
    }

    [Fact(DisplayName = "Whitespace inside a mark is trimmed")]
    public void WhitespaceInsideMarkIsTrimmed()
    {
        var xml = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>  use-case/spending.submit-expense  </implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var attribute = TestTrees.First<ClassDeclarationSyntax>("""
            [Implements("  use-case/spending.submit-expense  ")]
            internal sealed class SubmitExpenseCommandHandler;
            """);

        Assert.Equal("use-case/spending.submit-expense", Assert.Single(MarkReader.Read(xml)).RecordId);
        Assert.Equal("use-case/spending.submit-expense", Assert.Single(MarkReader.Read(attribute)).RecordId);
    }

    [Fact(DisplayName = "A mark reports its 1-based line and identifier span")]
    public void MarkReportsLineAndSpan()
    {
        var tree = TestTrees.Parse("""
            namespace Shop.Orders;

            /// <implements>use-case/spending.submit-expense</implements>
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var member = TestTrees.First<ClassDeclarationSyntax>(tree);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal(3, mark.Line);
        Assert.Equal("use-case/spending.submit-expense", tree.GetText().ToString(mark.Span));
    }

    [Fact(DisplayName = "An attribute mark reports its literal span")]
    public void AttributeMarkReportsLiteralSpan()
    {
        var tree = TestTrees.Parse("""
            [Implements("use-case/spending.submit-expense")]
            internal sealed class SubmitExpenseCommandHandler;
            """);
        var member = TestTrees.First<ClassDeclarationSyntax>(tree);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal(1, mark.Line);
        Assert.Equal("use-case/spending.submit-expense", tree.GetText().ToString(mark.Span));
    }

    [Fact(DisplayName = "Follows marks read the choice in both styles")]
    public void FollowsMarksReadTheChoice()
    {
        var xml = TestTrees.First<ClassDeclarationSyntax>("""
            /// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>
            internal sealed class CommitStep;
            """);
        var attribute = TestTrees.First<ClassDeclarationSyntax>("""
            [Follows("decision/0002-store-expenses-as-documents")]
            internal sealed class ExpenseRepository;
            """);
        var suffixed = TestTrees.First<ClassDeclarationSyntax>("""
            [global::LiteSpec.FollowsAttribute("deviation/export-review-bypass")]
            internal sealed class ExpenseExporter;
            """);

        var fromXml = Assert.Single(MarkReader.Read(xml));
        var fromAttribute = Assert.Single(MarkReader.Read(attribute));
        var fromSuffixed = Assert.Single(MarkReader.Read(suffixed));

        Assert.Equal("litespec/backend/persistence#commit-each-command-pipeline-once", fromXml.RecordId);
        Assert.Equal(MarkStyle.Xml, fromXml.Style);
        Assert.Equal(MarkVerb.Follows, fromXml.Verb);
        Assert.Equal("decision/0002-store-expenses-as-documents", fromAttribute.RecordId);
        Assert.Equal(MarkStyle.Attribute, fromAttribute.Style);
        Assert.Equal(MarkVerb.Follows, fromAttribute.Verb);
        Assert.Equal("deviation/export-review-bypass", fromSuffixed.RecordId);
        Assert.Equal(MarkVerb.Follows, fromSuffixed.Verb);
    }

    [Fact(DisplayName = "Follows marks read from comment text without documentation mode")]
    public void FollowsMarksReadWithoutDocumentationMode()
    {
        var tree = TestTrees.ParseWithoutDocumentation("""
            /// <follows>decision/0002-store-expenses-as-documents</follows>
            internal sealed class ExpenseRepository;
            """);
        var member = TestTrees.First<ClassDeclarationSyntax>(tree);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal("decision/0002-store-expenses-as-documents", mark.RecordId);
        Assert.Equal(MarkVerb.Follows, mark.Verb);
        Assert.Equal(MarkStyle.Xml, mark.Style);
    }

    [Fact(DisplayName = "Follows marks read beside implements marks in order")]
    public void FollowsMarksReadBesideImplements()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>use-case/spending.submit-expense</implements>
            /// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>
            internal sealed class SubmitExpenseCommandHandler;
            """);

        var marks = MarkReader.Read(member);

        Assert.Equal(2, marks.Count);
        Assert.Equal(MarkVerb.Implements, marks[0].Verb);
        Assert.Equal("use-case/spending.submit-expense", marks[0].RecordId);
        Assert.Equal(MarkVerb.Follows, marks[1].Verb);
        Assert.Equal("litespec/backend/persistence#commit-each-command-pipeline-once", marks[1].RecordId);
    }

    [Fact(DisplayName = "An empty follows tag reads as a mark with an empty id")]
    public void EmptyFollowsTagReadsAsEmptyMark()
    {
        var member = TestTrees.First<ClassDeclarationSyntax>("""
            /// <follows></follows>
            internal sealed class CommitStep;
            """);

        var mark = Assert.Single(MarkReader.Read(member));

        Assert.Equal(string.Empty, mark.RecordId);
        Assert.False(mark.HasRecordId);
        Assert.Equal(MarkVerb.Follows, mark.Verb);
    }
}
