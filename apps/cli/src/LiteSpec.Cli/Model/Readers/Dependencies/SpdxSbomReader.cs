using System.Text.Json;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     Reads the direct packages of an SPDX 2.x JSON bill of materials: the packages the
///     root package depends on in one step, where the root is the DESCRIBES target of
///     SPDXRef-DOCUMENT. The ecosystem and name come from each package's purl external
///     reference, and the license from licenseConcluded else licenseDeclared.
/// </summary>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
static class SpdxSbomReader
{
    /// <summary>Whether the document is an SPDX bill of materials.</summary>
    /// <param name="root">The root element of the parsed bill of materials document.</param>
    /// <returns>True when the document states the SPDX format; otherwise false.</returns>
    public static bool Is(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("spdxVersion", out var version)
        && version.ValueKind == JsonValueKind.String
        && version.GetString() is { } text
        && text.StartsWith("SPDX-", StringComparison.Ordinal);

    /// <summary>Reads every direct package of the SPDX document.</summary>
    /// <param name="file">The bill of materials file path, recorded on its packages.</param>
    /// <param name="root">The root element of the parsed document.</param>
    /// <returns>Every direct package of the document, in document order.</returns>
    public static IReadOnlyList<DirectPackage> Read(string file, JsonElement root)
    {
        if (RootId(root) is not { } rootId)
        {
            return [];
        }

        var targets = DirectTargets(root, rootId);

        if (targets.Count == 0)
        {
            return [];
        }

        var packages = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (root.TryGetProperty("packages", out var listed) && listed.ValueKind == JsonValueKind.Array)
        {
            foreach (var package in listed.EnumerateArray())
            {
                if (package.ValueKind == JsonValueKind.Object
                    && package.TryGetProperty("SPDXID", out var id)
                    && id.ValueKind == JsonValueKind.String
                    && id.GetString() is { Length: > 0 } spdxId)
                {
                    packages.TryAdd(spdxId, package);
                }
            }
        }

        var direct = new List<DirectPackage>();

        foreach (var target in targets.Order(StringComparer.Ordinal))
        {
            if (!packages.TryGetValue(target, out var package))
            {
                continue;
            }

            if (Purl.Parse(PurlOf(package) ?? "") is not { } parsed)
            {
                continue;
            }

            direct.Add(new DirectPackage(
                parsed.Ecosystem,
                parsed.Name,
                parsed.Version.Length > 0 ? parsed.Version : StringOf(package, "versionInfo"),
                LicenseOf(package),
                file));
        }

        return direct;
    }

    /// <summary>The root package: the DESCRIBES target of SPDXRef-DOCUMENT, or null when none is stated.</summary>
    /// <param name="root">The root element of the parsed document.</param>
    /// <returns>The SPDX identifier of the root package, or null when no DESCRIBES relation states one.</returns>
    private static string? RootId(JsonElement root)
    {
        if (root.TryGetProperty("relationships", out var relationships)
            && relationships.ValueKind == JsonValueKind.Array)
        {
            foreach (var relationship in relationships.EnumerateArray())
            {
                if (relationship.ValueKind == JsonValueKind.Object
                    && StringOf(relationship, "spdxElementId") == "SPDXRef-DOCUMENT"
                    && StringOf(relationship, "relationshipType") == "DESCRIBES"
                    && StringOf(relationship, "relatedSpdxElement") is { Length: > 0 } target)
                {
                    return target;
                }
            }
        }

        return null;
    }

    /// <summary>The packages the root package depends on in one step through DEPENDS_ON.</summary>
    /// <param name="root">The root element of the parsed document.</param>
    /// <param name="rootId">The root package identifier whose direct dependencies are read.</param>
    /// <returns>The identifiers the root package directly depends on.</returns>
    private static HashSet<string> DirectTargets(JsonElement root, string rootId)
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);

        if (root.TryGetProperty("relationships", out var relationships)
            && relationships.ValueKind == JsonValueKind.Array)
        {
            foreach (var relationship in relationships.EnumerateArray())
            {
                if (relationship.ValueKind == JsonValueKind.Object
                    && StringOf(relationship, "spdxElementId") == rootId
                    && StringOf(relationship, "relationshipType") == "DEPENDS_ON"
                    && StringOf(relationship, "relatedSpdxElement") is { Length: > 0 } target)
                {
                    targets.Add(target);
                }
            }
        }

        return targets;
    }

    /// <summary>The package's purl external reference locator, or null when it states none.</summary>
    /// <param name="package">The SPDX package object whose external references are read.</param>
    /// <returns>The locator text, or null when the package states no purl reference.</returns>
    private static string? PurlOf(JsonElement package)
    {
        if (package.TryGetProperty("externalRefs", out var refs) && refs.ValueKind == JsonValueKind.Array)
        {
            foreach (var reference in refs.EnumerateArray())
            {
                if (reference.ValueKind == JsonValueKind.Object
                    && StringOf(reference, "referenceType") == "purl"
                    && StringOf(reference, "referenceLocator") is { Length: > 0 } locator)
                {
                    return locator;
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     The package's license: licenseConcluded else licenseDeclared, where NONE and
    ///     NOASSERTION count as unstated and fall through. Empty when neither states one.
    /// </summary>
    /// <param name="package">The SPDX package object whose licenses are read.</param>
    /// <returns>The license text, or an empty string when the package states none.</returns>
    private static string LicenseOf(JsonElement package)
    {
        var concluded = StringOf(package, "licenseConcluded");

        if (concluded is { Length: > 0 } && concluded is not ("NONE" or "NOASSERTION"))
        {
            return concluded;
        }

        var declared = StringOf(package, "licenseDeclared");

        return declared is { Length: > 0 } && declared is not ("NONE" or "NOASSERTION") ? declared : "";
    }

    /// <summary>The string field, or empty when the object states none.</summary>
    /// <param name="owner">The JSON object that holds the field.</param>
    /// <param name="field">The field name to read.</param>
    /// <returns>The field text, or an empty string when the object states none or the field is not text.</returns>
    private static string StringOf(JsonElement owner, string field) =>
        owner.TryGetProperty(field, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
            ? text
            : "";
}
