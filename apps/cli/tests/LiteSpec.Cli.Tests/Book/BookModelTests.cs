using System.Text.Json.Nodes;
using LiteSpec.Book;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book index reads the builder's judgements: the stored tier, with risk as the fallback.</summary>
public sealed class BookModelTests
{
    /// <summary>A record stating its tier reads that tier, whatever its risk says.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Tier_Prefers_The_Stored_Tier()
    {
        Assert.Equal(3, BookModel.TierOf(Record(3, """["money"]""")));
        Assert.Equal(1, BookModel.TierOf(Record(1, "[]")));
    }

    /// <summary>Without a stored tier, any risk value makes the record tier 1, including physical access.</summary>
    /// <param name="risk">The risk value on the record, which drives the tier 1 fallback.</param>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Theory]
    [InlineData("money")]
    [InlineData("physical-access")]
    [InlineData("personal-data")]
    [InlineData("access")]
    public void Tier_Falls_Back_To_Risk(string risk)
    {
        Assert.Equal(1, BookModel.TierOf(Record(null, $"[\"{risk}\"]")));
    }

    /// <summary>Without a stored tier or risk, a record reads tier 3.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Tier_Without_Risk_Reads_Three()
    {
        Assert.Equal(3, BookModel.TierOf(Record(null, "[]")));
    }

    static JsonObject Record(int? tier, string risk)
    {
        var record = new JsonObject
        {
            ["type"] = "requirement",
            ["id"] = "requirement/borrowing.max-wait",
            ["fields"] = (JsonObject)JsonNode.Parse($"{{\"risk\": {risk}}}")!,
        };
        if (tier.HasValue)
        {
            record["tier"] = tier.Value;
        }

        return record;
    }
}
