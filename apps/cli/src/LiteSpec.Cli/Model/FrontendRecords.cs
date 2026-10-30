namespace LiteSpec.Model;

/// <summary>
///     The generated record file each web frontend imports from its frame: the
///     use-case and screen ids of that frontend's own entry points, so a
///     mistyped mark fails the type check, and the <c>proves</c> helper a
///     browser test annotates with.
/// </summary>
/// <follows>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</follows>
static class FrontendRecords
{
    /// <summary>How long a single-line union may run before each id takes its own line.</summary>
    private const int MaxSingleLineLength = 120;

    /// <summary>
    ///     Writes <c>litespec-records.ts</c> into every Svelte and React frontend's
    ///     frame, returning the written paths relative to the root.
    /// </summary>
    public static IReadOnlyList<string> Write(ProjectModel model)
    {
        var written = new List<string>();

        foreach (var frontend in model.Config.Frontends)
        {
            if (frontend.Platform is not ("svelte" or "react"))
            {
                continue;
            }

            var path = Path.Combine(frontend.FrameRoot, "litespec-records.ts");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, Render(
                UseCaseIds(model, frontend),
                ScreenIds(model, frontend),
                ProofTargetIds(model)));
            written.Add(model.Config.Relative(path));
        }

        return written;
    }

    /// <summary>
    ///     The entry points one frontend serves: the entry points of the screens
    ///     it holds a route for, or every browser entry point when it holds none.
    /// </summary>
    public static IReadOnlyList<string> EntryPointsOf(ProjectModel model, FrontendConfig frontend)
    {
        if (frontend.EntryPoint is { Length: > 0 } named)
        {
            return [named];
        }

        var own = model.Screens
            .Where(screen => screen.Frontend == frontend.Name && screen.ScreenId is not null)
            .Select(screen => screen.ScreenId!)
            .Where(id => model.ById.TryGetValue(id, out var record) && record.Type == "screen")
            .SelectMany(id => model.ById[id].Ids("entryPoint"))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (own.Count > 0)
        {
            return own;
        }

        return
        [
            .. model.Records
                .Where(record => record.Type == "entry-point" && record.Field("kind") == "browser")
                .Select(record => record.Id)
                .OrderBy(id => id, StringComparer.Ordinal),
        ];
    }

    /// <summary>Every use case reached through one of the frontend's entry points, sorted.</summary>
    public static IReadOnlyList<string> UseCaseIds(ProjectModel model, FrontendConfig frontend)
    {
        var entryPoints = new HashSet<string>(EntryPointsOf(model, frontend), StringComparer.Ordinal);

        return
        [
            .. model.Records
                .Where(record => record.Type == "use-case"
                    && record.Ids("reachedThrough").Any(entryPoints.Contains))
                .Select(record => record.Id)
                .OrderBy(id => id, StringComparer.Ordinal),
        ];
    }

    /// <summary>Every use case reached through a browser entry point, sorted (project-wide).</summary>
    public static IReadOnlyList<string> UseCaseIds(ProjectModel model) =>
    [
        .. model.Records
            .Where(record => record.Type == "use-case" && ReachedThroughBrowser(model, record))
            .Select(record => record.Id)
            .OrderBy(id => id, StringComparer.Ordinal),
    ];

    /// <summary>Every screen of one of the frontend's entry points, sorted.</summary>
    public static IReadOnlyList<string> ScreenIds(ProjectModel model, FrontendConfig frontend)
    {
        var entryPoints = new HashSet<string>(EntryPointsOf(model, frontend), StringComparer.Ordinal);

        return
        [
            .. model.Records
                .Where(record => record.Type == "screen"
                    && record.Ids("entryPoint").Any(entryPoints.Contains))
                .Select(record => record.Id)
                .OrderBy(id => id, StringComparer.Ordinal),
        ];
    }

    /// <summary>
    ///     Every record id a browser test may prove, and every LiteSpec frontend provision, such as
    ///     the route suite's, sorted.
    /// </summary>
    public static IReadOnlyList<string> ProofTargetIds(ProjectModel model) =>
    [
        .. model.Records
            .Where(record => record.Type is "scenario" or "rule" or "requirement" or "decision"
                or "external-system" or "integration-event")
            .Select(record => record.Id)
            .Concat(model.Provisions.Where(id => id.StartsWith("litespec/frontend/", StringComparison.Ordinal)))
            .OrderBy(id => id, StringComparer.Ordinal),
    ];

    /// <summary>Renders the records file for one frontend.</summary>
    public static string Render(
        IReadOnlyList<string> useCaseIds,
        IReadOnlyList<string> screenIds,
        IReadOnlyList<string> proofTargetIds)
    {
        var lines = new List<string>
        {
            "// This file is generated by litespec model build. Do not edit it by hand.",
        };

        lines.AddRange(Union("UseCaseId", useCaseIds));
        lines.AddRange(Union("ScreenId", screenIds));
        lines.AddRange(Union("ProofTargetId", proofTargetIds));
        lines.Add("export interface LiteSpecMark { readonly implements: UseCaseId | ScreenId }");
        lines.Add("export function proves(id: ProofTargetId): { annotation: [{ type: \"proves\"; description: ProofTargetId }] } {");
        lines.Add("  return { annotation: [{ type: \"proves\", description: id }] };");
        lines.Add("}");

        return string.Join('\n', lines) + "\n";
    }

    private static IReadOnlyList<string> Union(string name, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            return [$"export type {name} = never;"];
        }

        var single = $"export type {name} = {string.Join(" | ", ids.Select(id => $"\"{id}\""))};";

        if (single.Length <= MaxSingleLineLength)
        {
            return [single];
        }

        var lines = new List<string> { $"export type {name} =" };

        for (var index = 0; index < ids.Count; index++)
        {
            lines.Add(index + 1 < ids.Count ? $"  | \"{ids[index]}\"" : $"  | \"{ids[index]}\";");
        }

        return lines;
    }

    private static bool ReachedThroughBrowser(ProjectModel model, Record record) =>
        record.Ids("reachedThrough").Any(id =>
            model.ById.TryGetValue(id, out var point)
            && point.Type == "entry-point"
            && point.Field("kind") == "browser");
}
