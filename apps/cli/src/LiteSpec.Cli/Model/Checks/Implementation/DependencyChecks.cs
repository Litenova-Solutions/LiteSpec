using System.Text.Json;
using System.Xml.Linq;
using LiteSpec.Cli.Model.Readers.Dependencies;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The dependency checks, kept in one class so the partial checker holds only the entry point.
/// </summary>
/// <follows>litespec/workspace/dependencies#approve-new-packages-explicitly</follows>
/// <follows>litespec/ext/delivery#match-every-shipped-package-to-its-pin</follows>
/// <follows>litespec/ext/delivery#accept-every-shipped-package-license-explicitly</follows>
internal static class DependencyChecks
{
    /// <summary>The four facts every admits entry names.</summary>
    private static readonly string[] AdmissionFacts =
    [
        "layer", "license", "location", "failureBehavior",
    ];

    /// <summary>SPDX expression operators: every other token is a license identifier.</summary>
    private static readonly HashSet<string> ExpressionOperators = new(StringComparer.Ordinal)
    {
        "AND", "OR", "WITH",
    };

    /// <summary>
    ///     Runs admission-invalid, which always runs, and the three bill checks, which stay
    ///     silent when the project names no bill of materials in paths.sbom.
    /// </summary>
    /// <param name="model">The loaded project model that holds every decision record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckAdmissionsComplete(model, findings);
        CheckKnowledgeReferences(model, findings);

        if (model.Config.Sboms.Count == 0)
        {
            return;
        }

        var sbom = SbomReader.Read(model.Config);

        findings.AddRange(sbom.Problems);

        var packages = sbom.Packages;

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
                    "dependency-undeclared",
                    $"Direct package '{package.Id}' version {VersionText(package.Version)} with license {LicenseText(package.License)} is pinned by no manifest entry and admitted by no decision.",
                    package.File,
                    1,
                    null));
            }

            if (pin is not null && !SatisfiesPin(pin, package.Version))
            {
                var relation = pin.EndsWith(".x", StringComparison.Ordinal)
                    ? "is outside its manifest pin"
                    : "differs from its manifest pin";

                findings.Add(new Finding(
                    "dependency-version-drift",
                    $"Direct package '{package.Id}' version {VersionText(package.Version)} with license {LicenseText(package.License)} {relation} '{pin}'.",
                    package.File,
                    1,
                    null));
            }

            if (!LicenseAccepted(model.Config.AcceptedLicenses, admitted?.License, package.License))
            {
                findings.Add(new Finding(
                    "dependency-license-unknown",
                    $"Direct package '{package.Id}' version {VersionText(package.Version)} carries license {LicenseText(package.License)}, which the project does not accept.",
                    package.File,
                    1,
                    null));
            }
        }
    }


    /// <summary>
    ///     Reports every project reference that crosses the knowledge boundary: a module project referencing a
    ///     knowledge set, a knowledge set referencing a project other than its model and the Kernel, and a
    ///     knowledge model referencing a module. A workspace with no Knowledge folder stays silent.
    /// </summary>
    /// <param name="model">The loaded project model that holds the module and knowledge roots.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts</follows>
    private static void CheckKnowledgeReferences(ProjectModel model, List<Finding> findings)
    {
        var knowledge = SourceRoots.KnowledgeRoot(model.Config);

        if (knowledge.Length == 0)
        {
            return;
        }

        var children = KnowledgeChildren(knowledge);
        var models = children.Where(SourceRoots.IsKnowledgeModel).Order(StringComparer.Ordinal).ToList();
        var names = models.Select(SourceRoots.FolderName).ToHashSet(StringComparer.Ordinal);
        var sets = children
            .Where(child => SourceRoots.KnowledgeModelName(child) is { } own && names.Contains(own))
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var modules in model.Config.ModulesRoots.Order(StringComparer.Ordinal))
        {
            foreach (var (project, target) in ProjectReferences(modules))
            {
                if (sets.Any(set => SameDir(set, target)))
                {
                    findings.Add(new Finding(
                        "knowledge-reference-invalid",
                        $"Project '{SourceRoots.FolderName(modules)}' references the knowledge set '{SourceRoots.FolderName(target)}'; a module reaches a knowledge area through its model.",
                        project,
                        1,
                        null));
                }
            }
        }

        foreach (var set in sets)
        {
            var own = Path.Combine(knowledge, SourceRoots.KnowledgeModelName(set)!);

            foreach (var (project, target) in ProjectReferences(set))
            {
                if (!SameDir(own, target) && !SameDir(model.Config.KernelRoot, target))
                {
                    findings.Add(new Finding(
                        "knowledge-reference-invalid",
                        $"Knowledge set '{SourceRoots.FolderName(set)}' references '{SourceRoots.FolderName(target)}'; a set references only its model and the Kernel.",
                        project,
                        1,
                        null));
                }
            }
        }

        foreach (var area in models)
        {
            foreach (var (project, target) in ProjectReferences(area))
            {
                if (IsModuleProject(model, target))
                {
                    findings.Add(new Finding(
                        "knowledge-reference-invalid",
                        $"Knowledge model '{SourceRoots.FolderName(area)}' references the module project '{SourceRoots.FolderName(target)}'; a model references no module.",
                        project,
                        1,
                        null));
                }
            }
        }
    }

    /// <summary>The child folders of the Knowledge root in ordinal order, or an empty list when the folder cannot be read.</summary>
    /// <param name="knowledge">The absolute Knowledge folder.</param>
    /// <returns>The child folders in ordinal order.</returns>
    private static List<string> KnowledgeChildren(string knowledge)
    {
        try
        {
            return Directory.EnumerateDirectories(knowledge)
                .OrderBy(child => child, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Whether the referenced folder is a module project of the workspace.</summary>
    /// <param name="model">The loaded project model that holds the module roots.</param>
    /// <param name="target">The referenced project folder.</param>
    /// <returns>True when the folder is one of the module roots.</returns>
    private static bool IsModuleProject(ProjectModel model, string target) =>
        model.Config.ModulesRoots.Any(root => SameDir(root, target));

    /// <summary>Whether two folders name one folder: compared without case and trailing separators.</summary>
    /// <param name="left">The first folder.</param>
    /// <param name="right">The second folder.</param>
    /// <returns>True when both name one folder.</returns>
    private static bool SameDir(string left, string right) =>
        left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    /// <summary>Every project reference of the project: the referencing project file with the referenced project folder.</summary>
    /// <param name="project">The project folder holding the project file.</param>
    /// <returns>Each referenced project folder with its project file, in project file order.</returns>
    private static List<(string Project, string Target)> ProjectReferences(string project)
    {
        var references = new List<(string Project, string Target)>();
        List<string> files;

        try
        {
            if (!Directory.Exists(project))
            {
                return references;
            }

            files = Directory.EnumerateFiles(project, "*.csproj", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return references;
        }

        foreach (var file in files)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(file);
            }
            catch (Exception exception) when (exception is System.Xml.XmlException or IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var folder = Path.GetDirectoryName(file) ?? file;

            foreach (var element in document.Descendants())
            {
                if (!string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                {
                    continue;
                }

                var include = element.Attribute("Include")?.Value;

                if (include is not { Length: > 0 })
                {
                    continue;
                }

                try
                {
                    var target = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(folder, include.Replace('\\', Path.DirectorySeparatorChar))));

                    if (target is { Length: > 0 })
                    {
                        references.Add((file, target));
                    }
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    continue;
                }
            }
        }

        return references;
    }

    /// <summary>
    ///     Every admission: the admits entries of the technical decisions.
    /// </summary>
    /// <param name="model">The loaded project model that holds every technical decision record.</param>
    /// <returns>Each admitted package with its ecosystem, name and admitted license where one is named.</returns>
    private static List<Admission> AdmissionsOf(ProjectModel model)
    {
        var admissions = new List<Admission>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Decision
                || !MarkChecks.UnderImplementationDecisions(model.Config, record.File)
                || record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty(RecordFields.Admits, out var admits)
                || admits.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in admits.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || !entry.TryGetProperty(RecordFields.Package, out var package)
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
                    entry.TryGetProperty(RecordFields.License, out var license)
                        && license.ValueKind == JsonValueKind.String
                        && license.GetString() is { Length: > 0 } text
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
    /// <param name="admissions">The admitted packages read from the technical decisions.</param>
    /// <param name="package">The shipped direct package whose admission is wanted.</param>
    /// <returns>The admission naming the package, or null when none does.</returns>
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
    /// <param name="pins">The manifest pins by ecosystem and package name.</param>
    /// <param name="package">The shipped direct package whose pin is wanted.</param>
    /// <returns>The pinned version text, or null when the manifest pins no such package.</returns>
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
    ///     Whether a resolved version satisfies a manifest pin: an exact pin needs
    ///     ordinal equality, and a major pin needs the same major with no prerelease
    ///     suffix, because the baseline names stable releases.
    /// </summary>
    /// <param name="pin">The manifest pin, either an exact version or a major stated as N.x.</param>
    /// <param name="version">The resolved package version text, empty when the bill of materials names none.</param>
    /// <returns>True when the version is inside the pin; otherwise false.</returns>
    private static bool SatisfiesPin(string pin, string version)
    {
        if (!pin.EndsWith(".x", StringComparison.Ordinal)
            || !int.TryParse(pin[..^2], out var pinnedMajor))
        {
            return pin.Equals(version, StringComparison.Ordinal);
        }

        if (version.Length == 0 || version.Contains('-'))
        {
            return false;
        }

        var dot = version.IndexOf('.');
        var majorText = dot < 0 ? version : version[..dot];

        return int.TryParse(majorText, out var major) && major == pinnedMajor;
    }

    /// <summary>
    ///     Whether the shipped license is accepted: it equals the license the admission names,
    ///     or every identifier in the SPDX expression is accepted.
    /// </summary>
    /// <param name="accepted">The license identifiers the project accepts explicitly.</param>
    /// <param name="admitsLicense">The license the admission names, or null when it names none.</param>
    /// <param name="shipped">The license expression the shipped package carries.</param>
    /// <returns>True when the shipped license equals the admitted license or each identifier in its SPDX expression is accepted; otherwise false.</returns>
    private static bool LicenseAccepted(IReadOnlySet<string> accepted, string? admitsLicense, string shipped)
    {
        if (shipped.Length == 0)
        {
            return false;
        }

        if (admitsLicense is not null && shipped.Equals(admitsLicense, StringComparison.Ordinal))
        {
            return true;
        }

        var identifiers = shipped
            .Split([' ', '\t', '\r', '\n', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !ExpressionOperators.Contains(token))
            .ToList();

        return identifiers.Count > 0 && identifiers.All(accepted.Contains);
    }

    /// <summary>Reports every admits entry missing any of its four facts.</summary>
    /// <param name="model">The loaded project model that holds every technical decision record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAdmissionsComplete(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Decision
                || !MarkChecks.UnderImplementationDecisions(model.Config, record.File)
                || record.Raw.ValueKind != JsonValueKind.Object
                || !record.Raw.TryGetProperty(RecordFields.Admits, out var admits)
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

                var package = entry.TryGetProperty(RecordFields.Package, out var id)
                    && id.ValueKind == JsonValueKind.String
                    && id.GetString() is { Length: > 0 } text
                        ? $"'{text}'"
                        : "a package with no name";

                findings.Add(new Finding(
                    "admission-invalid",
                    $"Decision '{record.Id}' admits {package} with no {string.Join(", ", missing)}.",
                    record.File,
                    record.Line,
                    record.Id.Length > 0 ? record.Id : null));
            }
        }
    }

    /// <summary>Whether the entry names the fact with a non-blank string.</summary>
    /// <param name="entry">The admits entry under inspection.</param>
    /// <param name="fact">The fact name whose presence is tested.</param>
    /// <returns>True when the entry names the fact with a non-blank string; otherwise false.</returns>
    private static bool HasText(JsonElement entry, string fact) =>
        entry.TryGetProperty(fact, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString());

    /// <summary>The version as the message names it.</summary>
    /// <param name="version">The package version text, empty when the bill of materials names none.</param>
    /// <returns>The version in quotes, or the words no version when empty.</returns>
    private static string VersionText(string version) =>
        version.Length > 0 ? $"'{version}'" : "no version";

    /// <summary>The license as the message names it.</summary>
    /// <param name="license">The package license text, empty when the bill of materials names none.</param>
    /// <returns>The license in quotes, or the words no license when empty.</returns>
    private static string LicenseText(string license) =>
        license.Length > 0 ? $"'{license}'" : "no license";
}
