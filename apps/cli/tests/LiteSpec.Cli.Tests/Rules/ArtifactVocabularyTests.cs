using LiteSpec.Authoring;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves the settled artifact vocabulary the scope page declares.</summary>
public sealed class ArtifactVocabularyTests
{
    /// <summary>The scope page names every code kind the path rules classify.</summary>
    [Fact]
    public void ArtifactVocabularyNamesEveryCodeKind()
    {
        var vocabulary = Scopes.Load(TestCli.Repository);

        Assert.True(vocabulary.Readable);
        Assert.Contains("use-case", vocabulary.Artifacts);
        Assert.Contains("validator", vocabulary.Artifacts);
        Assert.Contains("reaction", vocabulary.Artifacts);
        Assert.Contains("port", vocabulary.Artifacts);
        Assert.Contains("repository", vocabulary.Artifacts);
        Assert.Contains("any", vocabulary.Artifacts);
        Assert.True(vocabulary.IsArtifact("validator"));
        Assert.True(vocabulary.IsArtifact("record:use-case"));
    }

    /// <summary>The value any is the layer-wide artifact and stands alone.</summary>
    [Fact]
    public void LayerWideArtifactIsTheValueAny()
    {
        Assert.True(Scopes.IsConcernWide("any"));
        Assert.True(Scopes.IsConcernWide(" any "));
        Assert.False(Scopes.IsConcernWide("aggregate"));
        Assert.Equal(Scopes.AnyArtifact, "any");
    }

    /// <summary>Several artifact values split on the comma that separates them.</summary>
    [Fact]
    public void ArtifactValuesSplitOnComma()
    {
        Assert.Equal(["command-handler", "query-handler"], Scopes.ArtifactValues("command-handler, query-handler"));
        Assert.Equal(["any"], Scopes.ArtifactValues("any"));
        Assert.Empty(Scopes.ArtifactValues(""));
    }
}
