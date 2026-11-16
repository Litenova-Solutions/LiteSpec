using System.Text.Json;
using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Warning severity cases: style diagnostics print but do not fail validation.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Sentence, modal, casing, and wording codes are warnings.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "sentence, modal, casing, and wording codes are warnings")]
    public void StyleCodesAreWarnings()
    {
        string[] warnings =
        [
            MaintainerDiagnostics.Codes.ConventionDefaultSentence,
            MaintainerDiagnostics.Codes.ConventionReplacementWaiver,
            MaintainerDiagnostics.Codes.ConventionReplacementSentence,
            MaintainerDiagnostics.Codes.GlossaryDefinition,
            MaintainerDiagnostics.Codes.HeadingAction,
            MaintainerDiagnostics.Codes.PageHeadingCase,
            MaintainerDiagnostics.Codes.PageTitleCase,
            MaintainerDiagnostics.Codes.ProseNormativeLocation,
            MaintainerDiagnostics.Codes.ProvisionRestatesHeading,
            MaintainerDiagnostics.Codes.StandardDeviationNormative,
            MaintainerDiagnostics.Codes.StandardDeviationSentence,
            MaintainerDiagnostics.Codes.StandardInformativeNormative,
            MaintainerDiagnostics.Codes.StandardModalCount,
            MaintainerDiagnostics.Codes.StandardModalVocabulary,
            MaintainerDiagnostics.Codes.StandardSentenceCount,
            MaintainerDiagnostics.Codes.VerifyDuplicateEvidence,
            MaintainerDiagnostics.Codes.VerifyGenericEvidence,
            MaintainerDiagnostics.Codes.VerifyTemplatedEvidence,
            MaintainerDiagnostics.Codes.ValueStyle,
        ];

        Assert.Equal(warnings.Length, MaintainerDiagnostics.Warnings.Count);
        foreach (var code in warnings)
        {
            Assert.True(MaintainerDiagnostics.IsWarning(code), code);
        }

        foreach (var code in MaintainerDiagnostics.Warnings)
        {
            Assert.Contains(code, warnings);
        }
    }

    /// <summary>Strength, layout, and structural codes are errors.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "strength, layout, and structural codes are errors")]
    public void StrengthLayoutAndStructuralCodesAreErrors()
    {
        string[] errors =
        [
            MaintainerDiagnostics.Codes.StandardStrengthModal,
            MaintainerDiagnostics.Codes.StandardRequirementCount,
            MaintainerDiagnostics.Codes.StandardUnexpectedDeviation,
            MaintainerDiagnostics.Codes.StandardMissingDeviation,
            MaintainerDiagnostics.Codes.StandardMissingRequirement,
            MaintainerDiagnostics.Codes.StandardLabelOrder,
            MaintainerDiagnostics.Codes.StrengthLine,
            MaintainerDiagnostics.Codes.PageMissingSection,
            MaintainerDiagnostics.Codes.LinkBroken,
            MaintainerDiagnostics.Codes.ProvisionsStale,
            MaintainerDiagnostics.Codes.TemplateSchema,
            MaintainerDiagnostics.Codes.FieldNameShape,
        ];

        foreach (var code in errors)
        {
            Assert.False(MaintainerDiagnostics.IsWarning(code), code);
        }
    }

    /// <summary>Validate with only warnings exits zero and prints them.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "validate with only warnings exits zero and prints them")]
    public void WarningsOnlyExitsZero()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard", "# Topic standard"));
        StandardsFixture.RebuildIndex(temp.Path);

        var result = Cli.Run("validate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains(MaintainerDiagnostics.Codes.PageTitleCase, result.Output, StringComparison.Ordinal);
        Assert.Contains("(warning)", result.Output, StringComparison.Ordinal);
    }

    /// <summary>Validate JSON with only warnings reports ok with a warnings array.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "validate JSON with only warnings reports ok with a warnings array")]
    public void WarningsOnlyJsonReportsOk()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard", "# Topic standard"));
        StandardsFixture.RebuildIndex(temp.Path);

        var result = Cli.Run("validate", "--format", "json", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        using var document = JsonDocument.Parse(result.Stdout);
        Assert.True(document.RootElement.GetProperty("ok").GetBoolean());
        Assert.Empty(document.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Contains(
            document.RootElement.GetProperty("warnings").EnumerateArray(),
            warning => warning.GetProperty("code").GetString() == MaintainerDiagnostics.Codes.PageTitleCase);
    }

    /// <summary>Validate with an error and a warning exits one and prints both.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "validate with an error and a warning exits one and prints both")]
    public void ErrorAndWarningExitsOne()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "# Topic Standard", "# Topic standard"));
        StandardsFixture.Write(temp.Path, "docs/README.md", "# Documentation\n\nRead the [missing page](missing.md).\n");
        StandardsFixture.RebuildIndex(temp.Path);

        var result = Cli.Run("validate", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains(MaintainerDiagnostics.Codes.LinkBroken, result.Output, StringComparison.Ordinal);
        Assert.Contains(MaintainerDiagnostics.Codes.PageTitleCase, result.Output, StringComparison.Ordinal);
    }
}
