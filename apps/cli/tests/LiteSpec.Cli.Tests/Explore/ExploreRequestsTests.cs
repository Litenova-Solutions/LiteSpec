using System.Text.Json.Nodes;
using LiteSpec.Cli.Explore;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for the walk request builder: collected ids, schema bodies, and seed determinism.</summary>
public sealed class ExploreRequestsTests
{
    /// <summary>A path fills its parameters from the collected values.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a path fills its parameters from the collected values")]
    public void BuildPathUsesCollectedIds()
    {
        var identifiers = new ExploreIdentifiers();
        identifiers.Collect(JsonNode.Parse("""{"expenseId": "e-42"}"""));
        var operation = new ExploreOperation("GET", "/expenses/{expenseId}", "read-expense", ["expenseId"], [], null, null);

        Assert.Equal("/expenses/e-42", ExploreRequests.BuildPath(operation, identifiers, new Random(1)));
    }

    /// <summary>A path without a collected value falls back to a GUID.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a path without a collected value falls back to a GUID")]
    public void BuildPathFallsBackToGuid()
    {
        var operation = new ExploreOperation("GET", "/expenses/{expenseId}", "read-expense", ["expenseId"], [], null, null);

        var path = ExploreRequests.BuildPath(operation, new ExploreIdentifiers(), new Random(1));

        Assert.StartsWith("/expenses/", path, StringComparison.Ordinal);
        Assert.True(Guid.TryParse(path["/expenses/".Length..], out _));
    }

    /// <summary>A body follows its schema: required properties are present with valid shapes.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a body follows its schema: required properties are present with valid shapes")]
    public void BuildBodyFollowsSchema()
    {
        var schema = JsonNode.Parse("""{"type": "object", "properties": {"title": {"type": "string"}, "count": {"type": "integer"}}, "required": ["title"]}""");
        var operation = new ExploreOperation("POST", "/expenses", "create-expense", [], [], schema, "application/json");

        var body = ExploreRequests.BuildBody(operation, new Random(1)) as JsonObject;

        Assert.NotNull(body);
        Assert.True(body["title"] is JsonValue title && title.TryGetValue<string>(out _));
    }

    /// <summary>An operation without a schema sends no body.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "an operation without a schema sends no body")]
    public void BuildBodyWithoutSchemaSendsNothing()
    {
        var operation = new ExploreOperation("GET", "/expenses", "list-expenses", [], [], null, null);

        Assert.Null(ExploreRequests.BuildBody(operation, new Random(1)));
    }

    /// <summary>The same seed builds the same body twice.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "the same seed builds the same body twice")]
    public void SameSeedBuildsSameBody()
    {
        var schema = JsonNode.Parse("""{"type": "object", "properties": {"title": {"type": "string"}, "count": {"type": "integer"}}}""");
        var operation = new ExploreOperation("POST", "/expenses", "create-expense", [], [], schema, "application/json");

        var first = ExploreRequests.BuildBody(operation, new Random(3))?.ToJsonString();
        var second = ExploreRequests.BuildBody(operation, new Random(3))?.ToJsonString();

        Assert.NotNull(first);
        Assert.Equal(first, second);
    }
}
