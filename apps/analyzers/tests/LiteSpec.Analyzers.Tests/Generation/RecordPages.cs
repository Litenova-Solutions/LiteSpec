using LiteSpec.Model;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Builds the project files, record pages, sources and additional files generator tests run over.</summary>
internal static class RecordPages
{
    /// <summary>The project file path every generator test uses.</summary>
    public const string ProjectPath = "/project/.litespec/project.yaml";

    /// <summary>Wraps one source in the single-file list.</summary>
    /// <param name="source">The C# source.</param>
    /// <returns>The one-file list.</returns>
    public static IReadOnlyList<(string Path, string Source)> Sources(string source) =>
        [("Source.cs", source)];

    /// <summary>Builds the additional files: the project file with the mark style, then each record page under the behavior docs.</summary>
    /// <param name="marks">The declared mark style.</param>
    /// <param name="pages">The record pages with their file names.</param>
    /// <returns>The additional files.</returns>
    public static IReadOnlyList<(string Path, string Text)> Files(
        string marks, params (string Name, string Text)[] pages)
    {
        var files = new List<(string Path, string Text)>
        {
            (ProjectPath, Project(marks)),
        };

        foreach (var page in pages)
        {
            files.Add(("/project/.litespec/behavior/" + page.Name, page.Text));
        }

        return files;
    }

    /// <summary>Builds the project file with the mark style and the documentation roots.</summary>
    /// <param name="marks">The declared mark style.</param>
    /// <returns>The project file text.</returns>
    public static string Project(string marks) =>
        $"schemaVersion: 1\nmarks: {marks}\npaths:\n  docs: .litespec\n  behaviorDocs: .litespec/behavior\n";

    /// <summary>Builds a domain-event page.</summary>
    /// <param name="id">The domain-event identifier.</param>
    /// <returns>The page text.</returns>
    public static string DomainEventPage(string id) =>
        Page("{\"type\": \"domain-event\", \"id\": \"" + id + "\", \"title\": \"Expense submitted\"}");

    /// <summary>Builds one rule record file at the top level.</summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="refusal">The refusal identifier, or null for no refusal.</param>
    /// <returns>The rule page text.</returns>
    public static string RulePage(string id, string? refusal) => Page(RuleEntry(id, refusal));

    /// <summary>Builds the fields of one rule record.</summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="refusal">The refusal identifier, or null for no refusal.</param>
    /// <returns>The rule fields as one JSON object.</returns>
    public static string RuleEntry(string id, string? refusal) =>
        "{\"type\": \"rule\", \"id\": \"" + id + "\", \"title\": \"A receipt is required\", \"kind\": \"precondition\", \"statement\": \"An expense carries a receipt.\""
        + (refusal is null ? string.Empty : ", \"refusal\": \"" + refusal + "\"")
        + "}";

    /// <summary>Builds a result entry.</summary>
    /// <param name="id">The result identifier.</param>
    /// <param name="outcome">The outcome: success or refusal.</param>
    /// <param name="code">The refusal code, or null for none.</param>
    /// <returns>The entry object.</returns>
    public static string ResultEntry(string id, string outcome, string? code) =>
        "{\"type\": \"result\", \"id\": \"" + id + "\", \"title\": \"Receipt missing\", \"outcome\": \"" + outcome + "\""
        + (code is null ? string.Empty : ", \"code\": \"" + code + "\"")
        + ", \"meaning\": \"No receipt was attached.\"}";

    /// <summary>Builds a decision page.</summary>
    /// <param name="id">The decision identifier.</param>
    /// <returns>The page text.</returns>
    public static string DecisionPage(string id) =>
        Page("{\"type\": \"decision\", \"id\": \"" + id + "\", \"title\": \"Keep expenses as documents\"}");

    /// <summary>Builds a use-case page.</summary>
    /// <param name="id">The use-case identifier.</param>
    /// <param name="grant">The access grant, or null for no access object.</param>
    /// <returns>The page text.</returns>
    public static string UseCasePage(string id, string? grant) =>
        Page("{\"type\": \"use-case\", \"id\": \"" + id + "\", \"title\": \"Submit expense\", \"kind\": \"command\""
        + (grant is null ? string.Empty : ", \"access\": {\"auth\": [\"session\"], \"grant\": \"" + grant + "\", \"tenant\": \"own\"}")
        + "}");

    /// <summary>Builds a permission-set page holding one entry per identifier.</summary>
    /// <param name="ids">The permission identifiers.</param>
    /// <returns>The page text.</returns>
    public static string PermissionPage(params string[] ids)
    {
        var entries = new List<string>();

        foreach (var id in ids)
        {
            entries.Add("{\"type\": \"permission\", \"id\": \"" + id + "\", \"title\": \"A permission\", \"meaning\": \"Its meaning.\"}");
        }

        return Page("{\"type\": \"permission-set\", \"permissions\": [" + string.Join(", ", entries) + "]}");
    }

    /// <summary>Builds a permission entry with the group, risks and granted roles the record states. The reach comes from the roles that grant it.</summary>
    /// <param name="id">The permission identifier.</param>
    /// <param name="group">The group, or null for no group field.</param>
    /// <param name="risks">The risks, or null for no risk field.</param>
    /// <param name="grantedTo">The granted roles, or null for no grantedTo field.</param>
    /// <returns>The entry object.</returns>
    public static string PermissionEntry(string id, string? group = null, string[]? risks = null, string[]? grantedTo = null)
    {
        var entry = "{\"type\": \"permission\", \"id\": \"" + id + "\", \"title\": \"A permission\", \"meaning\": \"Its meaning.\"";

        if (group is not null)
        {
            entry += ", \"group\": \"" + group + "\"";
        }

        if (risks is not null)
        {
            entry += ", \"risk\": [" + string.Join(", ", risks.Select(risk => "\"" + risk + "\"")) + "]";
        }

        if (grantedTo is not null)
        {
            entry += ", \"grantedTo\": [" + string.Join(", ", grantedTo.Select(role => "\"" + role + "\"")) + "]";
        }

        return entry + "}";
    }

    /// <summary>Builds a permission-set page holding the entries.</summary>
    /// <param name="entries">The entry objects.</param>
    /// <returns>The page text.</returns>
    public static string PermissionFactsPage(params string[] entries) =>
        Page("{\"type\": \"permission-set\", \"permissions\": [" + string.Join(", ", entries) + "]}");

    /// <summary>Builds a role page with the scope the record states.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="scope">The reach, or null for no scope field.</param>
    /// <returns>The page text.</returns>
    public static string RolePage(string id, string? scope = null)
    {
        var page = "{\"type\": \"role\", \"id\": \"" + id + "\", \"title\": \"A role\"";

        if (scope is not null)
        {
            page += ", \"scope\": \"" + scope + "\"";
        }

        return Page(page + "}");
    }

    private static string Page(string json) => YamlWriter.Write(JsonText.Parse(json));
}
