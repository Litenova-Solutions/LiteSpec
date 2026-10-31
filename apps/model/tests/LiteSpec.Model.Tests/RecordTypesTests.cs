using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the topic record type and the Intent link verbs are registered.</summary>
public sealed class RecordTypesTests
{
    /// <summary>The topic is an Intent record type, and the requirement page style is gone.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact]
    public void Topic_Is_An_Intent_Record_Type()
    {
        Assert.Contains("topic", RecordTypes.All);
        Assert.Equal("intent", RecordTypes.LayerByType["topic"]);
        Assert.DoesNotContain("requirement-page", RecordTypes.PageStyles);
    }

    /// <summary>Cites links the six reason types and the requirements a journey shows.</summary>
    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Cites_Links_Reasons_And_Requirements()
    {
        Assert.True(RecordTypes.LinkTargets["cites"].SetEquals(
            new[] { "goal", "problem", "feature", "constraint", "research", "decision", "requirement" }));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("features"));
    }

    /// <summary>Addresses links the actors a page speaks to and the permission an event needs.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact]
    public void Addresses_Links_Actors_And_Permissions()
    {
        Assert.True(RecordTypes.LinkTargets["addresses"].SetEquals(
            new[] { "actor", "permission" }));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("audience"));
    }
}
