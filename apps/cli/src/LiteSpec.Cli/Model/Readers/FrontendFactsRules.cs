namespace LiteSpec.Model.Readers;

/// <summary>Address matching and call mapping shared by every frontend reader.</summary>
/// <follows>litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case</follows>
static class FrontendFactsRules
{
    /// <summary>
    ///     Whether a <c>page.goto</c> target reaches a screen at the address: the
    ///     segments are equal, and a <c>[param]</c> or <c>{}</c> segment of either
    ///     side matches any one segment of the other.
    /// </summary>
    /// <param name="address">The route address a screen serves.</param>
    /// <param name="target">The target address to match against the address.</param>
    /// <returns>True when the segment counts are equal and every segment matches, counting a parameter segment as matching any one segment; otherwise false.</returns>
    public static bool AddressMatches(string address, string target)
    {
        var left = Segments(address);
        var right = Segments(target);

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (IsParameter(left[index]) || IsParameter(right[index]))
            {
                continue;
            }

            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The segments of a route address, without empty entries.</summary>
    /// <param name="address">The route address to split into segments.</param>
    /// <returns>The address segments, without empty entries.</returns>
    private static IReadOnlyList<string> Segments(string address) =>
        [.. address.Split('/', StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>
    ///     Whether a route segment is written as a parameter: braces, brackets, a dollar prefix, or a colon
    ///     prefix.
    /// </summary>
    /// <param name="segment">The address segment to test.</param>
    /// <returns>True when the segment is written as a path parameter; otherwise false.</returns>
    private static bool IsParameter(string segment) =>
        segment == "{}"
        || (segment.StartsWith('[') && segment.EndsWith(']'))
        || (segment.StartsWith('{') && segment.EndsWith('}'))
        || segment.StartsWith('$')
        || segment.StartsWith(':');
}
