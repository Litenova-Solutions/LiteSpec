using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the record schema carries no problem field on a feature: the folder alone states the
///     one problem a feature answers, so a feature record that names a problem is a finding.
/// </summary>
/// <follows>litespec/core/model#keep-shape-rules-in-the-schema</follows>
public sealed class FeatureProblemFieldTests
{
    /// <summary>The record schema file, relative to the release root.</summary>
    const string SchemaFile = "schemas/record.schema.yaml";

    /// <summary>A feature that names one problem other than its folder's is rejected.</summary>
    /// <proves>litespec/core/intent#answer-one-problem-with-each-feature</proves>
    [Fact]
    public void FeatureNamingAnotherProblemIsRejected()
    {
        var record = Node("feature", "feature/hold-queue", """{"summary": "Hold a copy.", "offering": "Readers hold a copy.", "solves": ["problem/other-pain"], "successCriteria": ["Readers hold copies."], "excludes": ["Returning copies."]}""");

        Assert.NotEmpty(SchemaProblems("feature/hold-queue", record));
    }

    /// <summary>A feature that names more than one problem is rejected.</summary>
    /// <proves>litespec/core/intent#answer-one-problem-with-each-feature</proves>
    [Fact]
    public void FeatureNamingTwoProblemsIsRejected()
    {
        var record = Node("feature", "feature/hold-queue", """{"summary": "Hold a copy.", "offering": "Readers hold a copy.", "solves": ["problem/lost-copies", "problem/other-pain"], "successCriteria": ["Readers hold copies."], "excludes": ["Returning copies."]}""");

        Assert.NotEmpty(SchemaProblems("feature/hold-queue", record));
    }

    /// <summary>A feature that names no problem is accepted: its folder states the problem it answers.</summary>
    /// <proves>litespec/core/intent#answer-one-problem-with-each-feature</proves>
    [Fact]
    public void FeatureNamingNoProblemIsAccepted()
    {
        var record = Node("feature", "feature/hold-queue", """{"summary": "Hold a copy.", "offering": "Readers hold a copy.", "successCriteria": ["Readers hold copies."], "excludes": ["Returning copies."]}""");

        Assert.Empty(SchemaProblems("feature/hold-queue", record));
    }

    /// <summary>The record as the schema reads it: the type, identifier and title, then the fields.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The record node.</returns>
    static JsonNode Node(string type, string id, string json)
    {
        var record = new JsonObject
        {
            [RecordFields.Type] = type,
            [RecordFields.Id] = id,
            [RecordFields.Title] = id,
        };

        foreach (var property in (JsonNode.Parse(json) as JsonObject)!)
        {
            record[property.Key] = property.Value?.DeepClone();
        }

        return record;
    }

    /// <summary>The problems the record schema reports for the record.</summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="record">The record node.</param>
    /// <returns>The problem messages, empty when the schema accepts the record.</returns>
    static List<string> SchemaProblems(string id, JsonNode record)
    {
        var schema = RecordText.Read(LiteSpecHome.PathTo(SchemaFile));

        return JsonSchema.Problems(SchemaFile, record, id, _ => schema);
    }
}
