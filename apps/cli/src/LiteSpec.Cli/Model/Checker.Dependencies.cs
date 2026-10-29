using System.Text.Json;
using LiteSpec.Model.Readers.Dependencies;

namespace LiteSpec.Model;

/// <summary>
///     The dependency checks: every direct package in the bill of materials is admitted by
///     a manifest pin or a technical decision, ships at its manifest pin, and carries an
///     accepted licence, and every admission names its six facts.
/// </summary>
static partial class Checker
{
    /// <summary>Runs the dependency checks.</summary>
    internal static void CheckDependencies(ProjectModel model, List<Finding> findings) =>
        DependencyChecks.Run(model, findings);
}

/// <summary>
///     The dependency checks, kept in one class so the partial checker holds only the entry point.
/// </summary>
/// <follows>litespec/workspace/dependencies#approve-new-packages-explicitly</follows>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
/// <follows>litespec/workspace/dependencies#accept-every-shipped-package-licence-explicitly</follows>
static class DependencyChecks
{
    /// <summary>The six facts every admits entry names.</summary>
    private static readonly string[] AdmissionFacts =
    [
        "layer", "licence", "location", "accountOwner", "failureBehavior", "operatingCost",
    ];

    /// <summary>SPDX expression operators: every other token is a licence identifier.</summary>
    private static readonly HashSet<string> ExpressionOperators = new(StringComparer.Ordinal)
    {
        "AND", "OR", "WITH",
    };

    /// <summary>
    ///     Runs admission-incomplete, which always runs, and the three bill checks, which stay
    ///     silent when the project names no bill of materials in paths.sbom.
    /// </summary>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckAdmissionsComplete(model, findings);

        if (model.Config.Sboms.Count == 0)
        {
            return;
        }

        var packages = SbomReader.Read(model.Config);

        if (packages.Count == 0)
        {
            return;
        }

        var pins = ManifestPins.Read();
        var admissions = AdmissionsOf(model);

        foreach (var package in packages)
        {
            var admitted = AdmissionFor(admissions, package);
            var pin = PinOf(pins, package);

            if (pin is null && admitted is null)
            {
                findings.Add(new Finding(
                    "dependency-unadmitted",
                    $"Direct package '{package.Id}' version {VersionText(package.Version)} with licence {LicenceText(package.Licence)} is pinned by no manifest entry and admitted by no decision.",
                    package.File,
                    1,
                    null));
            }

            if (pin is not null && !pin.Equals(package.Version, StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "dependency-version-drift",
                    $"Direct package '{package.Id}' version {VersionText(package.Version)} with licence {LicenceText(package.Licence)} differs from its manifest pin '{pin}'.",
                    package.File,
                    1,
                    null));
            }

            if (!LicenceAccepted(model.Config.AcceptedLicences, admitted?.Licence, package.Licence))
            {
                findings.Add(new Finding(
                    "dependency-licence-unapproved",
                    $"Direct package '{package.Id}' version {VersionText(package.Version)} carries licence {LicenceText(package.Licence)}, which the project does not accept.",
                    package.File,
                    1,
                    null));
            }
        }
    }

    /// <summary>One admission: the admitted package id and the licence its entry names, when it names one.</summary>
    private sealed record Admission(string Ecosystem, string Name, string? Licence);

    /// <summary>
    ///     Every admission: the admits entries of the technical decisions that are not retired.
    /// </summary>
    private static List<Admission> AdmissionsOf(ProjectModel model)
    {
        var admissions = new List<Admission>();

        foreach (var record in model.Records)
        {
            if (record.Type != "decision"
                || record.Status == "retired"
                || !Checker.UnderImplementationDecisions(model.Config, record.File)
                || record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty("admits", out var admits)
                || admits.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in admits.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty("package", out var package)
                    || package.ValueKind != JsonValueKind.String
                    || package.GetString() is not { Length: > 0 } id)
                {
                    continue;
                }

                var separator = id.IndexOf(':');

                if (separator <= 0 || separator == id.Length - 1)
                {
                    continue;
                }

                admissions.Add(new Admission(
                    id[..separator],
                    id[(separator + 1)..],
                    entry.TryGetProperty("licence", out var licence)
                        && licence.ValueKind == JsonValueKind.String
                        && licence.GetString() is { Length: > 0 } text
                            ? text
                            : null));
            }
        }

        return admissions;
    }

    /// <summary>
    ///     The admission naming the package, or null when none does. NuGet names compare
    ///     case-insensitively, npm names case-sensitively.
    /// </summary>
    private static Admission? AdmissionFor(List<Admission> admissions, DirectPackage package)
    {
        foreach (var admission in admissions)
        {
            if (!admission.Ecosystem.Equals(package.Ecosystem, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var namesEqual = package.Ecosystem.Equals("nuget", StringComparison.OrdinalIgnoreCase)
                ? admission.Name.Equals(package.Name, StringComparison.OrdinalIgnoreCase)
                : admission.Name.Equals(package.Name, StringComparison.Ordinal);

            if (namesEqual)
            {
                return admission;
            }
        }

        return null;
    }

    /// <summary>The manifest pin for the package, or null when the manifest pins no such package.</summary>
    private static string? PinOf(ManifestPins pins, DirectPackage package)
    {
        if (package.Ecosystem.Equals("nuget", StringComparison.OrdinalIgnoreCase)
            && pins.NuGet.TryGetValue(package.Name, out var nuget))
        {
            return nuget;
        }

        if (package.Ecosystem.Equals("npm", StringComparison.OrdinalIgnoreCase)
            && pins.Npm.TryGetValue(package.Name, out var npm))
        {
            return npm;
        }

        return null;
    }

    /// <summary>
    ///     Whether the shipped licence is accepted: it equals the licence the admission names,
    ///     or every identifier in the SPDX expression is accepted.
    /// </summary>
    private static bool LicenceAccepted(IReadOnlySet<string> accepted, string? admitsLicence, string shipped)
    {
        if (shipped.Length == 0)
        {
            return false;
        }

        if (admitsLicence is not null && shipped.Equals(admitsLicence, StringComparison.Ordinal))
        {
            return true;
        }

        var identifiers = shipped
            .Split([' ', '\t', '\r', '\n', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !ExpressionOperators.Contains(token))
            .ToList();

        return identifiers.Count > 0 && identifiers.All(accepted.Contains);
    }

    /// <summary>Reports every admits entry missing any of its six facts.</summary>
    private static void CheckAdmissionsComplete(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Type != "decision"
                || record.Status == "retired"
                || !Checker.UnderImplementationDecisions(model.Config, record.File)
                || record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty("admits", out var admits)
                || admits.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in admits.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var missing = AdmissionFacts.Where(fact => !HasText(entry, fact)).ToList();

                if (missing.Count == 0)
                {
                    continue;
                }

                var package = entry.TryGetProperty("package", out var id)
                    && id.ValueKind == JsonValueKind.String
                    && id.GetString() is { Length: > 0 } text
                        ? $"'{text}'"
                        : "a package with no name";

                findings.Add(new Finding(
                    "admission-incomplete",
                    $"Decision '{record.Id}' admits {package} with no {string.Join(", ", missing)}.",
                    record.File,
                    record.Line,
                    record.Id.Length > 0 ? record.Id : null));
            }
        }
    }

    /// <summary>Whether the entry names the fact with a non-blank string.</summary>
    private static bool HasText(JsonElement entry, string fact) =>
        entry.TryGetProperty(fact, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString());

    /// <summary>The version as the message names it.</summary>
    private static string VersionText(string version) =>
        version.Length > 0 ? $"'{version}'" : "no version";

    /// <summary>The licence as the message names it.</summary>
    private static string LicenceText(string licence) =>
        licence.Length > 0 ? $"'{licence}'" : "no licence";
}
