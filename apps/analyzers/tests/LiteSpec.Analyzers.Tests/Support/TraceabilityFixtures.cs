namespace LiteSpec.Analyzers.Tests.Support;

/// <summary>
///     Builds the additional files traceability tests hand the analyzer: the project
///     file and the record pages of the Claimwick expense product.
/// </summary>
internal static class TraceabilityFixtures
{
    /// <summary>The project file path every fixture uses.</summary>
    public const string ProjectPath = "/repo/litespec.project.json";

    /// <summary>The docs root every record page lives under.</summary>
    public const string DocsRoot = "/repo/docs";

    /// <summary>A project file with the mark style and the check phase.</summary>
    public static (string Path, string Source) ProjectFile(string marks = "xml", string phase = "working") =>
        (ProjectPath, $$"""
            {
              "project": "Claimwick",
              "marks": "{{marks}}",
              "check": { "phase": "{{phase}}" },
              "paths": { "docs": "docs" }
            }
            """);

    /// <summary>A use-case page with its raises list and status.</summary>
    public static (string Path, string Source) UseCase(
        string id = "use-case/spending.submit-expense",
        string title = "Submit expense",
        string status = "approved",
        string[]? raises = null) =>
        Page("submit-expense.md", $$"""
            {
              "type": "use-case",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}",
              "raises": [{{string.Join(", ", (raises ?? ["event/expense.submitted"]).Select(Quote))}}]
            }
            """, title);

    /// <summary>An event page with its status.</summary>
    public static (string Path, string Source) Event(
        string id = "event/expense.submitted",
        string title = "Expense submitted",
        string status = "approved") =>
        Page(FileOf(id), $$"""
            {
              "type": "event",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}"
            }
            """, title);

    /// <summary>A rule page with its refusal and status.</summary>
    public static (string Path, string Source) Rule(
        string id = "rule/spending.receipt-required",
        string title = "Receipt required",
        string status = "approved",
        string? refusal = "result/submit-expense.receipt-missing") =>
        Page(FileOf(id), $$"""
            {
              "type": "rule",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}",
              "kind": "precondition",
              "statement": "An expense over twenty euros carries a receipt.",
              "refusal": "{{refusal}}"
            }
            """, title);

    /// <summary>A result page with its outcome, code and status.</summary>
    public static (string Path, string Source) Result(
        string id = "result/submit-expense.receipt-missing",
        string title = "Receipt missing",
        string outcome = "refusal",
        string code = "spending.receipt-missing",
        string status = "approved") =>
        Page(FileOf(id), $$"""
            {
              "type": "result",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}",
              "outcome": "{{outcome}}",
              "code": "{{code}}",
              "meaning": "The expense carries no receipt."
            }
            """, title);

    /// <summary>A scenario page with its status.</summary>
    public static (string Path, string Source) Scenario(
        string id = "scenario/submit-expense.submitting-with-a-receipt-succeeds",
        string title = "Submitting with a receipt succeeds",
        string status = "approved") =>
        Page(FileOf(id), $$"""
            {
              "type": "scenario",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}",
              "given": "a draft expense",
              "when": "the clerk submits it with a receipt",
              "then": "the expense is submitted",
              "result": "result/submit-expense.submitted"
            }
            """, title);

    /// <summary>An aggregate page with its status.</summary>
    public static (string Path, string Source) Aggregate(
        string id = "aggregate/spending.expense",
        string title = "Expense",
        string status = "approved") =>
        Page(FileOf(id), $$"""
            {
              "type": "aggregate",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}"
            }
            """, title);

    /// <summary>A reaction page with its status.</summary>
    public static (string Path, string Source) Reaction(
        string id = "reaction/expense.notify-manager",
        string title = "Notify manager",
        string status = "approved") =>
        Page(FileOf(id), $$"""
            {
              "type": "reaction",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}"
            }
            """, title);

    /// <summary>A job page with its status.</summary>
    public static (string Path, string Source) Job(
        string id = "job/spending.remind-approvers",
        string title = "Remind approvers",
        string status = "approved") =>
        Page(FileOf(id), $$"""
            {
              "type": "job",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}"
            }
            """, title);

    /// <summary>An external-system page with its status.</summary>
    public static (string Path, string Source) ExternalSystem(
        string id = "external-system/payments",
        string title = "Payments",
        string status = "approved") =>
        Page("payments.md", $$"""
            {
              "type": "external-system",
              "id": "{{id}}",
              "title": "{{title}}",
              "status": "{{status}}"
            }
            """, title);

    /// <summary>An approved deviation covering the files and subjects until 2099.</summary>
    public static (string Path, string Source) Deviation(
        string[] code, string[]? subjects = null, string reviewBy = "2099-01-01") =>
        Page("deviation.md", $$"""
            {
              "type": "deviation",
              "id": "deviation/marks-deferred",
              "title": "Marks deferred",
              "status": "approved",
              "reviewBy": "{{reviewBy}}",
              "code": [{{string.Join(", ", code.Select(Quote))}}],
              "subject": [{{string.Join(", ", (subjects ?? []).Select(Quote))}}]
            }
            """, "Marks deferred");

    /// <summary>An open known gap covering the check on the subjects until 2099.</summary>
    public static (string Path, string Source) KnownGap(string check, string[] subjects, string endsBy = "2099-01-01") =>
        Page("gap.md", $$"""
            {
              "type": "known-gap",
              "id": "known-gap/marks",
              "title": "Marks",
              "status": "approved",
              "check": "{{check}}",
              "subject": [{{string.Join(", ", subjects.Select(Quote))}}],
              "endsBy": "{{endsBy}}"
            }
            """, "Marks");

    private static (string Path, string Source) Page(string file, string json, string heading) =>
        ($"{DocsRoot}/{file}", $"---\n{json}\n---\n\n# {heading}\n");

    private static string FileOf(string id)
    {
        var slash = id.IndexOf('/');
        var rest = slash < 0 ? id : id.Substring(slash + 1);
        var dot = rest.IndexOf('.');

        return (dot < 0 ? rest : rest.Substring(dot + 1)) + ".md";
    }

    private static string Quote(string value) => "\"" + value + "\"";
}
