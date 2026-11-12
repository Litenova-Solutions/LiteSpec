using System;

namespace LiteSpec.Model;

/// <summary>
///     Project convention identifiers: <c>&lt;project&gt;/conventions#&lt;slug&gt;</c>, the form a
///     follows or proves mark names when code follows a standing project rule.
/// </summary>
/// <follows>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</follows>
public static class ConventionIds
{
    /// <summary>Whether the identifier names a project convention: one project segment, then <c>conventions#</c> and a slug.</summary>
    /// <param name="id">The identifier to test, or null.</param>
    /// <returns>True when the identifier has the convention form; otherwise false.</returns>
    public static bool IsConventionId(string? id)
    {
        if (id is null || id.Length == 0)
        {
            return false;
        }

        var slash = id.IndexOf('/');

        if (slash <= 0 || slash != id.LastIndexOf('/'))
        {
            return false;
        }

        var rest = id.Substring(slash + 1);
        const string middle = "conventions#";

        if (!rest.StartsWith(middle, StringComparison.Ordinal) || rest.Length == middle.Length)
        {
            return false;
        }

        return rest.IndexOf('#', middle.Length) < 0;
    }
}
