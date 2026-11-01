using LiteSpec.Model;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the past-tense derivation: irregulars, prefixed verbs, and consonant doubling.</summary>
public sealed class PastTenseTests
{
    static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    /// <summary>Common Irregular Verb Tenses Irregularly.</summary>
    /// <param name="verb">The irregular present-tense verb the case converts.</param>
    /// <param name="expected">The irregular past-tense form the case expects.</param>
    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Theory]
    [InlineData("override", "overridden")]
    [InlineData("withhold", "withheld")]
    [InlineData("set", "set")]
    [InlineData("send", "sent")]
    [InlineData("sell", "sold")]
    [InlineData("pay", "paid")]
    [InlineData("rebuild", "rebuilt")]
    [InlineData("undo", "undone")]
    [InlineData("rewrite", "rewritten")]
    // The InlineData values pair each irregular verb with its past tense.
    // Irregular verbs take their stored past-tense form.
    public void Common_Irregular_Verb_Tenses_Irregularly(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }

    /// <summary>Prefixed Verb Tenses Its Base.</summary>
    /// <param name="verb">The prefixed present-tense verb the case converts through its base.</param>
    /// <param name="expected">The past-tense form the case expects for the prefixed verb.</param>
    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Theory]
    [InlineData("untag", "untagged")]
    [InlineData("cover", "covered")]
    [InlineData("reset", "reset")]
    [InlineData("resend", "resent")]
    [InlineData("reread", "reread")]
    [InlineData("repay", "repaid")]
    [InlineData("unhide", "unhidden")]
    // The InlineData values pair each prefixed verb with its past tense.
    // A prefixed verb tenses through the past tense of its base verb.
    public void Prefixed_Verb_Tenses_Its_Base(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }

    /// <summary>One Syllable Consonant Vowel Consonant Verb Doubles.</summary>
    /// <param name="verb">The one-syllable verb the case converts with consonant doubling.</param>
    /// <param name="expected">The doubled-consonant past-tense form the case expects.</param>
    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Theory]
    [InlineData("shred", "shredded")]
    [InlineData("tag", "tagged")]
    [InlineData("stop", "stopped")]
    [InlineData("plan", "planned")]
    // The InlineData values pair each doubling verb with its past tense.
    // A stressed final consonant doubles before the past-tense ending.
    public void One_Syllable_Consonant_Vowel_Consonant_Verb_Doubles(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }

    /// <summary>Two Syllable Verb Stays Regular.</summary>
    /// <param name="verb">The two-syllable verb the case converts without doubling.</param>
    /// <param name="expected">The regular past-tense form the case expects.</param>
    /// <proves>litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb</proves>
    [Theory]
    [InlineData("offer", "offered")]
    [InlineData("visit", "visited")]
    [InlineData("open", "opened")]
    [InlineData("cancel", "cancelled")]
    // The InlineData values pair each regular verb with its past tense.
    // A two-syllable verb takes the regular past-tense ending.
    public void Two_Syllable_Verb_Stays_Regular(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }
}
