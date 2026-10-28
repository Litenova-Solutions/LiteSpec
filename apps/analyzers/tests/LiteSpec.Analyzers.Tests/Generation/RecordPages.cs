namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Builds the project files, record pages, sources and additional files generator tests run over.</summary>
internal static class RecordPages
{
    /// <summary>The project file path every generator test uses.</summary>
    public const string ProjectPath = "/project/litespec.project.json";

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
            files.Add(("/project/docs/behavior/" + page.Name, page.Text));
        }

        return files;
    }

    /// <summary>Builds the project file with the mark style and the documentation roots.</summary>
    /// <param name="marks">The declared mark style.</param>
    /// <returns>The project file text.</returns>
    public static string Project(string marks) =>
        "{\"marks\": \"" + marks + "\", \"paths\": {\"docs\": \"docs\", \"behaviorDocs\": \"docs/behavior\"}}";

    /// <summary>Builds an event page.</summary>
    /// <param name="id">The event identifier.</param>
    /// <returns>The page text.</returns>
    public static string EventPage(string id) =>
        Page("{\"type\": \"event\", \"id\": \"" + id + "\", \"title\": \"Expense submitted\", \"owner\": \"team\", \"status\": \"reviewed\"}");

    /// <summary>Builds a rule-set page holding the entries.</summary>
    /// <param name="entries">The entry objects.</param>
    /// <returns>The page text.</returns>
    public static string RuleSetPage(params string[] entries) =>
        Page("{\"type\": \"rule-set\", \"owner\": \"team\", \"status\": \"reviewed\", \"records\": [" + string.Join(", ", entries) + "]}");

    /// <summary>Builds a rule entry.</summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="refusal">The refusal result or code, or null for no refusal.</param>
    /// <returns>The entry object.</returns>
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

    /// <summary>Builds a use-case page.</summary>
    /// <param name="id">The use-case identifier.</param>
    /// <param name="grant">The access grant, or null for no access object.</param>
    /// <returns>The page text.</returns>
    public static string UseCasePage(string id, string? grant) =>
        Page("{\"type\": \"use-case\", \"id\": \"" + id + "\", \"title\": \"Submit expense\", \"owner\": \"team\", \"status\": \"reviewed\", \"kind\": \"command\""
        + (grant is null ? string.Empty : ", \"access\": {\"auth\": [\"session\"], \"grant\": \"" + grant + "\", \"tenant\": \"own\"}")
        + "}");

    /// <summary>Builds a permission-list page holding one entry per identifier.</summary>
    /// <param name="ids">The permission identifiers.</param>
    /// <returns>The page text.</returns>
    public static string PermissionPage(params string[] ids)
    {
        var entries = new List<string>();

        foreach (var id in ids)
        {
            entries.Add("{\"type\": \"permission\", \"id\": \"" + id + "\", \"title\": \"A permission\", \"meaning\": \"Its meaning.\"}");
        }

        return Page("{\"type\": \"permission-list\", \"owner\": \"team\", \"status\": \"reviewed\", \"records\": [" + string.Join(", ", entries) + "]}");
    }

    private static string Page(string json) => "---\n" + json + "\n---\n\n# Record\n";
}
