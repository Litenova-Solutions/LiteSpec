using System.Text.Json.Nodes;
using LiteSpec.Authoring;
using LiteSpec.Schemas;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Schema, template-index, and template cases, each asserting its diagnostic codes.</summary>
public sealed partial class StandardsValidatorTests
{
    static readonly string OneOfSchema =
        "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\"," +
        "\"type\":\"object\"," +
        "\"properties\":{\"selectedExtensions\":{\"type\":\"array\",\"items\":{\"oneOf\":[" +
        "{\"type\":\"string\",\"pattern\":\"^[a-z][a-z0-9-]*$\"}," +
        "{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"id\",\"criterion\"]," +
        "\"properties\":{\"id\":{\"type\":\"string\"},\"criterion\":{\"type\":\"string\"}}}" +
        "]}}}}";

    /// <summary>Schema id naming another home or release.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema id naming another home or release")]
    public void SchemaIdNamingAnotherRelease() => Check(
        root =>
        {
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$id\":\"https://example.com/schemas/record.schema.json\",\"type\":\"object\"}");
        },
        ["SCHEMA_ID"]);

    /// <summary>Schema id naming the published home and release.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema id naming the published home and release")]
    public void SchemaIdNamingThePublishedRelease()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
        StandardsFixture.Write(temp.Path, "schemas/record.schema.json", "{\"$id\":\"https://litespec.dev/4.0.0/schemas/record.schema.json\",\"type\":\"object\"}");

        var result = StandardsValidator.Validate(temp.Path);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "SCHEMA_ID");
    }

    /// <summary>Schema file that does not parse.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema file that does not parse")]
    public void SchemaFileThatDoesNotParse() => Check(
        root =>
        {
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
            StandardsFixture.Write(root, "schemas/record.schema.json", "{");
        },
        ["SCHEMA_JSON"]);

    /// <summary>Schema with no $id.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema with no $id")]
    public void SchemaWithNoId() => Check(
        root =>
        {
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"type\":\"object\"}");
        },
        ["SCHEMA_ID"]);

    /// <summary>Invalid schema consumer.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "invalid schema consumer")]
    public void InvalidSchemaConsumer() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"required\":[\"version\"],\"properties\":{\"version\":{\"const\":\"1.0.0\"}},\"additionalProperties\":false}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Unsupported schema keyword.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unsupported schema keyword")]
    public void UnsupportedSchemaKeyword() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"example\":{\"version\":\"4.0.0\"}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_UNSUPPORTED_KEYWORD"]);

    /// <summary>Unsupported schema keyword inside a branch.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unsupported schema keyword inside a branch")]
    public void UnsupportedSchemaKeywordInBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"anyOf\":[{\"type\":\"object\",\"properties\":{\"version\":{\"example\":\"4.0.0\"}}}]}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_UNSUPPORTED_KEYWORD"]);

    /// <summary>Unsupported schema keyword inside propertyNames.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unsupported schema keyword inside propertyNames")]
    public void UnsupportedSchemaKeywordInPropertyNames() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"propertyNames\":{\"type\":\"string\",\"example\":\"version\"}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_UNSUPPORTED_KEYWORD"]);

    /// <summary>Schema upper bound.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema upper bound")]
    public void SchemaUpperBound() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"properties\":{\"version\":{\"type\":\"string\",\"maxLength\":3}}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Schema anyOf branch.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema anyOf branch")]
    public void SchemaAnyOfBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"properties\":{\"version\":{\"anyOf\":[{\"type\":\"integer\"},{\"type\":\"boolean\"}]}}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Schema dependent property.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema dependent property")]
    public void SchemaDependentProperty() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"dependentRequired\":{\"version\":[\"name\"]}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Schema property name shape.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema property name shape")]
    public void SchemaPropertyNameShape() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"propertyNames\":{\"pattern\":\"^[a-z]+$\"}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"Version\":\"4.0.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Schema pattern properties.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema pattern properties")]
    public void SchemaPatternProperties() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"patternProperties\":{\"^v\":{\"type\":\"integer\"}},\"additionalProperties\":false}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"4.0.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Schema array bounds.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema array bounds")]
    public void SchemaArrayBounds() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"properties\":{\"areas\":{\"type\":\"array\",\"maxItems\":1}}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"areas\":[\"CORE\",\"EXT\"]}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Schema JSON.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema JSON")]
    public void SchemaJson() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_JSON"]);

    /// <summary>Project template carrying a removed overrides entry.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "project template carrying a removed overrides entry")]
    public void ProjectTemplateWithRemovedOverrides() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-project.schema.json", StandardsFixture.ShippedProjectSchema());
            StandardsFixture.Write(root, "templates/consumer/project.json", "{\"overrides\":[{\"provisionId\":\"litespec/core/topic#keep-the-topic-bounded\"}]}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Value matches no oneOf branch.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "value matches no oneOf branch")]
    public void ValueMatchesNoOneOfBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-project.schema.json", OneOfSchema);
            StandardsFixture.Write(root, "templates/consumer/project.json", "{\"selectedExtensions\":[{\"id\":\"outbox\",\"criterion\":\"Durable delivery.\",\"scope\":\"project\"}]}");
        },
        ["SCHEMA_INVALID"]);

    /// <summary>Value matches exactly one oneOf branch.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "value matches exactly one oneOf branch")]
    public void ValueMatchesOneOneOfBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-project.schema.json", OneOfSchema);
            StandardsFixture.Write(root, "templates/consumer/project.json", "{\"selectedExtensions\":[\"outbox\",{\"id\":\"jobs\",\"criterion\":\"Holds expire on a timer.\"}]}");
        },
        [], ["SCHEMA_INVALID"]);

    /// <summary>Template index names a missing file.</summary>
    /// <proves>litespec/core/principles#keep-one-authored-source</proves>
    [Fact(DisplayName = "template index names a missing file")]
    public void TemplateIndexNamesMissingFile() => Check(
        root =>
        {
            StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample\n");
            StandardsFixture.Write(root, "templates/consumer/README.md", "# Templates\n\n## Intent\n\nUse these starting points.\n\n| Template | Target | Purpose |\n|:---|:---|:---|\n| `sample.md` | `docs/sample.md` | Present. |\n| `absent.md` | `docs/absent.md` | Renamed away. |\n");
        },
        ["TEMPLATE_INDEX_PATH"]);

    /// <summary>Template index omits a tracked template.</summary>
    /// <proves>litespec/core/principles#keep-one-authored-source</proves>
    [Fact(DisplayName = "template index omits a tracked template")]
    public void TemplateIndexOmitsTemplate() => Check(
        root =>
        {
            StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample\n");
            StandardsFixture.Write(root, "templates/consumer/unlisted.md", "# Unlisted\n");
            StandardsFixture.Write(root, "templates/consumer/README.md", "# Templates\n\n## Intent\n\nUse these starting points.\n\n| Template | Target | Purpose |\n|:---|:---|:---|\n| `sample.md` | `docs/sample.md` | Present. |\n");
        },
        ["TEMPLATE_INDEX_PATH"]);

    /// <summary>Template index resolves every row.</summary>
    /// <proves>litespec/core/principles#keep-one-authored-source</proves>
    [Fact(DisplayName = "template index resolves every row")]
    public void TemplateIndexResolvesEveryRow() => Check(
        root =>
        {
            StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample\n");
            StandardsFixture.Write(root, "templates/consumer/README.md", "# Templates\n\n## Intent\n\nUse these starting points.\n\n| Template | Target | Purpose |\n|:---|:---|:---|\n| `sample.md` | `docs/sample.md` | Present. |\n");
        },
        [], ["TEMPLATE_INDEX_PATH"]);

    /// <summary>Field name carrying two shapes.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "field name carrying two shapes")]
    public void FieldNameCarryingTwoShapes() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"summary\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"betaPage\":{\"type\":\"object\",\"properties\":{\"summary\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}}}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Date field without a By or On name.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "date field without a By or On name")]
    public void DateFieldWithoutByOrOnName() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"decisionPage\":{\"type\":\"object\",\"properties\":{\"decided\":{\"$ref\":\"#/$defs/isoDate\"}}}," +
                "\"isoDate\":{\"type\":\"string\",\"format\":\"date\"}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Deadline name holding a plain string.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "deadline name holding a plain string")]
    public void DeadlineNameHoldingPlainString() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"goalPage\":{\"type\":\"object\",\"properties\":{\"finishBy\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Record-type noun holding a list under its singular name.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "record-type noun holding a list under its singular name")]
    public void RecordTypeNounHoldingListUnderSingularName() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"requirementEntry\":{\"type\":\"object\",\"properties\":{\"feature\":{\"$ref\":\"#/$defs/featureIdArray\"}}}," +
                "\"featureId\":{\"type\":\"string\"}," +
                "\"featureIdArray\":{\"type\":\"array\",\"items\":{\"$ref\":\"#/$defs/featureId\"}}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Link verb holding prose.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "link verb holding prose")]
    public void LinkVerbHoldingProse() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"cites\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Binds holding prose.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "binds holding prose")]
    public void BindsHoldingProse() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"ruleEntry\":{\"type\":\"object\",\"properties\":{\"binds\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>By name without a participle stem.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "By name without a participle stem")]
    public void ByNameWithoutParticipleStem() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"writerBy\":{\"$ref\":\"#/$defs/writerName\"}}}," +
                "\"writerName\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Closed-set noun holding prose.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "closed-set noun holding prose")]
    public void ClosedSetNounHoldingProse() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"kind\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Prose noun holding a boolean.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "prose noun holding a boolean")]
    public void ProseNounHoldingBoolean() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"summary\":{\"type\":\"boolean\"}}}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>When condition holding a boolean.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "When condition holding a boolean")]
    public void WhenConditionHoldingBoolean() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"finishedWhen\":{\"type\":\"boolean\"}}}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Because reason holding a boolean.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "Because reason holding a boolean")]
    public void BecauseReasonHoldingBoolean() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"appliesBecause\":{\"type\":\"boolean\"}}}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Recorded date without a past verb stem.</summary>
    /// <proves>litespec/core/language#name-each-date-by-what-it-marks</proves>
    [Fact(DisplayName = "recorded date without a past verb stem")]
    public void RecordedDateWithoutPastVerbStem() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"writerOn\":{\"$ref\":\"#/$defs/isoDate\"}}}," +
                "\"isoDate\":{\"type\":\"string\",\"format\":\"date\"}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Lapse date stating what its date does.</summary>
    /// <proves>litespec/core/language#name-each-date-by-what-it-marks</proves>
    [Fact(DisplayName = "lapse date stating what its date does")]
    public void LapseDateStatingWhatItsDateDoes() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"exceptionPage\":{\"type\":\"object\",\"properties\":{\"expiresOn\":{\"$ref\":\"#/$defs/isoDate\"}}}," +
                "\"isoDate\":{\"type\":\"string\",\"format\":\"date\"}}}");
        },
        []);

    /// <summary>Boolean with an is prefix.</summary>
    /// <proves>litespec/core/language#name-a-boolean-as-a-predicate</proves>
    [Fact(DisplayName = "boolean with an is prefix")]
    public void BooleanWithIsPrefix() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"isActive\":{\"type\":\"boolean\"}}}}}");
        },
        ["FIELD_NAME_SHAPE"]);

    /// <summary>Mode objects sharing one shape across pages.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "mode objects sharing one shape across pages")]
    public void ModeObjectsSharingOneShapeAcrossPages() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"freshness\":{\"$ref\":\"#/$defs/freshness\"}}}," +
                "\"betaPage\":{\"type\":\"object\",\"properties\":{\"freshness\":{\"$ref\":\"#/$defs/boundedFreshness\"}}}," +
                "\"freshness\":{\"type\":\"object\",\"required\":[\"mode\"],\"properties\":{\"mode\":{\"enum\":[\"immediate\",\"snapshot\"]}}}," +
                "\"boundedFreshness\":{\"type\":\"object\",\"required\":[\"mode\",\"within\"],\"properties\":{\"mode\":{\"enum\":[\"bounded\"]},\"within\":{\"type\":\"string\"}}}}}");
        },
        []);

    /// <summary>Reaction and job fields sharing their word classes.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "reaction and job fields sharing their word classes")]
    public void ReactionAndJobFieldsSharingWordClasses() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"reactionPage\":{\"type\":\"object\",\"properties\":{\"triggeredBy\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"skippedWhen\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"jobPage\":{\"type\":\"object\",\"properties\":{\"kind\":{\"enum\":[\"scheduled\",\"one-off\",\"manual\"]},\"guard\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"aggregatePage\":{\"type\":\"object\",\"properties\":{\"drivenBy\":{\"type\":\"array\",\"items\":{\"type\":\"object\"}}}}," +
                "\"ruleEntry\":{\"type\":\"object\",\"properties\":{\"binds\":{\"type\":\"array\",\"items\":{\"$ref\":\"#/$defs/aggregateId\"}}}}," +
                "\"scenarioEntry\":{\"type\":\"object\",\"properties\":{\"exercises\":{\"type\":\"array\",\"items\":{\"$ref\":\"#/$defs/ruleId\"}}}}," +
                "\"aggregateId\":{\"type\":\"string\"}," +
                "\"ruleId\":{\"type\":\"string\"}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        []);

    /// <summary>Shipped schemas carry consistent field names.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "shipped schemas carry consistent field names")]
    public void ShippedSchemasCarryConsistentFieldNames()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "schemas/record.schema.json", File.ReadAllText(Path.Combine(Cli.Repository, "schemas", "record.schema.json")));
        StandardsFixture.Write(temp.Path, "schemas/glossary.schema.json", File.ReadAllText(Path.Combine(Cli.Repository, "schemas", "glossary.schema.json")));

        var result = StandardsValidator.Validate(temp.Path);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "FIELD_NAME_SHAPE");
    }

    static readonly string TemplateGoalSchema =
        "{\"$defs\":{" +
        "\"goalPage\":{\"type\":\"object\",\"additionalProperties\":false," +
        "\"required\":[\"type\",\"id\",\"title\",\"kind\"]," +
        "\"properties\":{\"type\":{\"const\":\"goal\"}," +
        "\"id\":{\"$ref\":\"#/$defs/goalId\"}," +
        "\"title\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
        "\"kind\":{\"enum\":[\"a\",\"b\"]}}}," +
        "\"goalId\":{\"type\":\"string\",\"pattern\":\"^goal/[a-z0-9][a-z0-9-]*$\"}," +
        "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}";

    static string TemplateDocument(string json) =>
        "---\n" + StandardsFixture.Yaml(json) + "---\n\n# <Sample Goal>\n\n## Why It Matters\n\nState why this outcome matters.\n";

    /// <summary>Consumer template carrying only placeholders.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying only placeholders")]
    public void ConsumerTemplateWithPlaceholders() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", TemplateGoalSchema);
            StandardsFixture.Write(root, "templates/consumer/goal.yaml", StandardsFixture.Yaml(
                "{\"type\":\"goal\",\"id\":\"goal/<name>\",\"title\":\"<Goal display title>\"," +
                "\"kind\":\"<a|b>\"}"));
        },
        []);

    /// <summary>Consumer template breaking its record shape.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template breaking its record shape")]
    public void ConsumerTemplateBreakingItsShape() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", TemplateGoalSchema);
            StandardsFixture.Write(root, "templates/consumer/goal.yaml", StandardsFixture.Yaml(
                "{\"type\":\"goal\",\"id\":\"goal/sample\",\"kind\":\"a\"}"));
        },
        ["TEMPLATE_SCHEMA"]);

    /// <summary>Consumer template with an invalid record file.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template with an invalid record file")]
    public void ConsumerTemplateWithInvalidRecordFile() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{}}");
            StandardsFixture.Write(root, "templates/consumer/goal.yaml", "{");
        },
        ["TEMPLATE_SCHEMA"]);

    /// <summary>Document template with invalid frontmatter.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "document template with invalid frontmatter")]
    public void DocumentTemplateWithInvalidFrontmatter() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{}}");
            StandardsFixture.Write(root, "templates/consumer/design-contract.md", "---\n{not yaml}\n---\n\n# Sample\n");
        },
        ["TEMPLATE_SCHEMA"]);

    /// <summary>Consumer template with an unknown record type.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template with an unknown record type")]
    public void ConsumerTemplateWithUnknownType() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{\"goalPage\":{\"type\":\"object\"}}}");
            StandardsFixture.Write(root, "templates/consumer/portal.yaml", StandardsFixture.Yaml("{\"type\":\"portal\"}"));
        },
        ["TEMPLATE_SCHEMA"]);

    /// <summary>Consumer template dropping a removable placeholder.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template dropping a removable placeholder")]
    public void ConsumerTemplateDroppingRemovablePlaceholder() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"screenPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"id\"]," +
                "\"properties\":{\"type\":{\"const\":\"screen\"}," +
                "\"id\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
                "\"address\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
                "\"host\":{\"$ref\":\"#/$defs/nonEmptyString\"}}," +
                "\"oneOf\":[{\"required\":[\"address\"],\"not\":{\"required\":[\"host\"]}}," +
                "{\"required\":[\"host\"],\"not\":{\"required\":[\"address\"]}}]}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
            StandardsFixture.Write(root, "templates/consumer/screen.yaml", StandardsFixture.Yaml(
                "{\"type\":\"screen\",\"id\":\"screen/sample\"," +
                "\"address\":\"<Route template, or remove this line and keep host>\"," +
                "\"host\":\"screen/sample\"}"));
        },
        []);

    /// <summary>Consumer template reading an enum under its shape.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template reading an enum under its shape")]
    public void ConsumerTemplateReadingEnumUnderItsShape() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"eventPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"kind\"]," +
                "\"properties\":{\"type\":{\"const\":\"event\"}," +
                "\"kind\":{\"enum\":[\"command\",\"query\"]}," +
                "\"output\":{\"type\":\"object\"}}," +
                "\"allOf\":[" +
                "{\"if\":{\"required\":[\"kind\"],\"properties\":{\"kind\":{\"const\":\"query\"}}}," +
                "\"then\":{\"required\":[\"output\"]}}," +
                "{\"if\":{\"required\":[\"kind\"],\"properties\":{\"kind\":{\"const\":\"command\"}}}," +
                "\"then\":{\"not\":{\"required\":[\"output\"]}}}]}}}");
            StandardsFixture.Write(root, "templates/consumer/event.yaml", StandardsFixture.Yaml(
                "{\"type\":\"event\",\"kind\":\"<command|query>\",\"output\":{\"include\":{}}}"));
        },
        []);

    /// <summary>Consumer template carrying a placeholder for a non-string field.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying a placeholder for a non-string field")]
    public void ConsumerTemplateWithNonStringPlaceholder() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"jobPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"batchSize\"]," +
                "\"properties\":{\"type\":{\"const\":\"job\"}," +
                "\"batchSize\":{\"type\":\"integer\"}," +
                "\"retry\":{\"anyOf\":[{\"const\":\"none\"},{\"$ref\":\"#/$defs/retryPolicy\"}]}}}," +
                "\"retryPolicy\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"maxAttempts\"]," +
                "\"properties\":{\"maxAttempts\":{\"type\":\"integer\",\"minimum\":1}}}}}");
            StandardsFixture.Write(root, "templates/consumer/job.yaml", StandardsFixture.Yaml(
                "{\"type\":\"job\",\"batchSize\":\"<number>\"," +
                "\"retry\":{\"maxAttempts\":\"<number>\"}}"));
        },
        []);

    /// <summary>Consumer template carrying a mode object.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying a mode object")]
    public void ConsumerTemplateCarryingModeObject() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"useCasePage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"freshness\"]," +
                "\"properties\":{\"type\":{\"const\":\"use-case\"}," +
                "\"freshness\":{\"$ref\":\"#/$defs/freshness\"}}}," +
                "\"freshness\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"mode\"]," +
                "\"properties\":{\"mode\":{\"enum\":[\"immediate\",\"bounded\"]}," +
                "\"within\":{\"$ref\":\"#/$defs/isoDuration\"}}," +
                "\"allOf\":[{\"if\":{\"required\":[\"mode\"],\"properties\":{\"mode\":{\"const\":\"bounded\"}}}," +
                "\"then\":{\"required\":[\"within\"]}}]}," +
                "\"isoDuration\":{\"type\":\"string\",\"pattern\":\"^P.+\"}}}");
            StandardsFixture.Write(root, "templates/consumer/use-case.yaml", StandardsFixture.Yaml(
                "{\"type\":\"use-case\"," +
                "\"freshness\":{\"mode\":\"bounded\",\"within\":\"<ISO 8601 duration>\"}}"));
        },
        []);

    /// <summary>Consumer template carrying a reaction identifier.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying a reaction identifier")]
    public void ConsumerTemplateWithReactionId() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"reactionPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"id\",\"title\",\"triggeredBy\"]," +
                "\"properties\":{\"type\":{\"const\":\"reaction\"}," +
                "\"id\":{\"$ref\":\"#/$defs/reactionId\"}," +
                "\"title\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
                "\"triggeredBy\":{\"$ref\":\"#/$defs/reactionTriggeredBy\"}}}," +
                "\"reactionId\":{\"type\":\"string\",\"pattern\":\"^reaction/[a-z0-9][a-z0-9-]*\\\\.[a-z0-9][a-z0-9-]*$\"}," +
                "\"reactionTriggeredBy\":{\"type\":\"object\"}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
            StandardsFixture.Write(root, "templates/consumer/reaction.yaml", StandardsFixture.Yaml(
                "{\"type\":\"reaction\",\"id\":\"reaction/<module>.<name>\",\"title\":\"<Reaction display title>\"," +
                "\"triggeredBy\":{}}"));
        },
        []);

    /// <summary>Consumer template carrying a job identifier.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying a job identifier")]
    public void ConsumerTemplateWithJobId() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"jobPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"id\",\"title\",\"kind\"]," +
                "\"properties\":{\"type\":{\"const\":\"job\"}," +
                "\"id\":{\"$ref\":\"#/$defs/jobId\"}," +
                "\"title\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
                "\"kind\":{\"enum\":[\"scheduled\",\"one-off\",\"manual\"]}}}," +
                "\"jobId\":{\"type\":\"string\",\"pattern\":\"^job/[a-z0-9][a-z0-9-]*\\\\.[a-z0-9][a-z0-9-]*$\"}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
            StandardsFixture.Write(root, "templates/consumer/job.yaml", StandardsFixture.Yaml(
                "{\"type\":\"job\",\"id\":\"job/<module>.<name>\",\"title\":\"<Job display title>\"," +
                "\"kind\":\"<scheduled|one-off|manual>\"}"));
        },
        []);

    /// <summary>Consumer template carrying an exception identifier.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying an exception identifier")]
    public void ConsumerTemplateWithExceptionId() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"exceptionPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"id\",\"title\",\"kind\",\"covers\",\"reason\",\"expiresOn\"]," +
                "\"properties\":{\"type\":{\"const\":\"exception\"}," +
                "\"id\":{\"$ref\":\"#/$defs/exceptionId\"}," +
                "\"title\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
                "\"kind\":{\"enum\":[\"departure\",\"gap\"]}," +
                "\"covers\":{\"$ref\":\"#/$defs/findingCodeArray\"}," +
                "\"reason\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
                "\"expiresOn\":{\"$ref\":\"#/$defs/isoDate\"}}}," +
                "\"exceptionId\":{\"type\":\"string\",\"pattern\":\"^exception/[a-z0-9][a-z0-9-]*$\"}," +
                "\"findingCodeArray\":{\"type\":\"array\",\"minItems\":1,\"uniqueItems\":true,\"items\":{\"$ref\":\"#/$defs/findingCode\"}}," +
                "\"findingCode\":{\"type\":\"string\",\"pattern\":\"^[a-z0-9]+(-[a-z0-9]+)*$\"}," +
                "\"isoDate\":{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}$\"}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
            StandardsFixture.Write(root, "templates/consumer/exception.yaml", StandardsFixture.Yaml(
                "{\"type\":\"exception\",\"id\":\"exception/<name>\",\"title\":\"<Exception display title>\"," +
                "\"kind\":\"gap\",\"covers\":[\"<finding-code>\"],\"reason\":\"<reason>\",\"expiresOn\":\"<YYYY-MM-DD>\"}"));
        },
        []);

    /// <summary>Consumer template carrying no record type.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying no record type")]
    public void ConsumerTemplateWithoutRecordType() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{\"goalPage\":{\"type\":\"object\"}}}");
            StandardsFixture.Write(root, "templates/consumer/design-contract.md", TemplateDocument(
                "{\"kind\":\"design-contract\",\"schemaVersion\":1," +
                "\"package\":\"@sample/ui\",\"shell\":\"sample\",\"patterns\":[\"list-page\"]}"));
        },
        []);

    /// <summary>Missing required property names its pointer.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "missing required property names its pointer")]
    public void MissingRequiredPropertyNamesItsPointer()
    {
        var error = Assert.Single(ValidateJson(
            "{\"type\":\"object\",\"required\":[\"title\"],\"properties\":{\"title\":{\"type\":\"string\"}}}",
            "{}"));

        Assert.Equal("value.json: missing required property 'title'", error);
    }

    /// <summary>Unknown property names its key.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unknown property names its key")]
    public void UnknownPropertyNamesItsKey()
    {
        var error = Assert.Single(ValidateJson(
            "{\"type\":\"object\",\"properties\":{\"a\":{}},\"additionalProperties\":false}",
            "{\"a\":1,\"b\":2}"));

        Assert.Equal("value.json: unknown property 'b'", error);
    }

    /// <summary>Minimum items keeps its count.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "minimum items keeps its count")]
    public void MinimumItemsKeepsItsCount()
    {
        var error = Assert.Single(ValidateJson("{\"type\":\"array\",\"minItems\":1}", "[]"));

        Assert.Equal("value.json: array has fewer than 1 items", error);
    }

    /// <summary>OneOf counts its shapes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "oneOf counts its shapes")]
    public void OneOfCountsItsShapes()
    {
        var error = Assert.Single(ValidateJson("{\"oneOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "1.5"));

        Assert.Equal("value.json: value matches 0 of 2 allowed shapes, expected exactly 1 (shape 1: expected string; shape 2: expected integer)", error);
    }

    /// <summary>AnyOf names its shapes.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "anyOf names its shapes")]
    public void AnyOfNamesItsShapes()
    {
        var error = Assert.Single(ValidateJson("{\"anyOf\":[{\"type\":\"string\"},{\"type\":\"boolean\"}]}", "1"));

        Assert.Equal("value.json: value matches none of the 2 allowed shapes (shape 1: expected string; shape 2: expected boolean)", error);
    }

    /// <summary>Problems prefix the gate before the validation.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "problems prefix the gate before the validation")]
    public void ProblemsPrefixGateBeforeValidation()
    {
        var schema = JsonNode.Parse("{\"type\":\"object\",\"mystery\":true}")!;
        var problems = JsonSchema.Problems("schema.json", JsonNode.Parse("\"x\""), "value.json", _ => schema);

        Assert.Equal(2, problems.Count);
        Assert.Equal("value.json: unsupported JSON Schema keyword 'mystery' at '#'", problems[0]);
        Assert.Equal("value.json: expected object", problems[1]);
    }

    /// <summary>Subschema resolves references against the root.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "subschema resolves references against the root")]
    public void SubschemaResolvesReferencesAgainstRoot()
    {
        var root = JsonNode.Parse("{\"$defs\":{\"page\":{\"type\":\"object\",\"properties\":{\"id\":{\"$ref\":\"#/$defs/id\"}}},\"id\":{\"type\":\"string\",\"minLength\":2}}}")!.AsObject();
        var page = root["$defs"]!.AsObject()["page"]!;
        var errors = new List<string>();
        JsonSchema.Validate(JsonNode.Parse("{\"id\":\"x\"}"), page, root, "value.json", errors);

        var error = Assert.Single(errors);

        Assert.Equal("value.json/id: string is shorter than 2", error);
    }

    /// <summary>Const names its value.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "const names its value")]
    public void ConstNamesItsValue()
    {
        var error = Assert.Single(ValidateJson("{\"const\":\"a\"}", "\"b\""));

        Assert.Equal("value.json: expected constant \"a\"", error);
    }

    /// <summary>Pattern names its expression.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "pattern names its expression")]
    public void PatternNamesItsExpression()
    {
        var error = Assert.Single(ValidateJson("{\"pattern\":\"^a\"}", "\"b\""));

        Assert.Equal("value.json: string does not match ^a", error);
    }

    /// <summary>A passing oneOf reports nothing from the shape it rejected, so a valid alternative never reads as missing.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a passing oneOf reports nothing from its rejected shapes")]
    public void PassingOneOfReportsNothingFromItsRejectedShapes()
    {
        var errors = ValidateJson(
            "{\"type\":\"object\",\"additionalProperties\":false,\"properties\":{\"url\":{},\"path\":{}},\"oneOf\":[{\"required\":[\"url\"]},{\"required\":[\"path\"]}]}",
            "{\"path\":\"notes.md\",\"extra\":1}");

        Assert.Equal(["value.json: unknown property 'extra'"], errors);
    }

    /// <summary>A false subschema outside additionalProperties reports the value as not allowed.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a false subschema reports the value as not allowed")]
    public void FalseSubschemaReportsTheValue()
    {
        var error = Assert.Single(ValidateJson("{\"properties\":{\"legacy\":false}}", "{\"legacy\":1}"));

        Assert.Equal("value.json/legacy: value is not allowed", error);
    }

    /// <summary>A reference is a URI fragment, so a percent-encoded name resolves.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "a percent-encoded reference resolves")]
    public void PercentEncodedReferenceResolves()
    {
        var error = Assert.Single(ValidateJson("{\"$defs\":{\"a%b\":{\"type\":\"string\"}},\"$ref\":\"#/$defs/a%25b\"}", "1"));

        Assert.Equal("value.json: expected string", error);
    }

    /// <summary>A property name that breaks propertyNames reports at that property.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "property names report at the property")]
    public void PropertyNamesReportAtTheProperty()
    {
        var error = Assert.Single(ValidateJson("{\"propertyNames\":{\"pattern\":\"^[a-z]+$\"}}", "{\"Bad\":1}"));

        Assert.Equal("value.json/Bad: string does not match ^[a-z]+$", error);
    }

    /// <summary>Numbers compare by value, so 1.0 is the integer 1 and equals the constant 1.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "numbers compare by value")]
    public void NumbersCompareByValue()
    {
        Assert.Empty(ValidateJson("{\"type\":\"integer\",\"const\":1,\"enum\":[1],\"minimum\":1}", "1.0"));
        Assert.Equal(["value.json: array items are not unique"], ValidateJson("{\"uniqueItems\":true}", "[1,1.0]"));
    }

    /// <summary>The gate flags the keywords the evaluator leaves out, so a schema that relies on one fails instead of passing unread.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "gate flags the keywords the evaluator leaves out")]
    public void GateFlagsTheKeywordsTheEvaluatorLeavesOut()
    {
        var found = new List<string>();
        JsonSchema.UnsupportedKeywords(
            JsonNode.Parse("{\"unevaluatedProperties\":false,\"$dynamicRef\":\"#a\",\"$anchor\":\"a\",\"contentSchema\":{}}"),
            "s.json",
            "#",
            (_, _, _, message) => found.Add(message));

        Assert.Equal(
            [
                "unsupported JSON Schema keyword 'unevaluatedProperties' at '#'",
                "unsupported JSON Schema keyword '$dynamicRef' at '#'",
                "unsupported JSON Schema keyword '$anchor' at '#'",
                "unsupported JSON Schema keyword 'contentSchema' at '#'",
            ],
            found);
    }

    /// <summary>Gate flags an unknown keyword.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "gate flags an unknown keyword")]
    public void GateFlagsUnknownKeyword()
    {
        var found = new List<string>();
        JsonSchema.UnsupportedKeywords(
            JsonNode.Parse("{\"type\":\"object\",\"example\":\"v\"}"),
            "s.json",
            "#",
            (relative, line, code, message) => found.Add($"{relative}:{line}:{code}:{message}"));

        var single = Assert.Single(found);

        Assert.Equal("s.json:1:SCHEMA_UNSUPPORTED_KEYWORD:unsupported JSON Schema keyword 'example' at '#'", single);
    }

    /// <summary>Gate passes implemented keywords.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "gate passes implemented keywords")]
    public void GatePassesImplementedKeywords()
    {
        var found = new List<string>();
        JsonSchema.UnsupportedKeywords(
            JsonNode.Parse("{\"type\":\"object\",\"additionalProperties\":false,\"dependentSchemas\":{\"a\":{\"type\":\"string\"}},\"properties\":{\"a\":{\"contentEncoding\":\"base64\",\"contentMediaType\":\"text/plain\",\"readOnly\":true},\"b\":{\"contains\":{},\"minContains\":1,\"maxContains\":2}}}"),
            "s.json",
            "#",
            (relative, line, code, message) => found.Add($"{relative}:{line}:{code}:{message}"));

        Assert.Empty(found);
    }

    /// <summary>Gate reaches dependent schemas.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "gate reaches dependent schemas")]
    public void GateReachesDependentSchemas()
    {
        var found = new List<string>();
        JsonSchema.UnsupportedKeywords(
            JsonNode.Parse("{\"dependentSchemas\":{\"a\":{\"example\":1}}}"),
            "s.json",
            "#",
            (relative, line, code, message) => found.Add($"{relative}:{line}:{code}:{message}"));

        var single = Assert.Single(found);

        Assert.Equal("s.json:1:SCHEMA_UNSUPPORTED_KEYWORD:unsupported JSON Schema keyword 'example' at '#/dependentSchemas/a'", single);
    }

    /// <summary>Validates one JSON value against one JSON schema.</summary>
    /// <param name="schemaJson">The schema text.</param>
    /// <param name="valueJson">The value text.</param>
    /// <returns>The schema errors.</returns>
    static List<string> ValidateJson(string schemaJson, string valueJson)
    {
        var schema = JsonNode.Parse(schemaJson)!;
        var errors = new List<string>();
        JsonSchema.Validate(JsonNode.Parse(valueJson), schema, schema, "value.json", errors);
        return errors;
    }
}
