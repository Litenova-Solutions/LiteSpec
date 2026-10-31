using LiteSpec.Authoring;
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
        "{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"id\",\"criterion\",\"reviewBy\"]," +
        "\"properties\":{\"id\":{\"type\":\"string\"},\"criterion\":{\"type\":\"string\"},\"reviewBy\":{\"type\":\"string\"}}}" +
        "]}}}}";

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema id naming another home or release")]
    public void SchemaIdNamingAnotherRelease() => Check(
        root =>
        {
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"2.0.0\"}");
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$id\":\"https://example.com/schemas/record.schema.json\",\"type\":\"object\"}");
        },
        ["SCHEMA_ID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema id naming the published home and release")]
    public void SchemaIdNamingThePublishedRelease()
    {
        using var temp = Cli.Temp("standards");
        StandardsFixture.Base(temp.Path);
        StandardsFixture.Write(temp.Path, "litespec.manifest.json", "{\"version\":\"2.0.0\"}");
        StandardsFixture.Write(temp.Path, "schemas/record.schema.json", "{\"$id\":\"https://litespec.dev/2.0.0/schemas/record.schema.json\",\"type\":\"object\"}");

        var result = StandardsValidator.Validate(temp.Path);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "SCHEMA_ID");
    }

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "invalid schema consumer")]
    public void InvalidSchemaConsumer() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"required\":[\"version\"],\"properties\":{\"version\":{\"const\":\"1.0.0\"}},\"additionalProperties\":false}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"1.17.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unsupported schema keyword")]
    public void UnsupportedSchemaKeyword() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"unevaluatedProperties\":false}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_UNSUPPORTED_KEYWORD"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unsupported schema keyword inside a branch")]
    public void UnsupportedSchemaKeywordInBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"anyOf\":[{\"type\":\"object\",\"properties\":{\"version\":{\"contentEncoding\":\"base64\"}}}]}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_UNSUPPORTED_KEYWORD"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "unsupported schema keyword inside propertyNames")]
    public void UnsupportedSchemaKeywordInPropertyNames() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"propertyNames\":{\"type\":\"string\",\"contentMediaType\":\"text/plain\"}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_UNSUPPORTED_KEYWORD"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema upper bound")]
    public void SchemaUpperBound() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"properties\":{\"version\":{\"type\":\"string\",\"maxLength\":3}}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"1.16.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema anyOf branch")]
    public void SchemaAnyOfBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"properties\":{\"version\":{\"anyOf\":[{\"type\":\"integer\"},{\"type\":\"boolean\"}]}}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"1.16.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema dependent property")]
    public void SchemaDependentProperty() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"dependentRequired\":{\"version\":[\"name\"]}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"1.16.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema property name shape")]
    public void SchemaPropertyNameShape() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"propertyNames\":{\"pattern\":\"^[a-z]+$\"}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"Version\":\"1.16.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema pattern properties")]
    public void SchemaPatternProperties() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"patternProperties\":{\"^v\":{\"type\":\"integer\"}},\"additionalProperties\":false}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"version\":\"1.16.0\"}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema array bounds")]
    public void SchemaArrayBounds() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{\"type\":\"object\",\"properties\":{\"areas\":{\"type\":\"array\",\"maxItems\":1}}}");
            StandardsFixture.Write(root, "litespec.manifest.json", "{\"areas\":[\"CORE\",\"EXT\"]}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "schema JSON")]
    public void SchemaJson() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-manifest.schema.json", "{");
            StandardsFixture.Write(root, "litespec.manifest.json", "{}");
        },
        ["SCHEMA_JSON"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "project template carrying a removed overrides entry")]
    public void ProjectTemplateWithRemovedOverrides() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-project.schema.json", StandardsFixture.ShippedProjectSchema());
            StandardsFixture.Write(root, "templates/consumer/litespec.project.json", "{\"overrides\":[{\"provisionId\":\"litespec/core/topic#keep-the-topic-bounded\"}]}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "value matches no oneOf branch")]
    public void ValueMatchesNoOneOfBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-project.schema.json", OneOfSchema);
            StandardsFixture.Write(root, "templates/consumer/litespec.project.json", "{\"selectedExtensions\":[{\"id\":\"outbox\",\"criterion\":\"Durable delivery.\"}]}");
        },
        ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "value matches exactly one oneOf branch")]
    public void ValueMatchesOneOneOfBranch() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/litespec-project.schema.json", OneOfSchema);
            StandardsFixture.Write(root, "templates/consumer/litespec.project.json", "{\"selectedExtensions\":[\"outbox\",{\"id\":\"jobs\",\"criterion\":\"Holds expire on a timer.\",\"reviewBy\":\"2099-01-01\"}]}");
        },
        [], ["SCHEMA_INVALID"]);

    /// <proves>litespec/core/principles#keep-one-authored-source</proves>
    [Fact(DisplayName = "template index names a missing file")]
    public void TemplateIndexNamesMissingFile() => Check(
        root =>
        {
            StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample\n");
            StandardsFixture.Write(root, "templates/consumer/README.md", "# Templates\n\n## Intent\n\nUse these starting points.\n\n| Template | Target | Purpose |\n|:---|:---|:---|\n| `sample.md` | `docs/sample.md` | Present. |\n| `absent.md` | `docs/absent.md` | Renamed away. |\n");
        },
        ["TEMPLATE_INDEX_PATH"]);

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

    /// <proves>litespec/core/principles#keep-one-authored-source</proves>
    [Fact(DisplayName = "template index resolves every row")]
    public void TemplateIndexResolvesEveryRow() => Check(
        root =>
        {
            StandardsFixture.Write(root, "templates/consumer/sample.md", "# Sample\n");
            StandardsFixture.Write(root, "templates/consumer/README.md", "# Templates\n\n## Intent\n\nUse these starting points.\n\n| Template | Target | Purpose |\n|:---|:---|:---|\n| `sample.md` | `docs/sample.md` | Present. |\n");
        },
        [], ["TEMPLATE_INDEX_PATH"]);

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

    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "deadline name holding a plain string")]
    public void DeadlineNameHoldingPlainString() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"goalPage\":{\"type\":\"object\",\"properties\":{\"reviewBy\":{\"$ref\":\"#/$defs/nonEmptyString\"}}}," +
                "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

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

    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact(DisplayName = "By name without a participle stem")]
    public void ByNameWithoutParticipleStem() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"ownerBy\":{\"$ref\":\"#/$defs/ownerName\"}}}," +
                "\"ownerName\":{\"type\":\"string\",\"minLength\":1}}}");
        },
        ["FIELD_NAME_SHAPE"]);

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

    /// <proves>litespec/core/language#name-each-date-by-what-it-marks</proves>
    [Fact(DisplayName = "recorded date without a past verb stem")]
    public void RecordedDateWithoutPastVerbStem() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"alphaPage\":{\"type\":\"object\",\"properties\":{\"ownerOn\":{\"$ref\":\"#/$defs/isoDate\"}}}," +
                "\"isoDate\":{\"type\":\"string\",\"format\":\"date\"}}}");
        },
        ["FIELD_NAME_SHAPE"]);

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
        "\"required\":[\"type\",\"id\",\"title\",\"owner\",\"status\",\"decideBy\",\"kind\"]," +
        "\"properties\":{\"type\":{\"const\":\"goal\"}," +
        "\"id\":{\"$ref\":\"#/$defs/goalId\"}," +
        "\"title\":{\"$ref\":\"#/$defs/nonEmptyString\"}," +
        "\"owner\":{\"$ref\":\"#/$defs/ownerName\"}," +
        "\"status\":{\"$ref\":\"#/$defs/intentStatus\"}," +
        "\"decideBy\":{\"$ref\":\"#/$defs/isoDate\"}," +
        "\"kind\":{\"enum\":[\"a\",\"b\"]}}}," +
        "\"goalId\":{\"type\":\"string\",\"pattern\":\"^goal/[a-z0-9][a-z0-9-]*$\"}," +
        "\"nonEmptyString\":{\"type\":\"string\",\"minLength\":1}," +
        "\"ownerName\":{\"type\":\"string\",\"minLength\":1}," +
        "\"intentStatus\":{\"type\":\"string\",\"enum\":[\"draft\",\"approved\"]}," +
        "\"isoDate\":{\"type\":\"string\",\"pattern\":\"^\\\\d{4}-\\\\d{2}-\\\\d{2}$\"}}}";

    static string TemplatePage(string frontmatter) =>
        "---\n" + frontmatter + "\n---\n\n# <Sample Goal>\n\n## Why It Matters\n\nState why this outcome matters.\n";

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying only placeholders")]
    public void ConsumerTemplateWithPlaceholders() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", TemplateGoalSchema);
            StandardsFixture.Write(root, "templates/consumer/goal.md", TemplatePage(
                "{\"type\":\"goal\",\"id\":\"goal/<name>\",\"title\":\"<Goal display title>\"," +
                "\"owner\":\"<owner>\",\"status\":\"draft\",\"decideBy\":\"<YYYY-MM-DD>\",\"kind\":\"<a|b>\"}"));
        },
        []);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template breaking its record shape")]
    public void ConsumerTemplateBreakingItsShape() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", TemplateGoalSchema);
            StandardsFixture.Write(root, "templates/consumer/goal.md", TemplatePage(
                "{\"type\":\"goal\",\"id\":\"goal/sample\",\"owner\":\"Sam\"," +
                "\"status\":\"draft\",\"decideBy\":\"2026-11-30\",\"kind\":\"a\"}"));
        },
        ["TEMPLATE_SCHEMA"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template with invalid frontmatter")]
    public void ConsumerTemplateWithInvalidFrontmatter() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{}}");
            StandardsFixture.Write(root, "templates/consumer/goal.md", "---\n{not json}\n---\n\n# Sample\n");
        },
        ["TEMPLATE_SCHEMA"]);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template with an unknown record type")]
    public void ConsumerTemplateWithUnknownType() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{\"goalPage\":{\"type\":\"object\"}}}");
            StandardsFixture.Write(root, "templates/consumer/portal.md", TemplatePage("{\"type\":\"portal\"}"));
        },
        ["TEMPLATE_SCHEMA"]);

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
            StandardsFixture.Write(root, "templates/consumer/screen.md", TemplatePage(
                "{\"type\":\"screen\",\"id\":\"screen/sample\"," +
                "\"address\":\"<Route template, or remove this line and keep host>\"," +
                "\"host\":\"screen/sample\"}"));
        },
        []);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template reading an enum under its shape")]
    public void ConsumerTemplateReadingEnumUnderItsShape() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json",
                "{\"$defs\":{" +
                "\"queryPage\":{\"type\":\"object\",\"additionalProperties\":false," +
                "\"required\":[\"type\",\"kind\"]," +
                "\"properties\":{\"type\":{\"const\":\"query\"}," +
                "\"kind\":{\"enum\":[\"command\",\"query\"]}," +
                "\"output\":{\"type\":\"object\"}}," +
                "\"allOf\":[" +
                "{\"if\":{\"required\":[\"kind\"],\"properties\":{\"kind\":{\"const\":\"query\"}}}," +
                "\"then\":{\"required\":[\"output\"]}}," +
                "{\"if\":{\"required\":[\"kind\"],\"properties\":{\"kind\":{\"const\":\"command\"}}}," +
                "\"then\":{\"not\":{\"required\":[\"output\"]}}}]}}}");
            StandardsFixture.Write(root, "templates/consumer/query.md", TemplatePage(
                "{\"type\":\"query\",\"kind\":\"<command|query>\",\"output\":{\"include\":{}}}"));
        },
        []);

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
            StandardsFixture.Write(root, "templates/consumer/job.md", TemplatePage(
                "{\"type\":\"job\",\"batchSize\":\"<number>\"," +
                "\"retry\":{\"maxAttempts\":\"<number>\"}}"));
        },
        []);

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
            StandardsFixture.Write(root, "templates/consumer/use-case.md", TemplatePage(
                "{\"type\":\"use-case\"," +
                "\"freshness\":{\"mode\":\"bounded\",\"within\":\"<ISO 8601 duration>\"}}"));
        },
        []);

    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact(DisplayName = "consumer template carrying no record type")]
    public void ConsumerTemplateWithoutRecordType() => Check(
        root =>
        {
            StandardsFixture.Write(root, "schemas/record.schema.json", "{\"$defs\":{\"goalPage\":{\"type\":\"object\"}}}");
            StandardsFixture.Write(root, "templates/consumer/design-contract.md", TemplatePage(
                "{\"kind\":\"design-contract\",\"schemaVersion\":1," +
                "\"package\":\"@sample/ui\",\"shell\":\"sample\",\"patterns\":[\"list-page\"]}"));
        },
        []);
}
