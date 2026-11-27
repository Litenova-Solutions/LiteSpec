using LiteSpec.Maintain.Tests.Support;
using LiteSpec.Maintain.Validate;

namespace LiteSpec.Maintain.Tests.Validate;

/// <summary>Prose lint cases: paragraph length and banned phrases, each asserting its diagnostic code.</summary>
public sealed partial class StandardsValidatorTests
{
    /// <summary>Runs one case and asserts it reports exactly one diagnostic with the expected code.</summary>
    /// <param name="mutate">The change to apply to the fixture repository.</param>
    /// <param name="code">The expected diagnostic code.</param>
    static void CheckSingle(Action<string> mutate, string code)
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        mutate(temp.Path);
        var result = StandardsValidator.Validate(temp.Path);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(code, diagnostic.Code);
    }

    /// <summary>Builds a paragraph of exactly the given number of words.</summary>
    /// <param name="count">The word count.</param>
    /// <returns>The paragraph text.</returns>
    static string LongWords(int count) => string.Join(" ", Enumerable.Repeat("word", count));

    /// <summary>A 120-word paragraph passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "120-word paragraph passes")]
    public void Paragraph120Passes() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", LongWords(120))),
        []);

    /// <summary>A 121-word paragraph fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "121-word paragraph fails")]
    public void Paragraph121Fails() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", LongWords(121))),
        MaintainerDiagnostics.Codes.ProseParagraphLength);

    /// <summary>A long fenced code block passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "long fenced code block passes")]
    public void LongFencedBlockPasses() => Check(
        root => StandardsFixture.Write(root, "docs/tools/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.CommandPage, "node tools/sample.mjs [root]", LongWords(150))),
        []);

    /// <summary>A long table passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "long table passes")]
    public void LongTablePasses() => Check(
        root => StandardsFixture.Write(root, "docs/reference/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReferencePage, "One published value.", LongWords(150))),
        []);

    /// <summary>A long list item passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "long list item passes")]
    public void LongListItemPasses() => Check(
        root => StandardsFixture.Write(root, "docs/guide/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.GuidePage, "Read the applicable canonical provision.", LongWords(150))),
        []);

    /// <summary>A long block quote passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "long block quote passes")]
    public void LongBlockQuotePasses() => Check(
        root => StandardsFixture.Write(root, "docs/reference/sample.md", StandardsFixture.ReplaceFirst(StandardsFixture.ReferencePage, "The values above come from the repository manifest.", "> " + LongWords(150))),
        []);

    /// <summary>Banned phrase gains a home fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase gains a home fails")]
    public void BannedPhraseGainsAHome() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page shows where one topic gains a home.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase refuses other folders fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase refuses other folders fails")]
    public void BannedPhraseRefusesOtherFolders() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page refuses other folders for one topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase names no fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase names no fails")]
    public void BannedPhraseNamesNo() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page names no other topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase holds the values speaks fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase holds the values speaks fails")]
    public void BannedPhraseHoldsValuesSpeaks() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This record holds the values the summary speaks about.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase the homes of records fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase the homes of records fails")]
    public void BannedPhraseHomesOfRecords() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page lists the homes of records for one topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase seamlessly fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase seamlessly fails")]
    public void BannedPhraseSeamlessly() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page moves one topic seamlessly.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase instantly fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase instantly fails")]
    public void BannedPhraseInstantly() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page shows one topic instantly.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase effortlessly fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase effortlessly fails")]
    public void BannedPhraseEffortlessly() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page loads one topic effortlessly.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase unlock the power fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase unlock the power fails")]
    public void BannedPhraseUnlockThePower() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page helps readers unlock the power of one topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase leverage fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase leverage fails")]
    public void BannedPhraseLeverage() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page shows how teams leverage one topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase robust fails case-insensitively.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase robust fails case-insensitively")]
    public void BannedPhraseRobust() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines one ROBUST topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase cutting-edge fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase cutting-edge fails")]
    public void BannedPhraseCuttingEdge() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines one cutting-edge topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase game-changer fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase game-changer fails")]
    public void BannedPhraseGameChanger() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines one game-changer for topics.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase best-in-class fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase best-in-class fails")]
    public void BannedPhraseBestInClass() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines one best-in-class topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase world-class fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase world-class fails")]
    public void BannedPhraseWorldClass() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines one world-class topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>Banned phrase supercharge fails.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "banned phrase supercharge fails")]
    public void BannedPhraseSupercharge() => CheckSingle(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page shows how to supercharge one topic.")),
        MaintainerDiagnostics.Codes.ProseBannedPhrase);

    /// <summary>A phrase inside inline code passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "phrase inside inline code passes")]
    public void PhraseInInlineCodePasses() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines the `seamlessly` token.")),
        []);

    /// <summary>A verification line over 120 words passes: it is one structured field, not a prose paragraph.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "long verification line passes")]
    public void LongVerificationLinePasses() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "confirms the topic stays inside its declared boundary.", "confirms the topic stays inside its declared boundary, " + LongWords(130) + "."));
            StandardsFixture.RebuildIndex(root);
        },
        []);

    /// <summary>A phrase inside a block quote passes: the quote shows other text, such as the before side of a rewrite.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "phrase inside a block quote passes")]
    public void PhraseInBlockQuotePasses() => Check(
        root => StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "This page defines one repository topic.", "This page defines one repository topic.\n\n> The chain gains a home in the module and runs seamlessly.")),
        []);

    /// <summary>A phrase in a statement line passes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "phrase in a statement line passes")]
    public void PhraseInStatementLinePasses() => Check(
        root =>
        {
            StandardsFixture.Write(root, "docs/core/topic.md", StandardsFixture.ReplaceFirst(StandardsFixture.TopicPage, "Consumers MUST keep the topic inside its declared boundary.", "Consumers MUST keep the topic moving seamlessly inside its declared boundary."));
            StandardsFixture.RebuildIndex(root);
        },
        []);
}
