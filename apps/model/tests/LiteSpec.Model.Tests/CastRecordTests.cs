using System.Linq;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the cast record: its four entry lists read as entries of their types, its links and homes, and the retired page style.</summary>
public sealed class CastRecordTests
{
    /// <summary>A cast page with a party and a person reads each entry under the type its list names.</summary>
    /// <proves>litespec/core/model#keep-the-whole-specification-in-the-records</proves>
    [Fact(DisplayName = "a cast reads each entry as a record of the type its list names")]
    public void CastReadsEntriesUnderTheirListsTypes()
    {
        const string cast = """
            type: cast
            id: cast/claimwick
            title: Claimwick Cast
            parties:
            - type: party
              id: party/harbor-and-pine
              name: Harbor and Pine
            people:
            - type: person
              id: person/ava
              name: Ava
            """;

        var records = RecordFiles.Read(".litespec/cast.yaml", cast);

        Assert.Equal(
            new[] { "cast/claimwick", "party/harbor-and-pine", "person/ava" },
            records.Select(record => record.Id).ToArray());

        var party = records[1];
        Assert.Equal("party", party.Type);
        Assert.True(party.IsEntry);
        Assert.Equal("cast/claimwick", party.PageId);
        Assert.Equal("Harbor and Pine", party.Title);

        var person = records[2];
        Assert.Equal("person", person.Type);
        Assert.Equal("Ava", person.Title);
    }

    /// <summary>An entry that states another type than its list names is left out; the schema reports it.</summary>
    /// <proves>litespec/core/model#keep-the-whole-specification-in-the-records</proves>
    [Fact(DisplayName = "an entry stating another type than its list is left out of the records")]
    public void EntryUnderTheWrongListIsLeftOut()
    {
        const string cast = """
            type: cast
            id: cast/claimwick
            title: Claimwick Cast
            people:
            - type: party
              id: party/harbor-and-pine
              name: Harbor and Pine
            """;

        var records = RecordFiles.Read(".litespec/cast.yaml", cast);

        Assert.Equal(new[] { "cast/claimwick" }, records.Select(record => record.Id).ToArray());
    }

    /// <summary>The four cast entry types and the cast page form the cast, and the example-cast page style is gone.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "the cast types form one set and the example cast page style is retired")]
    public void CastTypesAreOneSetAndExampleCastIsRetired()
    {
        Assert.True(RecordTypes.CastTypes.SetEquals(["cast", "party", "person", "occasion", "figure"]));
        Assert.Contains("cast", RecordTypes.All);
        Assert.DoesNotContain("example-cast", RecordTypes.All);
        Assert.DoesNotContain("example-cast", RecordTypes.PageStyles);
    }

    /// <summary>The cast's links name the types they may point at: a party, a person or a term.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact(DisplayName = "the cast links name the types they point at")]
    public void CastLinksNameTheirTargets()
    {
        Assert.True(RecordTypes.LinkTargets["party"].SetEquals(["party"]));
        Assert.True(RecordTypes.LinkTargets["involves"].SetEquals(["person", "party"]));
        Assert.True(RecordTypes.LinkTargets["term"].SetEquals(["term"]));
    }

    /// <summary>The cast and its entries live in the root cast file, beside the glossary, and no other file.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "the cast and its entries live in the root cast file")]
    public void CastAndEntriesLiveInTheCastFile()
    {
        Assert.True(RecordFolders.Places("cast", [], "cast.yaml"));
        Assert.True(RecordFolders.Places("party", [], "cast.yaml"));
        Assert.True(RecordFolders.Places("person", [], "cast.yaml"));
        Assert.True(RecordFolders.Places("occasion", [], "cast.yaml"));
        Assert.True(RecordFolders.Places("figure", [], "cast.yaml"));
        Assert.False(RecordFolders.Places("figure", [], "glossary.yaml"));
    }

    /// <summary>A seeds tag names a cast entry, and no element mark reads it.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "a seeds tag names a cast entry and no element mark reads it")]
    public void SeedsTagIsReadApartFromElementMarks()
    {
        const string text = "/// <seeds>person/ava</seeds>\n/// <implements>use-case/spending.submit-expense</implements>";

        var seed = Assert.Single(MarkParser.ReadSeedTags(text));
        Assert.Equal(MarkVerb.Seeds, seed.Verb);
        Assert.Equal("person/ava", seed.RecordId);

        var element = Assert.Single(MarkParser.ReadTags(text));
        Assert.Equal(MarkVerb.Implements, element.Verb);
    }

    /// <summary>A TypeScript line comment names a cast entry as a seeds mark.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "a TypeScript comment names a cast entry as a seeds mark")]
    public void SeedsCommentNamesACastEntry()
    {
        const string text = "const fare = seed();\n// litespec: seeds figure/train-fare\n";

        var mark = Assert.Single(MarkParser.ReadSeedComments(text));
        Assert.Equal("figure/train-fare", mark.RecordId);
        Assert.Equal(MarkVerb.Seeds, mark.Verb);
    }

    /// <summary>A comment naming another verb and an empty seeds tag name no cast entry.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "a comment naming another verb and an empty seeds tag are no seeds mark")]
    public void OtherVerbAndEmptySeedsAreNoSeedsMark()
    {
        Assert.Empty(MarkParser.ReadSeedComments("// litespec: implements use-case/spending.submit-expense\n"));
        Assert.Empty(MarkParser.ReadSeedTags("/// <seeds></seeds>"));
    }

    /// <summary>The seeds verb has its documentation tag name, and the attribute form does not exist for it.</summary>
    /// <proves>litespec/core/model#declare-one-mark-style</proves>
    [Fact(DisplayName = "the seeds verb reads as the seeds tag and no attribute")]
    public void SeedsVerbHasTheTagNameOnly()
    {
        Assert.Equal("seeds", MarkVerb.Seeds.ToTagName());
        Assert.True(MarkVerbNames.TryParseTag("seeds", out var verb));
        Assert.Equal(MarkVerb.Seeds, verb);
        Assert.False(MarkVerbNames.TryParseAttributeName("Seeds", out _));
    }
}
