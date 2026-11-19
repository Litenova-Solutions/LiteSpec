using LiteSpec.Model;

namespace LiteSpec.Analyzers.Tests.Support;

/// <summary>
///     Builds the additional files traceability tests hand the analyzer: the project
///     file and the record pages of the Claimwick expense product.
/// </summary>
internal static class TraceabilityFixtures
{
    /// <summary>The project file path every fixture uses.</summary>
    public const string ProjectPath = "/repo/.litespec/project.yaml";

    /// <summary>The docs root every record page lives under.</summary>
    public const string DocsRoot = "/repo/.litespec";

    /// <summary>A project file with the mark style.</summary>
    /// <param name="marks">The mark style name the project file declares.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) ProjectFile(string marks = "xml") =>
        (ProjectPath, $$"""
            schemaVersion: 1
            project:
              name: Claimwick
            marks: {{marks}}
            paths:
              docs: .litespec
            """);

    /// <summary>A use-case page with its raises list.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <param name="raises">The identifiers of the domain events the use case raises, or null for the default raised domain event.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) UseCase(
        string id = "use-case/spending.submit-expense",
        string title = "Submit expense",
        string[]? raises = null) =>
        Page("submit-expense.yaml", $$"""
            {
              "type": "use-case",
              "id": "{{id}}",
              "title": "{{title}}",
              "raises": [{{string.Join(", ", (raises ?? ["domain-event/expense.submitted"]).Select(Quote))}}]
            }
            """);

    /// <summary>A domain-event page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) DomainEvent(
        string id = "domain-event/expense.submitted",
        string title = "Expense submitted") =>
        Page(FileOf(id), $$"""
            {
              "type": "domain-event",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>A rule page with its refusal.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <param name="refusal">The refusal identifier the rule names, or null when the rule names no refusal.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Rule(
        string id = "rule/spending.receipt-required",
        string title = "Receipt required",
        string? refusal = "refusal/spending.receipt-missing")
    {
        var refusalLine = refusal is null ? "" : $",\n  \"refusal\": \"{refusal}\"";

        return Page(FileOf(id), $$"""
            {
              "type": "rule",
              "id": "{{id}}",
              "title": "{{title}}",
              "kind": "precondition",
              "statement": "An expense over twenty euros carries a receipt."{{refusalLine}}
            }
            """);
    }

    /// <summary>A result page with its outcome and refusal.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <param name="outcome">The outcome the result declares.</param>
    /// <param name="code">The stable code the result carries, written as its refusal identifier.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Result(
        string id = "result/submit-expense.receipt-missing",
        string title = "Receipt missing",
        string outcome = "refusal",
        string code = "spending.receipt-missing") =>
        Page(FileOf(id), $$"""
            {
              "type": "result",
              "id": "{{id}}",
              "title": "{{title}}",
              "outcome": "{{outcome}}",
              "refusal": "refusal/{{code}}",
              "meaning": "The expense carries no receipt."
            }
            """);

    /// <summary>A scenario page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Scenario(
        string id = "scenario/submit-expense.submitting-with-a-receipt-succeeds",
        string title = "Submitting with a receipt succeeds") =>
        Page(FileOf(id), $$"""
            {
              "type": "scenario",
              "id": "{{id}}",
              "title": "{{title}}",
              "given": "a draft expense",
              "when": "the clerk submits it with a receipt",
              "then": "the expense is submitted",
              "result": "result/submit-expense.submitted"
            }
            """);

    /// <summary>An aggregate page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Aggregate(
        string id = "aggregate/spending.expense",
        string title = "Expense") =>
        Page(FileOf(id), $$"""
            {
              "type": "aggregate",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>A job page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Job(
        string id = "job/spending.remind-approvers",
        string title = "Remind approvers") =>
        Page(FileOf(id), $$"""
            {
              "type": "job",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>A reaction page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Reaction(
        string id = "reaction/spending.notify-subscribers-on-order-placed",
        string title = "Notify subscribers on order placed") =>
        Page(FileOf(id), $$"""
            {
              "type": "reaction",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>An external-system page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) ExternalSystem(
        string id = "external-system/payments",
        string title = "Payments") =>
        Page("payments.yaml", $$"""
            {
              "type": "external-system",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>A gap waiver covering the codes on the subjects and paths until 2099, or a lasting waiver when no expiry is given.</summary>
    /// <param name="covers">The covers list: finding codes the waiver covers.</param>
    /// <param name="subjects">The subject identifiers the waiver covers, or null for none.</param>
    /// <param name="paths">The file paths the waiver covers, or null for none.</param>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="expiresOn">The date the waiver expires, as ISO date text, or null for a lasting waiver.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Waiver(
        string[] covers,
        string[]? subjects = null,
        string[]? paths = null,
        string id = "waiver/marks-deferred",
        string? expiresOn = "2099-01-01")
    {
        var closers = subjects is { Length: > 0 } ? subjects : [id];
        var tail = expiresOn is null
            ? ",\n  \"kind\": \"lasting\",\n  \"consequences\": \"Marks stay deferred.\""
            : $",\n  \"kind\": \"gap\",\n  \"closedBy\": [{string.Join(", ", closers.Select(Quote))}],\n  \"expiresOn\": \"{expiresOn}\",\n  \"exit\": \"Marks are added.\"";

        return Page(FileOf(id), $$"""
            {
              "type": "waiver",
              "id": "{{id}}",
              "title": "Marks deferred",
              "covers": [{{string.Join(", ", covers.Select(Quote))}}],
              "subject": [{{string.Join(", ", (subjects ?? []).Select(Quote))}}],
              "paths": [{{string.Join(", ", (paths ?? []).Select(Quote))}}],
              "reason": "Marks are deferred."{{tail}}
            }
            """);
    }

    /// <summary>A decision page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Decision(
        string id = "decision/0002-keep-expenses-as-documents",
        string title = "Keep expenses as documents") =>
        Page(FileOf(id), $$"""
            {
              "type": "decision",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>A technical decision page under the implementation decisions folder.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) TechnicalDecision(
        string id = "decision/0002-keep-expenses-as-documents",
        string title = "Keep expenses as documents") =>
        Page("implementation/decisions/0002-keep-expenses-as-documents.yaml", $$"""
            {
              "type": "decision",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    /// <summary>A requirement page with its kind.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <param name="kind">The requirement kind the page declares.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) Requirement(
        string id = "requirement/borrowing.borrow-latency",
        string title = "Borrow latency",
        string kind = "quality") =>
        Page(FileOf(id), $$"""
            {
              "type": "requirement",
              "id": "{{id}}",
              "title": "{{title}}",
              "kind": "{{kind}}",
              "statement": "Borrowing answers fast."
            }
            """);

    /// <summary>An integration-event page.</summary>
    /// <param name="id">The record identifier of the page.</param>
    /// <param name="title">The title of the page.</param>
    /// <returns>The path and source text of the additional file.</returns>
    public static (string Path, string Source) IntegrationEvent(
        string id = "integration-event/expense-submitted",
        string title = "Expense submitted") =>
        Page(FileOf(id), $$"""
            {
              "type": "integration-event",
              "id": "{{id}}",
              "title": "{{title}}"
            }
            """);

    private static (string Path, string Source) Page(string file, string json) =>
        ($"{DocsRoot}/{file}", YamlWriter.Write(JsonText.Parse(json)));

    private static string FileOf(string id)
    {
        var slash = id.IndexOf('/');
        var rest = slash < 0 ? id : id.Substring(slash + 1);
        var dot = rest.IndexOf('.');

        return (dot < 0 ? rest : rest.Substring(dot + 1)) + ".yaml";
    }

    private static string Quote(string value) => "\"" + value + "\"";
}
