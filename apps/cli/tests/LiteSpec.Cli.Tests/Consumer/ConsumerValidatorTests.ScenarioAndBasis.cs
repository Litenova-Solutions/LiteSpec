using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for quality scenarios and decision bases.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>Quality requirement carrying its scenario.</summary>
    /// <proves>litespec/core/intent#state-each-quality-requirement-as-a-scenario</proves>
    [Fact(DisplayName = "quality requirement carrying its scenario")]
    public void QualityRequirementCarryingItsScenario()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, "docs/product/pricing.md", meta => QualityRecord(meta, []));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Quality requirement missing a scenario field.</summary>
    /// <param name="field">The quality scenario field the case removes, such as stimulus or measure.</param>
    /// <proves>litespec/core/intent#state-each-quality-requirement-as-a-scenario</proves>
    [Theory(DisplayName = "quality requirement missing a scenario field")]
    [InlineData("stimulus")]
    [InlineData("environment")]
    [InlineData("response")]
    [InlineData("measure")]
    [InlineData("dueBy")]
    // The InlineData values name each scenario field case.
    public void QualityRequirementMissingScenarioField(string field)
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, "docs/product/pricing.md", meta => QualityRecord(meta, [field]));
        AssertProblem(RunConsumer(folder.Path), $"missing required property '{field}'");
    }

    /// <summary>Functional requirement without scenario fields.</summary>
    /// <proves>litespec/core/intent#state-each-quality-requirement-as-a-scenario</proves>
    [Fact(DisplayName = "functional requirement without scenario fields")]
    public void FunctionalRequirementWithoutScenarioFields()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, "docs/product/pricing.md", meta =>
        {
            var record = FirstRecord(meta);
            record["kind"] = "functional";
            record.Remove("values");
        });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Technical decision naming a requirement in basedOn.</summary>
    /// <proves>litespec/core/model#keep-rules-and-targets-out-of-technical-decisions</proves>
    [Fact(DisplayName = "technical decision naming a requirement in basedOn")]
    public void TechnicalDecisionNamingRequirement()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(folder.Path, "docs/implementation/decisions/0002-order-claim-store.md", ConsumerFixture.Page(
            ConsumerFixture.Meta("""{"type":"decision","id":"decision/0002-order-claim-store","title":"Store order claims as documents","owner":"fixture","status":"approved","options":[{"name":"Documents","pros":"Each claim reads as one record.","cons":"Cross-claim reports need an index."}],"choice":"Documents","reopenedWhen":"Cross-claim reports pass one hundred a day.","decides":[],"basedOn":["requirement/pricing.flat-fee"],"approvedBy":"fixture","approvedOn":"2026-10-01","approvedHash":"00000000"}"""),
            """
            # Store Order Claims as Documents

            ## Context

            Each claim reads as one record with no joins.

            ## Decision

            The shop stores each claim as one document.

            ## Consequences

            The document store owns claim persistence.

            """));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>The first requirement entry on the pricing page.</summary>
    /// <param name="meta">The pricing page frontmatter.</param>
    /// <returns>The first record in the page records array.</returns>
    static JsonObject FirstRecord(JsonObject meta) => (JsonObject)((JsonArray)meta["records"]!)[0]!;

    /// <summary>Turns the first pricing record into a quality requirement missing the named fields.</summary>
    /// <param name="meta">The pricing page frontmatter to mutate.</param>
    /// <param name="missing">The scenario fields to omit from the record.</param>
    static void QualityRecord(JsonObject meta, string[] missing)
    {
        var record = FirstRecord(meta);
        record["kind"] = "quality";
        record.Remove("values");
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stimulus"] = "Ten thousand shoppers check out in the last hour of the sale.",
            ["environment"] = "Normal operation, one application instance down.",
            ["response"] = "Each order is stored and acknowledged.",
            ["measure"] = "Share of orders acknowledged within two seconds: at least 99 percent.",
            ["dueBy"] = "Holiday sale",
        };

        foreach (var (name, value) in fields)
        {
            if (!missing.Contains(name, StringComparer.Ordinal))
            {
                record[name] = value;
            }
        }
    }
}
