using System.Text.Json;

namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     Reads the direct packages of a CycloneDX 1.x JSON bill of materials: the dependsOn of
///     the dependencies entry whose ref is metadata.component.bom-ref. The ecosystem and name
///     come from each component's purl, and the license from its licenses entries.
/// </summary>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
static class CycloneDxSbomReader
{
    /// <summary>Whether the document is a CycloneDX bill of materials.</summary>
    /// <param name="root">The root element of the parsed bill of materials document.</param>
    /// <returns>True when the document states the CycloneDX format; otherwise false.</returns>
    public static bool Is(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("bomFormat", out var format)
        && format.ValueKind == JsonValueKind.String
        && format.GetString() == "CycloneDX";

    /// <summary>Reads every direct package of the CycloneDX document.</summary>
    /// <param name="file">The bill of materials file path, recorded on its packages.</param>
    /// <param name="root">The root element of the parsed document.</param>
    /// <returns>Every direct package of the document, in document order.</returns>
    public static IReadOnlyList<DirectPackage> Read(string file, JsonElement root)
    {
        if (RootRef(root) is not { } rootRef)
        {
            return [];
        }

        var targets = DirectTargets(root, rootRef);

        if (targets.Count == 0)
        {
            return [];
        }

        var components = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (root.TryGetProperty("components", out var listed) && listed.ValueKind == JsonValueKind.Array)
        {
            foreach (var component in listed.EnumerateArray())
            {
                if (component.ValueKind == JsonValueKind.Object
                    && component.TryGetProperty("bom-ref", out var reference)
                    && reference.ValueKind == JsonValueKind.String
                    && reference.GetString() is { Length: > 0 } bomRef)
                {
                    components.TryAdd(bomRef, component);
                }
            }
        }

        var direct = new List<DirectPackage>();

        foreach (var target in targets.Order(StringComparer.Ordinal))
        {
            if (!components.TryGetValue(target, out var component))
            {
                continue;
            }

            if (Purl.Parse(StringOf(component, "purl")) is not { } parsed)
            {
                continue;
            }

            direct.Add(new DirectPackage(
                parsed.Ecosystem,
                parsed.Name,
                parsed.Version.Length > 0 ? parsed.Version : StringOf(component, "version"),
                LicenseOf(component),
                file));
        }

        return direct;
    }

    /// <summary>The root component's bom-ref from metadata.component, or null when none is stated.</summary>
    /// <param name="root">The root element of the parsed document.</param>
    /// <returns>The bom-ref text, or null when metadata states no component.</returns>
    private static string? RootRef(JsonElement root)
    {
        if (root.TryGetProperty("metadata", out var metadata)
            && metadata.ValueKind == JsonValueKind.Object
            && metadata.TryGetProperty("component", out var component)
            && component.ValueKind == JsonValueKind.Object
            && component.TryGetProperty("bom-ref", out var reference)
            && reference.ValueKind == JsonValueKind.String
            && reference.GetString() is { Length: > 0 } bomRef)
        {
            return bomRef;
        }

        return null;
    }

    /// <summary>The dependsOn of the dependencies entry whose ref is the root's bom-ref.</summary>
    /// <param name="root">The root element of the parsed document.</param>
    /// <param name="rootRef">The root component bom-ref whose direct dependencies are read.</param>
    /// <returns>The bom-refs the root component directly depends on.</returns>
    private static HashSet<string> DirectTargets(JsonElement root, string rootRef)
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);

        if (root.TryGetProperty("dependencies", out var dependencies)
            && dependencies.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in dependencies.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || StringOf(entry, "ref") != rootRef
                    || !entry.TryGetProperty("dependsOn", out var dependsOn)
                    || dependsOn.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var target in dependsOn.EnumerateArray())
                {
                    if (target.ValueKind == JsonValueKind.String
                        && target.GetString() is { Length: > 0 } bomRef)
                    {
                        targets.Add(bomRef);
                    }
                }
            }
        }

        return targets;
    }

    /// <summary>
    ///     The component's license: the first licenses expression, else every
    ///     licenses license id joined by OR. Empty when the component states none.
    /// </summary>
    /// <param name="component">The CycloneDX component object whose licenses are read.</param>
    /// <returns>The license text, or an empty string when the component states none.</returns>
    private static string LicenseOf(JsonElement component)
    {
        if (!component.TryGetProperty("licenses", out var licenses)
            || licenses.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        var ids = new List<string>();

        foreach (var entry in licenses.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (StringOf(entry, "expression") is { Length: > 0 } expression)
            {
                return expression;
            }

            if (entry.TryGetProperty("license", out var license)
                && license.ValueKind == JsonValueKind.Object
                && StringOf(license, "id") is { Length: > 0 } id)
            {
                ids.Add(id);
            }
        }

        return string.Join(" OR ", ids);
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
