using System.Text.Json;
using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the validators generator writes one validator per command and query carrying constrained inputs.</summary>
public sealed class GenerateValidatorsTests
{
    /// <summary>A command validator renders its golden file with one rule per constraint.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void CommandValidatorMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path);
        WriteRenewLoan(temp.Path);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);

        var file = Assert.Single(
            plan.Files,
            candidate => candidate.AbsolutePath.EndsWith(
                Path.Combine("Application", "Loans", "RenewLoan", "RenewLoanCommandValidator.generated.cs"),
                StringComparison.Ordinal));

        Assert.DoesNotContain("Adjustment", file.Content, StringComparison.Ordinal);
        GenerateGoldens.AssertEqual("validators-dotnet-loans-renew-loan.txt", file.Content);
    }

    /// <summary>A query validator renders its golden file, judging the query parameter with optional guards.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void QueryValidatorMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path);
        WriteSearchLoans(temp.Path);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);

        var file = Assert.Single(
            plan.Files,
            candidate => candidate.AbsolutePath.EndsWith(
                Path.Combine("Application", "Loans", "SearchLoans", "SearchLoansQueryValidator.generated.cs"),
                StringComparison.Ordinal));

        GenerateGoldens.AssertEqual("validators-dotnet-loans-search-loans.txt", file.Content);
    }

    /// <summary>A use case with no constrained input renders no validator file.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UseCaseWithoutConstrainedInputsRendersNoFile()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path);
        WriteUseCase(
            temp.Path,
            "loans",
            "read-catalog",
            "Read catalog",
            "query",
            """
            - name: shelf
              meaning: The shelf the shown loans sit on.
              type: text
              required: false
              origin: caller
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.DoesNotContain(
            plan.Files,
            file => file.AbsolutePath.Contains("ReadCatalogQueryValidator", StringComparison.Ordinal));
    }

    /// <summary>A validators target for a TypeScript stack reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ValidatorsTargetNeedsDotnetStack()
    {
        using var temp = ModelFixture.Prepare("small");
        File.AppendAllText(
            Path.Combine(temp.Path, ".litespec", "project.yaml"),
            "\n"
            + """
              generate:
              - kind: validators
                stack: svelte
                path: apps/web/src/generated
              """
            + "\n");

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains(".NET", StringComparison.Ordinal));
    }

    /// <summary>A validators target without a namespace reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ValidatorsTargetWithoutNamespaceReports()
    {
        using var temp = ModelFixture.Prepare("small");
        File.AppendAllText(
            Path.Combine(temp.Path, ".litespec", "project.yaml"),
            "\n"
            + """
              generate:
              - kind: validators
                stack: dotnet
                path: apps/api/src/Modules/Library.{Module}
              """
            + "\n");

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("namespace", StringComparison.Ordinal));
    }

    /// <summary>Required fields judge presence by shape: text for emptiness, actor ids for an empty Guid, aggregate ids for default, choice and list for null, and value types never.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PresenceRulesFollowTheFieldShape()
    {
        AssertRule(Input("""{ "name": "note", "type": "text", "required": true }"""), "Note", "string.IsNullOrEmpty(command.Note)");
        AssertRule(
            Input("""{ "name": "readerId", "type": "id", "identifies": "actor/reader", "required": true }"""),
            "ReaderId",
            "command.ReaderId == Guid.Empty");
        AssertRule(
            Input("""{ "name": "loanId", "type": "id", "identifies": "aggregate/lending.loan", "required": true }"""),
            "LoanId",
            "command.LoanId == default");
        AssertRule(Input("""{ "name": "kind", "type": "choice", "required": true }"""), "Kind", "command.Kind is null");
        AssertRule(Input("""{ "name": "tags", "type": "list", "required": true }"""), "Tags", "command.Tags is null");
        AssertNoRules(Input("""{ "name": "days", "type": "integer", "required": true }"""), "Days");
        AssertNoRules(Input("""{ "name": "amount", "type": "decimal", "required": true }"""), "Amount");
        AssertNoRules(Input("""{ "name": "active", "type": "boolean", "required": true }"""), "Active");
        AssertNoRules(Input("""{ "name": "due", "type": "date", "required": true }"""), "Due");
    }

    /// <summary>A field holding many values renders no rules: per-item checks stay hand-written.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ManyValuedFieldRendersNoRules()
    {
        AssertNoRules(Input("""{ "name": "tags", "type": "text", "many": true, "maxLength": 20, "required": true }"""), "Tags");
    }

    /// <summary>A money bound compares the amount of the shared money object, guarding null when optional.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyBoundsCompareTheAmount()
    {
        var rules = new List<ValidatorsRenderer.ValidationRule>();

        ValidatorsRenderer.AddRulesFor(rules, "Deposit", "command", false, Input("""{ "name": "deposit", "type": "money", "bounds": { "min": 0 }, "required": true }"""));

        var rule = Assert.Single(rules);

        Assert.Equal("Deposit", rule.Member);
        Assert.Equal("too-small", rule.Code);
        Assert.Equal("command.Deposit.Amount < 0m", rule.Condition);

        var guardedRules = new List<ValidatorsRenderer.ValidationRule>();

        ValidatorsRenderer.AddRulesFor(guardedRules, "Deposit", "command", false, Input("""{ "name": "deposit", "type": "money", "bounds": { "min": 0 }, "required": false }"""));

        var guarded = Assert.Single(guardedRules);

        Assert.Equal("command.Deposit is not null && command.Deposit.Amount < 0m", guarded.Condition);
    }

    /// <summary>A required map renders a presence rule judging null.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapPresenceRendersNullCheck()
    {
        AssertRule(
            Input("""{ "name": "versions", "type": "map", "keyType": "text", "valueType": "integer", "required": true }"""),
            "Versions",
            "command.Versions is null");
    }

    /// <summary>Map bounds judge each value through Linq, guarding null when optional.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapBoundsJudgeEachValue()
    {
        var rules = new List<ValidatorsRenderer.ValidationRule>();

        var (_, _, usesLinq) = ValidatorsRenderer.AddRulesFor(
            rules, "Versions", "command", false,
            Input("""{ "name": "versions", "type": "map", "keyType": "text", "valueType": "integer", "bounds": { "min": 1, "max": 5 }, "required": true }"""));

        Assert.True(usesLinq);
        Assert.Equal(3, rules.Count);
        Assert.Equal("command.Versions is null", rules[0].Condition);
        Assert.Equal("too-small", rules[1].Code);
        Assert.Equal("command.Versions.Values.Any(value => value < 1)", rules[1].Condition);
        Assert.Equal("The versions must each be at least 1.", rules[1].Message);
        Assert.Equal("too-large", rules[2].Code);
        Assert.Equal("command.Versions.Values.Any(value => value > 5)", rules[2].Condition);

        var guardedRules = new List<ValidatorsRenderer.ValidationRule>();

        ValidatorsRenderer.AddRulesFor(
            guardedRules, "Versions", "command", false,
            Input("""{ "name": "versions", "type": "map", "keyType": "text", "valueType": "integer", "bounds": { "max": 5 }, "required": false }"""));

        var guarded = Assert.Single(guardedRules);

        Assert.Equal("command.Versions is not null && command.Versions.Values.Any(value => value > 5)", guarded.Condition);
    }

    /// <summary>Map text constraints judge each value, matching the pattern with a regular expression.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapTextConstraintsJudgeEachValue()
    {
        var rules = new List<ValidatorsRenderer.ValidationRule>();

        var (_, usesRegex, usesLinq) = ValidatorsRenderer.AddRulesFor(
            rules, "Labels", "command", false,
            Input("""{ "name": "labels", "type": "map", "keyType": "text", "valueType": "text", "maxLength": 20, "matches": "^[a-z]+$", "required": true }"""));

        Assert.True(usesLinq);
        Assert.True(usesRegex);
        Assert.Equal(3, rules.Count);
        Assert.Equal("too-long", rules[1].Code);
        Assert.Equal("command.Labels.Values.Any(value => value.Length > 20)", rules[1].Condition);
        Assert.Equal("The labels must each be at most 20 characters long.", rules[1].Message);
        Assert.Equal("invalid-format", rules[2].Code);
        Assert.Equal("command.Labels.Values.Any(value => !Regex.IsMatch(value, \"^[a-z]+$\"))", rules[2].Condition);
    }

    /// <summary>Field display names drop the identity suffix and read as lowercase words.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DisplayNamesReadAsWords()
    {
        Assert.Equal("loan", ValidatorsRenderer.DisplayOf("LoanId"));
        Assert.Equal("renewal code", ValidatorsRenderer.DisplayOf("RenewalCode"));
        Assert.Equal("extension days", ValidatorsRenderer.DisplayOf("ExtensionDays"));
        Assert.Equal("currency", ValidatorsRenderer.DisplayOf("Currency"));
    }

    /// <summary>Asserts the input renders one presence rule with the condition.</summary>
    /// <param name="input">The input field as the record states it.</param>
    /// <param name="member">The message property judging the field.</param>
    /// <param name="condition">The expected presence condition.</param>
    static void AssertRule(JsonElement input, string member, string condition)
    {
        var rules = new List<ValidatorsRenderer.ValidationRule>();

        ValidatorsRenderer.AddRulesFor(rules, member, "command", false, input);

        var rule = Assert.Single(rules);

        Assert.Equal(member, rule.Member);
        Assert.Equal("required", rule.Code);
        Assert.Equal(condition, rule.Condition);
    }

    /// <summary>Asserts the input renders no rules.</summary>
    /// <param name="input">The input field as the record states it.</param>
    /// <param name="member">The message property judging the field.</param>
    static void AssertNoRules(JsonElement input, string member)
    {
        var rules = new List<ValidatorsRenderer.ValidationRule>();

        ValidatorsRenderer.AddRulesFor(rules, member, "command", false, input);

        Assert.Empty(rules);
    }

    /// <summary>Parses one input field from JSON.</summary>
    /// <param name="json">The input field JSON.</param>
    /// <returns>The parsed field.</returns>
    static JsonElement Input(string json) => JsonDocument.Parse(json).RootElement;

    /// <summary>Writes the renew-loan command with every constraint kind and the rule owning one bound.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteRenewLoan(string root)
    {
        WriteUseCase(
            root,
            "loans",
            "renew-loan",
            "Renew loan",
            "command",
            """
            - name: loanId
              meaning: The loan the reader renews.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              classification: internal
            - name: readerId
              meaning: The signed-in reader renewing the loan.
              type: id
              identifies: actor/reader
              required: true
              origin: verified-claim
              verifiedBy: entry-point/library-app
              classification: personal
            - name: note
              meaning: The note the reader leaves on the renewal.
              type: text
              minLength: 3
              maxLength: 200
              required: true
              origin: caller
              classification: internal
            - name: renewalCode
              meaning: The renewal code printed on the receipt.
              type: text
              matches: ^R-[0-9]{4}$
              required: false
              origin: caller
              classification: internal
            - name: extensionDays
              meaning: The days the loan extends.
              type: integer
              bounds:
                min: 1
                max: 30
              required: true
              origin: caller
              classification: internal
            - name: fine
              meaning: The open fine the renewal settles.
              type: decimal
              scale: 2
              bounds:
                max: 50
                inclusive: false
              required: false
              origin: caller
              classification: internal
            - name: deposit
              meaning: The deposit the reader leaves for the renewal.
              type: money
              currency: EUR
              bounds:
                min: 0
              required: true
              origin: caller
              classification: internal
            - name: adjustment
              meaning: The adjustment the desk applies to the deposit.
              type: money
              currency: EUR
              bounds:
                min: 0
              required: false
              rules:
              - rule/loan.no-negative-adjustment
              origin: caller
              classification: internal
            """);

        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "rules", "no-negative-adjustment");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "no-negative-adjustment.yaml"),
            """
            type: rule
            id: rule/loan.no-negative-adjustment
            title: No Negative Adjustment
            kind: invariant
            statement: An adjustment is never negative.
            governs:
            - use-case/lending.renew-loan
            - aggregate/lending.loan
            refusal: refusal/lending.negative-adjustment
            """);
    }

    /// <summary>Writes the search-loans query with optional constrained inputs.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteSearchLoans(string root) =>
        WriteUseCase(
            root,
            "loans",
            "search-loans",
            "Search loans",
            "query",
            """
            - name: reader
              meaning: The reader whose loans are listed.
              type: id
              identifies: actor/reader
              required: false
              origin: verified-claim
              verifiedBy: entry-point/library-app
              classification: personal
            - name: filter
              meaning: The text the loan rows match.
              type: text
              maxLength: 60
              required: false
              origin: caller
              classification: internal
            - name: limit
              meaning: The page size.
              type: integer
              bounds:
                min: 1
                max: 100
              required: false
              origin: caller
              classification: internal
            """);

    /// <summary>Writes one use case record into the prepared fixture.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="aggregate">The aggregate folder holding the use case.</param>
    /// <param name="name">The record use case name.</param>
    /// <param name="title">The use case title.</param>
    /// <param name="kind">The use case kind: command or query.</param>
    /// <param name="input">The input entries YAML.</param>
    static void WriteUseCase(string root, string aggregate, string name, string title, string kind, string input)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", aggregate, "use-cases", name);
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, $"{name}.yaml"),
            $$"""
              type: use-case
              id: use-case/lending.{{name}}
              title: {{title}}
              kind: {{kind}}
              input:
              {{input}}
              """);
    }

    /// <summary>Appends a validators target to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void AppendGenerate(string root) =>
        File.AppendAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            "\n"
            + """
              generate:
              - kind: validators
                stack: dotnet
                path: apps/api/src/Modules/Library.{Module}
                namespace: Library.{Module}
              """
            + "\n");
}
