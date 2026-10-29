namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     A package URL split into the parts the dependency gate compares: the ecosystem,
///     the decoded name with its scope, and the version.
/// </summary>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
static class Purl
{
    /// <summary>
    ///     Parses a package URL such as pkg:nuget/PdfSharp@6.2.0 or
    ///     pkg:npm/%40scope/name@1.0.0. Qualifiers and subpaths are dropped, the name
    ///     is percent-decoded, and a URL without a version yields an empty version.
    ///     Returns null when the value is no package URL.
    /// </summary>
    public static (string Ecosystem, string Name, string Version)? Parse(string value)
    {
        if (!value.StartsWith("pkg:", StringComparison.Ordinal))
        {
            return null;
        }

        var rest = value["pkg:".Length..];
        var fragment = rest.IndexOf('#');

        if (fragment >= 0)
        {
            rest = rest[..fragment];
        }

        var qualifiers = rest.IndexOf('?');

        if (qualifiers >= 0)
        {
            rest = rest[..qualifiers];
        }

        var slash = rest.IndexOf('/');

        if (slash <= 0 || slash == rest.Length - 1)
        {
            return null;
        }

        var ecosystem = rest[..slash].ToLowerInvariant();
        var remainder = rest[(slash + 1)..];
        var version = "";
        var namePart = remainder;
        var at = remainder.LastIndexOf('@');

        if (at >= 0)
        {
            namePart = remainder[..at];
            version = remainder[(at + 1)..];
        }

        var name = Uri.UnescapeDataString(namePart);

        return name.Length > 0 ? (ecosystem, name, version) : null;
    }
}
