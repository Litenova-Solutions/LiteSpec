using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the record schema holds the cast's shape: the four entry lists, the typed figure value, and the
///     links, and rejects a cast that breaks one of them. The domain of an email is a model check, not a schema rule.
/// </summary>
/// <follows>litespec/core/model#keep-shape-rules-in-the-schema</follows>
public sealed class CastSchemaTests
{
    /// <summary>The record schema file, relative to the release root.</summary>
    const string SchemaFile = "schemas/record.schema.yaml";

    /// <summary>The Claimwick example cast, which the schema must accept.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "the Claimwick cast is accepted by the record schema")]
    public void ClaimwickCastIsAccepted()
    {
        var text = File.ReadAllText(Path.Combine(TestCli.Repository, "examples", "claimwick", ".litespec", "cast.yaml"));

        Assert.Empty(CastProblems(text));
    }

    /// <summary>A figure holding two typed values is rejected: a figure holds exactly one value.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a figure holding two typed values is rejected")]
    public void FigureWithTwoValuesIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            figures:
            - type: figure
              id: figure/train-fare
              title: Train fare
              value:
                money:
                  amount: '48.90'
                  currency: EUR
                text: about forty-nine euros
            """)));
    }

    /// <summary>A money amount written with a decimal comma is rejected: the amount is a decimal string with a point.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a money amount with a decimal comma is rejected")]
    public void MoneyAmountWithCommaIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            figures:
            - type: figure
              id: figure/train-fare
              title: Train fare
              value:
                money:
                  amount: '48,90'
                  currency: EUR
            """)));
    }

    /// <summary>A currency that is not three capital letters is rejected.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a currency that is not an ISO code is rejected")]
    public void CurrencyThatIsNotAnIsoCodeIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            figures:
            - type: figure
              id: figure/train-fare
              title: Train fare
              value:
                money:
                  amount: '48.90'
                  currency: euro
            """)));
    }

    /// <summary>A negative count is rejected: a count is a non-negative whole number.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a negative count is rejected")]
    public void NegativeCountIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            figures:
            - type: figure
              id: figure/nights
              title: Hotel nights
              value:
                count: -1
            """)));
    }

    /// <summary>A party identifier with capital letters is rejected: the identifier grammar is lowercase.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a party identifier with capital letters is rejected")]
    public void PartyIdentifierWithCapitalsIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            parties:
            - type: party
              id: party/Harbor-and-Pine
              name: Harbor and Pine
              uses: product
              description: A small consultancy.
            """)));
    }

    /// <summary>A person without an actor is rejected: every person plays one actor.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a person without an actor is rejected")]
    public void PersonWithoutActorIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            people:
            - type: person
              id: person/ava
              name: Ava
              does: Travels to clients.
            """)));
    }

    /// <summary>An occasion involving an actor is rejected: an occasion involves people and parties only.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "an occasion that involves an actor is rejected")]
    public void OccasionInvolvingAnActorIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            occasions:
            - type: occasion
              id: occasion/client-visit
              title: A client visit
              involves:
              - actor/employee
              scene: Ava takes a train.
            """)));
    }

    /// <summary>An entry placed in the wrong list is rejected: each entry states the type its list names.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "an entry in the wrong list is rejected")]
    public void EntryInTheWrongListIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            people:
            - type: party
              id: party/harbor-and-pine
              name: Harbor and Pine
              uses: product
              description: A small consultancy.
            """)));
    }

    /// <summary>A party with a use other than product or appears-in-data is rejected.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a party use other than product or appears-in-data is rejected")]
    public void PartyUseOutsideTheTwoKindsIsRejected()
    {
        Assert.NotEmpty(CastProblems(Cast("""
            parties:
            - type: party
              id: party/harbor-and-pine
              name: Harbor and Pine
              uses: shared
              description: A small consultancy.
            """)));
    }

    /// <summary>An email on a public domain is well formed, so the schema accepts it; the domain is a model check.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "an email on a public domain is well formed for the schema")]
    public void PublicEmailIsWellFormedForTheSchema()
    {
        Assert.Empty(CastProblems(Cast("""
            people:
            - type: person
              id: person/ava
              name: Ava
              actor: actor/employee
              email: ava@gmail.com
              does: Travels to clients.
            """)));
    }

    /// <summary>A cast with a field the schema does not name is rejected.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a cast with an unknown field is rejected")]
    public void CastWithUnknownFieldIsRejected()
    {
        Assert.NotEmpty(CastProblems("""
            type: cast
            id: cast/shop
            title: Shop Cast
            shoeSize: 42
            """));
    }

    /// <summary>A cast page with a title and its id is accepted with no entries.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "a cast page with no entries is accepted")]
    public void CastPageWithoutEntriesIsAccepted()
    {
        Assert.Empty(CastProblems("""
            type: cast
            id: cast/shop
            title: Shop Cast
            """));
    }

    /// <summary>A cast with the given entry lists beneath its page.</summary>
    /// <param name="lists">The YAML entry lists, each at the top level.</param>
    /// <returns>The cast record YAML.</returns>
    static string Cast(string lists) =>
        "type: cast\nid: cast/shop\ntitle: Shop Cast\n" + lists;

    /// <summary>The problems the record schema reports for a cast written as YAML.</summary>
    /// <param name="yaml">The cast record YAML.</param>
    /// <returns>The problem messages, empty when the schema accepts the cast.</returns>
    static List<string> CastProblems(string yaml)
    {
        Assert.True(RecordFile.TryParse(yaml, out _, out var tree, out var error), error);

        var schema = RecordText.Read(LiteSpecHome.PathTo(SchemaFile));

        return JsonSchema.Problems(SchemaFile, RecordFile.NodeOf(tree), "cast/shop", _ => schema);
    }
}
