using System.Text.Json.Nodes;
using LiteSpec.Cli.Explore;

namespace LiteSpec.Cli.Tests.Explore;

/// <summary>Tests for the walk OpenAPI reader: operations, references, skipped routes, and denies.</summary>
public sealed class ExploreOpenApiTests
{
    /// <summary>Parsing lists the operations with their parameters and request schemas.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "parsing lists the operations with their parameters and request schemas")]
    public void ParseListsOperations()
    {
        var document = JsonNode.Parse(
        """
        {"openapi": "3.1.0", "paths": {
          "/expenses": {
            "get": {"operationId": "list-expenses", "parameters": [{"name": "status", "in": "query", "required": false, "schema": {"type": "string"}}]},
            "post": {"requestBody": {"content": {"application/json": {"schema": {"type": "object", "properties": {"title": {"type": "string"}}, "required": ["title"]}}}}}
          },
          "/expenses/{expenseId}": {
            "get": {"operationId": "read-expense"}
          }
        }}
        """)!.AsObject();
        var skipped = new List<(string Template, string Reason)>();

        var operations = ExploreOpenApi.ParseOperations(document, [], skipped);

        Assert.Equal(3, operations.Count);
        Assert.Empty(skipped);
        var list = operations[0];
        Assert.Equal("GET", list.Method);
        Assert.Equal("/expenses", list.PathTemplate);
        Assert.Equal("list-expenses", list.Name);
        Assert.Empty(list.PathParameters);
        Assert.Equal("status", Assert.Single(list.QueryParameters).Name);
        Assert.False(Assert.Single(list.QueryParameters).Required);
        Assert.Null(list.RequestSchema);
        var create = operations[1];
        Assert.Equal("POST", create.Method);
        Assert.Equal("POST /expenses", create.Name);
        Assert.NotNull(create.RequestSchema);
        Assert.Equal("application/json", create.RequestContentType);
        var read = operations[2];
        Assert.Equal(["expenseId"], read.PathParameters);
        Assert.True(read.IsRead);
        Assert.False(create.IsRead);
    }

    /// <summary>Parsing resolves shared schema and parameter references.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "parsing resolves shared schema and parameter references")]
    public void ParseResolvesReferences()
    {
        var document = JsonNode.Parse(
        """
        {"openapi": "3.1.0",
         "components": {
           "schemas": {"NewExpense": {"type": "object", "properties": {"title": {"type": "string"}}, "required": ["title"]}},
           "parameters": {"StatusFilter": {"name": "status", "in": "query", "required": true, "schema": {"type": "string"}}},
           "requestBodies": {"ExpenseBody": {"content": {"application/json": {"schema": {"$ref": "#/components/schemas/NewExpense"}}}}}
         },
         "paths": {
          "/expenses": {
            "get": {"parameters": [{"$ref": "#/components/parameters/StatusFilter"}]},
            "post": {"requestBody": {"$ref": "#/components/requestBodies/ExpenseBody"}}
          }
        }}
        """)!.AsObject();
        var skipped = new List<(string Template, string Reason)>();

        var operations = ExploreOpenApi.ParseOperations(document, [], skipped);

        Assert.Equal(2, operations.Count);
        var status = Assert.Single(operations[0].QueryParameters);
        Assert.Equal("status", status.Name);
        Assert.True(status.Required);
        Assert.NotNull(status.Schema);
        var schema = operations[1].RequestSchema as JsonObject;
        Assert.NotNull(schema);
        Assert.NotNull((schema["properties"] as JsonObject)?["title"]);
    }

    /// <summary>Parsing skips the OpenAPI document and health routes silently.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "parsing skips the OpenAPI document and health routes silently")]
    public void ParseSkipsPlumbingRoutes()
    {
        var document = JsonNode.Parse(
        """
        {"openapi": "3.1.0", "paths": {
          "/openapi/v1.json": {"get": {}},
          "/health": {"get": {}},
          "/expenses": {"get": {}}
        }}
        """)!.AsObject();
        var skipped = new List<(string Template, string Reason)>();

        var operations = ExploreOpenApi.ParseOperations(document, [], skipped);

        Assert.Equal("/expenses", Assert.Single(operations).PathTemplate);
        Assert.Empty(skipped);
    }

    /// <summary>Parsing denies matching operations with their reasons.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "parsing denies matching operations with their reasons")]
    public void ParseDeniesMatchingOperations()
    {
        var document = JsonNode.Parse(
        """
        {"openapi": "3.1.0", "paths": {
          "/expenses": {"get": {}, "delete": {}},
          "/session/sign-out": {"post": {}}
        }}
        """)!.AsObject();
        var skipped = new List<(string Template, string Reason)>();

        var operations = ExploreOpenApi.ParseOperations(document, ExploreDeny.Defaults(allowDelete: false), skipped);

        Assert.Equal("/expenses", Assert.Single(operations).PathTemplate);
        Assert.Equal("GET", Assert.Single(operations).Method);
        Assert.Equal(2, skipped.Count);
        Assert.Contains(skipped, entry => entry.Template == "/expenses");
        Assert.Contains(skipped, entry => entry.Template == "/session/sign-out");
    }

    /// <summary>A document without paths lists no operations.</summary>
    /// <proves>litespec/core/release#report-verification-exactly</proves>
    [Fact(DisplayName = "a document without paths lists no operations")]
    public void ParseWithoutPathsListsNothing()
    {
        var document = JsonNode.Parse("""{"openapi": "3.1.0"}""")!.AsObject();
        var skipped = new List<(string Template, string Reason)>();

        Assert.Empty(ExploreOpenApi.ParseOperations(document, [], skipped));
        Assert.Empty(skipped);
    }
}
