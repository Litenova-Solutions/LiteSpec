using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>Tests the follows checks against the marks-follows fixture consumer.</summary>
public sealed class FollowsCheckTests
{
    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class with follows marks reports nothing")]
    public void TechnicalClassWithFollowsReportsNothing()
    {
        using var temp = MarksFixture.Prepare("follows");

        var findings = MarksFixture.Check(temp.Path);

        foreach (var file in new[]
        {
            "CommitStep.cs",
            "ExportStep.cs",
            "LoanDocumentStore.cs",
            "LoanResponseWriter.cs",
            "SplitStepStore.cs",
            "SplitStepMark.cs",
            "ITextGateway.cs",
            "TextGatewayAdapter.cs",
        })
        {
            Assert.DoesNotContain(findings, finding => FileName(finding) == file);
        }
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class without a mark reports mark-missing")]
    public void TechnicalClassWithoutAMarkReportsMarkMissing()
    {
        using var temp = MarksFixture.Prepare("follows");

        var missing = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Equal("Technical class 'LoanCache' carries no mark.", finding.Message);
        Assert.Equal("LoanCache.cs", FileName(finding));
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class with only implements use-case reports nothing")]
    public void TechnicalClassWithOnlyImplementsReportsNothing()
    {
        using var temp = MarksFixture.Prepare("follows");

        var findings = MarksFixture.Check(temp.Path);

        Assert.DoesNotContain(findings, finding => FileName(finding) == "BorrowCopyValidator.cs");
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class with both verbs reports nothing")]
    public void TechnicalClassWithBothVerbsReportsNothing()
    {
        using var temp = MarksFixture.Prepare("follows");

        var findings = MarksFixture.Check(temp.Path);

        Assert.DoesNotContain(findings, finding => FileName(finding) == "BorrowCopyStore.cs");
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A technical class with only implements decision reports mark-unresolved")]
    public void TechnicalClassWithImplementsDecisionReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("follows");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "LoanDecisionClaim.cs");

        Assert.Contains("which is a decision record", finding.Message);
    }

    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact(DisplayName = "An implements aggregate on a technical class does not silence aggregate-unimplemented")]
    public void TechnicalImplementsAggregateDoesNotSilenceUnimplemented()
    {
        using var temp = MarksFixture.Prepare("follows");

        var findings = MarksFixture.Check(temp.Path);

        Assert.DoesNotContain(
            MarksFixture.WithCode(findings, "mark-missing"),
            finding => FileName(finding) == "LoanAggregateStore.cs");
        Assert.DoesNotContain(
            MarksFixture.WithCode(findings, "mark-unresolved"),
            finding => FileName(finding) == "LoanAggregateStore.cs");

        var unimplemented = Assert.Single(
            MarksFixture.WithCode(findings, "aggregate-unimplemented"),
            finding => finding.Subject == "aggregate/lending.loan");
        Assert.Contains("has no aggregate root carrying its mark", unimplemented.Message);
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A follows naming an unknown provision reports mark-unresolved")]
    public void UnknownProvisionReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("follows");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "LoanExporter.cs");

        Assert.Contains("which matches no provision of the carried release.", finding.Message);
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A follows naming a provision on an unapplied page reports mark-unresolved")]
    public void UnappliedProvisionReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("follows");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "LoanArchiver.cs");

        Assert.Contains("which sits on a page the project's stacks and extensions do not apply.", finding.Message);
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A follows naming an unknown decision reports mark-unresolved")]
    public void UnknownDecisionReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("follows");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "LoanIndexer.cs");

        Assert.Contains("which matches no decision or deviation.", finding.Message);
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A follows naming a retired decision reports mark-unresolved")]
    public void RetiredDecisionReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("follows");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "LegacyExporter.cs");

        Assert.Contains("which is retired.", finding.Message);
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "No other mark-unresolved fires in the follows fixture")]
    public void NoOtherMarkUnresolvedFires()
    {
        using var temp = MarksFixture.Prepare("follows");

        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved");

        Assert.Equal(5, unresolved.Count);
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A technical class naming one decision twice reports mark-count")]
    public void DuplicateFollowsReportsMarkCount()
    {
        using var temp = MarksFixture.Prepare("follows");

        var counts = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-count");

        var finding = Assert.Single(counts);
        Assert.Contains("names 'decision/0002-store-loans-as-documents' twice.", finding.Message);
        Assert.Equal("LoanCounter.cs", FileName(finding));
    }

    /// <proves>litespec/core/model#record-each-technical-choice-as-a-decision</proves>
    [Fact(DisplayName = "A technical decision no code follows reports decision-unfollowed")]
    public void UnfollowedDecisionReportsDecisionUnfollowed()
    {
        using var temp = MarksFixture.Prepare("follows");

        var unfollowed = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "decision-unfollowed");

        var finding = Assert.Single(unfollowed);
        Assert.Equal("Decision 'decision/0003-loan-cache-region' is followed by no code element.", finding.Message);
    }

    /// <proves>litespec/core/model#name-what-every-decision-decides</proves>
    [Fact(DisplayName = "A product decision with no decides reports decision-decides-nothing")]
    public void ProductDecisionWithoutDecidesReportsDecidesNothing()
    {
        using var temp = MarksFixture.Prepare("follows");

        var empty = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "decision-decides-nothing");

        var finding = Assert.Single(empty);
        Assert.Equal("Decision 'decision/0001-loan-threshold' names nothing it decides.", finding.Message);
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "Excluded shapes report nothing")]
    public void ExclusionsReportNothing()
    {
        using var temp = MarksFixture.Prepare("follows");

        var findings = MarksFixture.Check(temp.Path);

        foreach (var file in new[]
        {
            "LoanSummary.cs",
            "ILoanClock.cs",
            "LoanSettings.cs",
            "AddLoansMigration.cs",
            "LoanReports.cs",
            "LoanGeneratedClient.g.cs",
        })
        {
            Assert.DoesNotContain(findings, finding => FileName(finding) == file);
        }
    }

    /// <proves>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</proves>
    [Fact(DisplayName = "A follows mark in the attribute style resolves")]
    public void AttributeStyleFollowsResolves()
    {
        using var temp = WriteAttributeConsumer();

        var config = ConsumerConfig.Load(temp.Path);
        var model = ModelBuilder.Build(config);
        var findings = Checker.Check(model).Findings;

        var element = Assert.Single(model.Code.Elements, element => element.Class == "CommitStep");
        Assert.Equal(CodeKind.Technical, element.Kind);
        var mark = Assert.Single(element.Marks);
        Assert.Equal(MarkVerb.Follows, mark.Verb);
        Assert.Equal(MarkStyle.Attribute, mark.Style);
        Assert.Empty(MarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(MarksFixture.WithCode(findings, "mark-unresolved"));
        Assert.Empty(MarksFixture.WithCode(findings, "mark-style-mixed"));
        Assert.Empty(MarksFixture.WithCode(findings, "decision-unfollowed"));
    }

    static TempFolder WriteAttributeConsumer()
    {
        var temp = Cli.Temp("marks");

        try
        {
            Write(temp.Path, "litespec.project.json", """
                {"project": {"name": "Marks Follows Attribute Fixture"}, "size": "small", "shape": "server",
                  "stacks": ["dotnet"], "reviewedLiteSpecVersion": "3.0.0", "owners": ["lending-team"],
                  "marks": "attribute",
                  "paths": {"docs": "docs", "glossary": "docs/glossary.json", "testRoots": [], "frontends": []},
                  "stackConfig": {"dotnet": {"application": "apps/api/src/Library.Application"}}}
                """);
            Write(temp.Path, "docs/glossary.json", """{"schemaVersion": 2, "terms": [], "mannered": []}""");
            Write(temp.Path, "docs/implementation/decisions/0002-store-loans-as-documents.md", """
                ---
                {
                  "type": "decision",
                  "id": "decision/0002-store-loans-as-documents",
                  "title": "Store loans as documents",
                  "owner": "lending-team",
                  "status": "draft",
                  "decider": "lending-team",
                  "decideBy": "2030-01-31",
                  "options": [{"name": "Documents", "for": "Reads stay simple", "against": "Queries need an index"}],
                  "changeWhen": "Queries pass one hundred a day"
                }
                ---

                # Store loans as documents

                ## Context

                Loans read as one aggregate with no joins.

                ## Decision

                Store each loan as one document.

                ## Consequences

                The document store owns loan persistence.
                """);
            Write(temp.Path, "apps/api/src/Library.Application/Lending/Technical/CommitStep.cs", """
                namespace Library.Application.Lending.Technical;

                [Follows("decision/0002-store-loans-as-documents")]
                internal sealed class CommitStep
                {
                    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
                }
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void Write(string root, string relative, string text)
    {
        var file = Path.Combine(root, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }

    static string FileName(Finding finding) =>
        Path.GetFileName(finding.Path);
}
