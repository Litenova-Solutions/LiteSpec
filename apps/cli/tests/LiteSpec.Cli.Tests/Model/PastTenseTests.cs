using LiteSpec.Model;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the past-tense derivation: irregulars, prefixed verbs, and consonant doubling.</summary>
public sealed class PastTenseTests
{
    static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

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
    public void Common_Irregular_Verb_Tenses_Irregularly(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }

    [Theory]
    [InlineData("untag", "untagged")]
    [InlineData("cover", "covered")]
    [InlineData("reset", "reset")]
    [InlineData("resend", "resent")]
    [InlineData("reread", "reread")]
    [InlineData("repay", "repaid")]
    [InlineData("unhide", "unhidden")]
    public void Prefixed_Verb_Tenses_Its_Base(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }

    [Theory]
    [InlineData("shred", "shredded")]
    [InlineData("tag", "tagged")]
    [InlineData("stop", "stopped")]
    [InlineData("plan", "planned")]
    public void One_Syllable_Consonant_Vowel_Consonant_Verb_Doubles(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }

    [Theory]
    [InlineData("offer", "offered")]
    [InlineData("visit", "visited")]
    [InlineData("open", "opened")]
    [InlineData("cancel", "cancelled")]
    public void Two_Syllable_Verb_Stays_Regular(string verb, string expected)
    {
        Assert.Equal(expected, UseCaseVocabulary.PastTenseOf(verb, Empty));
    }
}
