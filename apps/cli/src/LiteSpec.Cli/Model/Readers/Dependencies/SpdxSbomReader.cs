using System.Text.Json;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     Reads the direct packages of an SPDX 2.x JSON bill of materials: the packages the
///     root package depends on in one step, where the root is the DESCRIBES target of
///     SPDXRef-DOCUMENT. The ecosystem and name come from each package's purl external
///     reference, and the licence from licenseConcluded else licenseDeclared.
/// </summary>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
static class SpdxSbomReader
{
    /// <summary>Whether the document is an SPDX bill of materials.</summary>
    public static bool Is(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("spdxVersion", out var version)
        && version.ValueKind == JsonValueKind.String
        && version.GetString() is { } text
        && text.StartsWith("SPDX-", StringComparison.Ordinal);

    /// <summary>Reads every direct package of the SPDX document.</summary>
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
                LicenceOf(package),
                file));
        }

        return direct;
    }

    /// <summary>The root package: the DESCRIBES target of SPDXRef-DOCUMENT, or null when none is stated.</summary>
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
    ///     The package's licence: licenseConcluded else licenseDeclared, where NONE and
    ///     NOASSERTION count as unstated and fall through. Empty when neither states one.
    /// </summary>
    private static string LicenceOf(JsonElement package)
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
    private static string StringOf(JsonElement owner, string field) =>
        owner.TryGetProperty(field, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { } text
            ? text
            : "";
}
