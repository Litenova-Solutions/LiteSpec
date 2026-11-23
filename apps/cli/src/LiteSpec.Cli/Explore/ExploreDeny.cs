namespace LiteSpec.Cli.Explore;

/// <summary>One walk deny entry: a method and a path pattern the walk never calls.</summary>
/// <param name="Method">The HTTP method in upper case, or * for every method.</param>
/// <param name="Pattern">The path substring that denies, or * for every path.</param>
sealed record ExploreDeny(string Method, string Pattern)
{
    /// <summary>The default deny entries: the deletes, sign-outs and erasures the walk skips.</summary>
    /// <param name="allowDelete">Whether the walk calls DELETE operations and paths holding delete.</param>
    /// <returns>The default entries with the reason each is denied.</returns>
    internal static IReadOnlyList<(ExploreDeny Deny, string Reason)> Defaults(bool allowDelete)
    {
        var denies = new List<(ExploreDeny Deny, string Reason)>();

        if (!allowDelete)
        {
            denies.Add((new ExploreDeny("DELETE", "*"), "Deleting removes records; pass --allow-delete to walk DELETE operations."));
            denies.Add((new ExploreDeny("*", "delete"), "Deleting removes records; pass --allow-delete to walk paths holding delete."));
        }

        denies.Add((new ExploreDeny("*", "sign-out"), "Sign-out ends the walker's session."));
        denies.Add((new ExploreDeny("*", "signout"), "Sign-out ends the walker's session."));
        denies.Add((new ExploreDeny("*", "logout"), "Logout ends the walker's session."));
        denies.Add((new ExploreDeny("*", "log-out"), "Logout ends the walker's session."));
        denies.Add((new ExploreDeny("*", "erase"), "Erasure removes data the walk cannot restore."));
        denies.Add((new ExploreDeny("*", "erasure"), "Erasure removes data the walk cannot restore."));
        return denies;
    }

    /// <summary>Whether the entry denies one operation.</summary>
    /// <param name="method">The operation method in upper case.</param>
    /// <param name="pathTemplate">The operation path template.</param>
    /// <returns>Whether the method and the path match the entry.</returns>
    internal bool Matches(string method, string pathTemplate) =>
        (Method == "*" || string.Equals(Method, method, StringComparison.OrdinalIgnoreCase))
        && (Pattern == "*" || pathTemplate.Contains(Pattern, StringComparison.OrdinalIgnoreCase));
}
