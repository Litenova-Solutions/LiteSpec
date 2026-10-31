using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the use-case verb table answers kinds, openings and declarations.</summary>
public sealed class UseCaseVerbsTests
{
    /// <summary>The baseline reading verbs name a query.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Theory]
    [InlineData("read")]
    [InlineData("list")]
    [InlineData("search")]
    [InlineData("export")]
    [InlineData("download")]
    public void Reading_Verb_Names_A_Query(string verb)
    {
        Assert.Equal(UseCaseVerbs.VerbKind.Query, UseCaseVerbs.KindOf(verb, []));
    }

    /// <summary>The baseline changing verbs name a command.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Theory]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("change")]
    [InlineData("grant")]
    [InlineData("revoke")]
    [InlineData("send")]
    [InlineData("retire")]
    public void Changing_Verb_Names_A_Command(string verb)
    {
        Assert.Equal(UseCaseVerbs.VerbKind.Command, UseCaseVerbs.KindOf(verb, []));
    }

    /// <summary>The model-written text verbs serve either kind.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Theory]
    [InlineData("answer")]
    [InlineData("draft")]
    [InlineData("summarize")]
    [InlineData("translate")]
    public void Either_Verb_Names_Either(string verb)
    {
        Assert.Equal(UseCaseVerbs.VerbKind.Either, UseCaseVerbs.KindOf(verb, []));
    }

    /// <summary>A project verb in query verbs names a query.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact]
    public void Project_Query_Verb_Names_A_Query()
    {
        Assert.Equal(UseCaseVerbs.VerbKind.Query, UseCaseVerbs.KindOf("borrow", ["borrow"]));
    }

    /// <summary>A project verb outside query verbs names either, never a query or a command.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact]
    public void Project_Verb_Outside_Query_Verbs_Names_Either()
    {
        Assert.Equal(UseCaseVerbs.VerbKind.Either, UseCaseVerbs.KindOf("borrow", []));
    }

    /// <summary>The baseline decides the kind even when a project query verb repeats it.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact]
    public void Baseline_Wins_Over_Project_Query_Verbs()
    {
        Assert.Equal(UseCaseVerbs.VerbKind.Command, UseCaseVerbs.KindOf("create", ["create"]));
    }

    /// <summary>The opening word of a name is its verb.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Theory]
    [InlineData("submit-expense", "submit")]
    [InlineData("read", "read")]
    public void Verb_Of_Returns_The_First_Word(string name, string verb)
    {
        Assert.Equal(verb, UseCaseVerbs.VerbOf(name, []));
    }

    /// <summary>A phrasal verb counts as one opening.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact]
    public void Verb_Of_Returns_The_Phrasal_Verb()
    {
        Assert.Equal("check-in", UseCaseVerbs.VerbOf("check-in-guest", ["check-in"]));
    }

    /// <summary>Without the phrasal entry the opening word is the verb.</summary>
    /// <proves>litespec/core/use-cases#keep-one-kind-per-verb</proves>
    [Fact]
    public void Verb_Of_Returns_The_First_Word_Without_The_Phrasal_Entry()
    {
        Assert.Equal("check", UseCaseVerbs.VerbOf("check-in-guest", []));
    }

    /// <summary>A baseline verb of any kind counts as declared.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Theory]
    [InlineData("read")]
    [InlineData("create")]
    [InlineData("answer")]
    public void Baseline_Verb_Is_Declared(string verb)
    {
        Assert.True(UseCaseVerbs.IsDeclared(verb, [], [], []));
    }

    /// <summary>A project verb, query verb or phrasal verb counts as declared.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Fact]
    public void Project_Additions_Are_Declared()
    {
        Assert.True(UseCaseVerbs.IsDeclared("borrow", ["borrow"], [], []));
        Assert.True(UseCaseVerbs.IsDeclared("inspect", [], ["inspect"], []));
        Assert.True(UseCaseVerbs.IsDeclared("check-in", [], [], ["check-in"]));
    }

    /// <summary>An unknown verb and a retired synonym count as undeclared.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Theory]
    [InlineData("frobnicate")]
    [InlineData("get")]
    public void Unknown_And_Retired_Verbs_Are_Undeclared(string verb)
    {
        Assert.False(UseCaseVerbs.IsDeclared(verb, [], [], []));
    }

    /// <summary>Each retired synonym names the verb that replaces it.</summary>
    /// <proves>litespec/core/use-cases#start-every-use-case-with-a-declared-verb</proves>
    [Theory]
    [InlineData("get", "read")]
    [InlineData("delete", "remove")]
    [InlineData("update", "change")]
    public void Retired_Synonym_Names_Its_Replacement(string synonym, string verb)
    {
        Assert.Equal(verb, UseCaseVerbs.Retired[synonym]);
    }
}
