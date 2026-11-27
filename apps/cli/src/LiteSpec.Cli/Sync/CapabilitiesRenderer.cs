namespace LiteSpec.Cli.Sync;

/// <summary>
///     Renders the screen capabilities the records declare as TypeScript: one entry per
///     screen route holding the permissions the use cases the screen offers need.
/// </summary>
/// <follows>litespec/core/model#sync-record-determined-code-from-the-records</follows>
static class CapabilitiesRenderer
{
    /// <summary>One screen route with the permissions its offered use cases need.</summary>
    /// <param name="Route">The screen route, such as /expenses.</param>
    /// <param name="Permissions">The distinct permission ids the offered use cases grant, in identifier order.</param>
    public sealed record ScreenCapability(string Route, IReadOnlyList<string> Permissions);

    /// <summary>
    ///     Renders every screen route as TypeScript: a record from each route to the
    ///     permissions the use cases the screen offers need.
    /// </summary>
    /// <param name="screens">Every screen route with the permissions its offered use cases need.</param>
    /// <returns>The file text with LF line endings.</returns>
    public static string RenderTypeScript(IReadOnlyList<ScreenCapability> screens)
    {
        var ordered = screens.OrderBy(screen => screen.Route, StringComparer.Ordinal).ToList();
        var lines = new List<string>
        {
            SyncNaming.HeaderLine,
            "import type { PermissionId } from \"./permissions\";",
            "export const screenCapabilities: Record<string, readonly PermissionId[]> = {",
        };

        foreach (var screen in ordered)
        {
            var permissions = string.Join(", ", screen.Permissions.Select(permission => $"\"{SyncNaming.EscapeLiteral(permission)}\""));

            lines.Add($"  \"{SyncNaming.EscapeLiteral(screen.Route)}\": [{permissions}],");
        }

        lines.Add("};");

        return string.Join('\n', lines) + "\n";
    }
}
