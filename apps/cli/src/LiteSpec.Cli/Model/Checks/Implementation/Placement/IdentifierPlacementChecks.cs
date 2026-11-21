using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using LiteSpec.Standards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The identifier placement check: every business identifier lives in the Contracts or Domain folder of the module owning its aggregate, the Kernel holds one only with a decision naming the cycle it breaks, and the Platform holds none.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner</follows>
/// <follows>litespec/backend/dotnet/architecture#name-no-business-identifier-in-the-platform</follows>
internal static class IdentifierPlacementChecks
{
    /// <summary>Reports every business identifier outside the folder its aggregate record names.</summary>
    /// <param name="model">The loaded project model that holds every aggregate and decision record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var files = BackendFiles(model.Config);

        if (files.Count == 0)
        {
            return;
        }

        var identifiers = DeclaredIdentifiers(files);

        if (identifiers.Count == 0)
        {
            return;
        }

        var aggregates = DeclaredAggregates(model);
        var namings = new NamingIndex(files);

        foreach (var identifier in identifiers.OrderBy(found => found.File, StringComparer.Ordinal).ThenBy(found => found.Line))
        {
            CheckIdentifier(model, identifier, aggregates, namings, findings);
        }
    }

    /// <summary>One identifier type a backend file declares: its name, file and line.</summary>
    /// <param name="Name">The simple type name as declared.</param>
    /// <param name="File">The file declaring the type.</param>
    /// <param name="Line">The 1-based line of the type name.</param>
    sealed record IdentifierType(string Name, string File, int Line);

    /// <summary>Every C# file under the backend roots with its text, in ordinal path order.</summary>
    /// <param name="config">The project configuration naming the source roots.</param>
    /// <returns>Each readable file with its text; a file that cannot be read reads as empty and is left out.</returns>
    private static List<(string File, string Text)> BackendFiles(ConsumerConfig config)
    {
        var files = new List<(string File, string Text)>();

        foreach (var root in SourceRoots.BackendRoots(config))
        {
            foreach (var file in SourceTree.Files(root, ".cs"))
            {
                try
                {
                    if (!File.Exists(file))
                    {
                        continue;
                    }

                    files.Add((file, File.ReadAllText(file)));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }
            }
        }

        files.Sort((left, right) => string.Compare(left.File, right.File, StringComparison.Ordinal));

        return files;
    }

    /// <summary>Every identifier type the backend files declare: a type named *Id implementing the typed-id marker, or a readonly record struct wrapping one Guid.</summary>
    /// <param name="files">Every backend file with its text.</param>
    /// <returns>Each identifier declaration in file order.</returns>
    private static List<IdentifierType> DeclaredIdentifiers(List<(string File, string Text)> files)
    {
        var identifiers = new List<IdentifierType>();

        foreach (var (file, text) in files)
        {
            if (GeneratedCode.IsGeneratedFileName(file)
                || (!text.Contains("ITypedId", StringComparison.Ordinal) && !text.Contains("record struct", StringComparison.Ordinal)))
            {
                continue;
            }

            var tree = CSharpSyntaxTree.ParseText(text);

            foreach (var declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (declared.Parent is TypeDeclarationSyntax
                    || declared.Identifier.IsMissing
                    || declared.Identifier.Span.Length == 0)
                {
                    continue;
                }

                var name = declared.Identifier.ValueText;

                if (!IsIdentifierShape(declared, name))
                {
                    continue;
                }

                var line = tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1;
                identifiers.Add(new IdentifierType(name, file, line));
            }
        }

        return identifiers;
    }

    /// <summary>Whether the declaration has an identifier shape: *Id on the typed-id marker, or a readonly record struct wrapping one Guid.</summary>
    /// <param name="declared">The top-level type declaration.</param>
    /// <param name="name">The simple type name as declared.</param>
    /// <returns>True when the declaration names an identifier type.</returns>
    private static bool IsIdentifierShape(TypeDeclarationSyntax declared, string name) =>
        (name.Length > 2
            && name.EndsWith("Id", StringComparison.Ordinal)
            && declared.BaseList is not null
            && declared.BaseList.Types.Any(baseType => SimpleNameOf(baseType.Type) == "ITypedId"))
        || IsGuidWrapper(declared);

    /// <summary>Whether the declaration is a readonly record struct wrapping exactly one Guid.</summary>
    /// <param name="declared">The top-level type declaration.</param>
    /// <returns>True when the declaration is a readonly record struct with one Guid payload member and no other data.</returns>
    private static bool IsGuidWrapper(TypeDeclarationSyntax declared)
    {
        if (declared is not RecordDeclarationSyntax record
            || !declared.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
            || !string.Equals(record.ClassOrStructKeyword.ValueText, "struct", StringComparison.Ordinal))
        {
            return false;
        }

        var payloads = 0;
        var others = 0;

        if (record.ParameterList is not null)
        {
            foreach (var parameter in record.ParameterList.Parameters)
            {
                if (parameter.Type is null)
                {
                    others++;
                }
                else if (SimpleNameOf(parameter.Type) == "Guid")
                {
                    payloads++;
                }
                else
                {
                    others++;
                }
            }
        }

        foreach (var property in record.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
            {
                continue;
            }

            if (SimpleNameOf(property.Type) == "Guid")
            {
                payloads++;
            }
            else
            {
                others++;
            }
        }

        foreach (var field in record.Members.OfType<FieldDeclarationSyntax>())
        {
            if (field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword) || modifier.IsKind(SyntaxKind.ConstKeyword)))
            {
                continue;
            }

            var count = field.Declaration.Variables.Count;

            if (SimpleNameOf(field.Declaration.Type) == "Guid")
            {
                payloads += count;
            }
            else
            {
                others += count;
            }
        }

        return payloads == 1 && others == 0;
    }

    /// <summary>The simple name a type use states: its rightmost identifier, without nullability.</summary>
    /// <param name="type">The type syntax as written.</param>
    /// <returns>The simple type name, or an empty string when the use states none.</returns>
    private static string SimpleNameOf(TypeSyntax type) =>
        type switch
        {
            IdentifierNameSyntax plain => plain.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            QualifiedNameSyntax qualified => SimpleNameOf(qualified.Right),
            AliasQualifiedNameSyntax alias => SimpleNameOf(alias.Name),
            NullableTypeSyntax nullable => SimpleNameOf(nullable.ElementType),
            _ => "",
        };

    /// <summary>The declared aggregates by module, aggregate folder, expected plural folder and identifier name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, folder, expected plural and name of each aggregate record placed in a module folder.</returns>
    private static List<(string Module, string Aggregate, string Expected, string Name)> DeclaredAggregates(ProjectModel model)
    {
        var aggregates = new List<(string Module, string Aggregate, string Expected, string Name)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is null || path.Aggregate is null
                || !Identifiers.TrySplit(record.Id, out _, out _, out var name)
                || name.Length == 0)
            {
                continue;
            }

            aggregates.Add((path.Module, path.Aggregate, ExpectedAggregateFolder(model, path.Module, path.Aggregate), name));
        }

        return aggregates;
    }

    /// <summary>Reports one identifier outside the folder its aggregate record names.</summary>
    /// <param name="model">The loaded project model that holds every aggregate and decision record.</param>
    /// <param name="identifier">The identifier type under test.</param>
    /// <param name="aggregates">The declared aggregates by module, folder, expected plural and name.</param>
    /// <param name="namings">The per-file type names, answering which modules name the identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIdentifier(
        ProjectModel model,
        IdentifierType identifier,
        List<(string Module, string Aggregate, string Expected, string Name)> aggregates,
        NamingIndex namings,
        List<Finding> findings)
    {
        var stem = identifier.Name.Length > 2 && identifier.Name.EndsWith("Id", StringComparison.Ordinal)
            ? identifier.Name[..^2]
            : identifier.Name;

        if (SourceRoots.IsUnder(model.Config.PlatformRoot, identifier.File))
        {
            var platformExpected = ExpectedPlace(model.Config, MatchAggregate(aggregates, null, stem), null, identifier, namings);
            var platformTail = platformExpected is null
                ? "move it to the Contracts or Domain folder of the module owning its aggregate."
                : $"it belongs in '{platformExpected}/'.";

            findings.Add(new Finding("code-placement-drift", $"Identifier '{identifier.Name}' sits in the Platform, which names no business identifier; {platformTail}", identifier.File, identifier.Line, null));
            return;
        }

        if (SourceRoots.IsUnder(model.Config.KernelRoot, identifier.File))
        {
            if (!DecisionNames(model, identifier.Name))
            {
                var kernelExpected = ExpectedPlace(model.Config, MatchAggregate(aggregates, null, stem), null, identifier, namings);
                var kernelTail = kernelExpected is null
                    ? "move it to the Contracts or Domain folder of the module owning its aggregate, or record a decision naming the Contracts cycle it breaks."
                    : $"move it to '{kernelExpected}/', or record a decision naming the Contracts cycle it breaks.";

                findings.Add(new Finding("code-placement-drift", $"Identifier '{identifier.Name}' sits in the Kernel with no decision naming it; {kernelTail}", identifier.File, identifier.Line, null));
            }

            return;
        }

        var context = ModuleContextOf(model.Config, identifier.File);
        var match = MatchAggregate(aggregates, context?.Owner, stem);

        if (match is null)
        {
            return;
        }

        var shared = namings.SharedByAnotherModule(model.Config, identifier, context?.Owner);
        var actual = ActualPlace(model.Config, identifier.File, context);
        var expected = ExpectedPlace(model.Config, match, context, identifier, namings);

        if (expected is null || string.Equals(actual, expected, StringComparison.Ordinal))
        {
            return;
        }

        // A file directly in the owner Contracts answers to the closed-folder check,
        // which names the file beside the aggregate folders.
        if (context?.InContracts == true && DirectlyInContracts(model.Config, identifier.File, context))
        {
            return;
        }

        if (shared)
        {
            findings.Add(new Finding("code-placement-drift", $"Identifier '{identifier.Name}' sits in '{actual}', but another module carries it, so it belongs in '{expected}/'.", identifier.File, identifier.Line, null));
        }
        else
        {
            findings.Add(new Finding("code-placement-drift", $"Identifier '{identifier.Name}' sits in '{actual}', but no other module carries it, so it belongs in '{expected}/'.", identifier.File, identifier.Line, null));
        }
    }

    /// <summary>Whether the file sits directly in its owner Contracts root, beside the aggregate folders.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="file">The full path of the file under test.</param>
    /// <param name="context">The module context of the file.</param>
    /// <returns>True when the file sits directly in the Contracts root.</returns>
    private static bool DirectlyInContracts(ConsumerConfig config, string file, ModuleContext context)
    {
        foreach (var root in config.ModulesRoots)
        {
            if (!SourceRoots.IsUnder(root, file))
            {
                continue;
            }

            string contracts;

            if (SourceRoots.IsSingleModulesProject(root))
            {
                contracts = Path.Combine(root, context.Owner, "Contracts");
            }
            else if (SourceRoots.IsContractsProject(root))
            {
                contracts = root;
            }
            else
            {
                continue;
            }

            return !PathText.Relative(contracts, file).Contains('/');
        }

        return false;
    }

    /// <summary>The module context of one file: its owner module, whether it sits in Contracts, and the display names of the Contracts and module trees.</summary>
    /// <param name="Owner">The owner module folder name as written on disk.</param>
    /// <param name="InContracts">Whether the file sits in the owner Contracts project or folder.</param>
    /// <param name="ContractsDisplay">The Contracts tree name as printed in an expected path.</param>
    /// <param name="ModuleDisplay">The module tree name as printed in an expected path.</param>
    sealed record ModuleContext(string Owner, bool InContracts, string ContractsDisplay, string ModuleDisplay);

    /// <summary>The module context of one file, or null when the file sits outside the module layout.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="file">The full path of the file whose context is wanted.</param>
    /// <returns>The module context, or null under the Kernel, the Platform, a host, or outside every root.</returns>
    private static ModuleContext? ModuleContextOf(ConsumerConfig config, string file)
    {
        if (SourceRoots.IsUnderKernelOrPlatform(config, file))
        {
            return null;
        }

        foreach (var root in config.ModulesRoots)
        {
            if (!SourceRoots.IsUnder(root, file))
            {
                continue;
            }

            if (SourceRoots.IsSingleModulesProject(root))
            {
                var segments = PathText.Relative(root, file).Split('/');

                if (segments.Length < 2 || segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var single = segments[0];
                var nested = segments.Length > 2 && segments[1].Equals("Contracts", StringComparison.OrdinalIgnoreCase);

                return new ModuleContext(single, nested, $"{single}/Contracts", single);
            }

            var owner = SourceRoots.ModuleNameOf(root) ?? Path.GetFileName(root);

            if (SourceRoots.IsContractsProject(root))
            {
                return new ModuleContext(owner, true, SourceRoots.FolderName(root), owner);
            }

            var sibling = config.ModulesRoots.FirstOrDefault(candidate => SourceRoots.IsContractsProject(candidate)
                && string.Equals(SourceRoots.ModuleNameOf(candidate), owner, StringComparison.OrdinalIgnoreCase));

            return new ModuleContext(
                owner,
                false,
                sibling is null ? "Contracts" : SourceRoots.FolderName(sibling),
                owner);
        }

        return null;
    }

    /// <summary>The declared aggregate the stem names: the owner's aggregate it spells, else any module's.</summary>
    /// <param name="aggregates">The declared aggregates by module, folder, expected plural and name.</param>
    /// <param name="owner">The owner module folder name as written on disk, or null outside the module layout.</param>
    /// <param name="stem">The identifier stem: its name without a trailing Id.</param>
    /// <returns>The matching aggregate, or null when no record spells the stem.</returns>
    private static (string Module, string Aggregate, string Expected, string Name)? MatchAggregate(
        List<(string Module, string Aggregate, string Expected, string Name)> aggregates, string? owner, string stem)
    {
        foreach (var aggregate in aggregates)
        {
            if (owner is not null
                && SameName(owner, aggregate.Module)
                && (SameName(stem, aggregate.Name) || SameAggregate(stem, aggregate.Aggregate)))
            {
                return aggregate;
            }
        }

        foreach (var aggregate in aggregates)
        {
            if (SameName(stem, aggregate.Name) || SameAggregate(stem, aggregate.Aggregate))
            {
                return aggregate;
            }
        }

        return null;
    }

    /// <summary>The expected tree path of the identifier: its Contracts or Domain aggregate folder.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="match">The matching aggregate, or null when no record spells the stem.</param>
    /// <param name="context">The module context of the identifier file, or null outside the module layout.</param>
    /// <param name="identifier">The identifier type under test.</param>
    /// <param name="namings">The per-file type names, answering which modules name the identifier.</param>
    /// <returns>The expected tree path without a trailing slash, or null when no record spells the stem.</returns>
    private static string? ExpectedPlace(
        ConsumerConfig config,
        (string Module, string Aggregate, string Expected, string Name)? match,
        ModuleContext? context,
        IdentifierType identifier,
        NamingIndex namings)
    {
        if (match is null)
        {
            return null;
        }

        var home = context is not null && SameName(context.Owner, match.Value.Module) ? context : null;
        var shared = namings.SharedByAnotherModule(config, identifier, context?.Owner);

        if (shared)
        {
            var contracts = home?.ContractsDisplay ?? ContractsDisplayOf(config, match.Value.Module);

            return $"{contracts}/{match.Value.Expected}";
        }

        var module = home?.ModuleDisplay ?? ModuleDisplayOf(config, match.Value.Module);

        return $"{module}/Domain/{match.Value.Expected}";
    }

    /// <summary>The Contracts tree name of one module as printed in an expected path.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="module">The module name from its aggregate record.</param>
    /// <returns>The sibling Contracts folder name, or Contracts with the module folder for the single project.</returns>
    private static string ContractsDisplayOf(ConsumerConfig config, string module)
    {
        foreach (var root in config.ModulesRoots)
        {
            if (SourceRoots.IsSingleModulesProject(root))
            {
                return $"{module}/Contracts";
            }

            if (SourceRoots.IsContractsProject(root)
                && string.Equals(SourceRoots.ModuleNameOf(root), module, StringComparison.OrdinalIgnoreCase))
            {
                return SourceRoots.FolderName(root);
            }
        }

        return "Contracts";
    }

    /// <summary>The module tree name of one module as printed in an expected path.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="module">The module name from its aggregate record.</param>
    /// <returns>The module folder name as written on disk, or the record name when no folder spells it.</returns>
    private static string ModuleDisplayOf(ConsumerConfig config, string module)
    {
        foreach (var root in config.ModulesRoots)
        {
            if (SourceRoots.IsSingleModulesProject(root))
            {
                return module;
            }

            if (!SourceRoots.IsContractsProject(root)
                && string.Equals(SourceRoots.ModuleNameOf(root), module, StringComparison.OrdinalIgnoreCase))
            {
                return SourceRoots.ModuleNameOf(root) ?? module;
            }
        }

        return module;
    }

    /// <summary>The tree path the identifier file sits in, for the finding message, in the same display as the expected path.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="file">The full path of the identifier file.</param>
    /// <param name="context">The module context of the file, or null outside the module layout.</param>
    /// <returns>The tree path as printed: the folders holding the file.</returns>
    private static string ActualPlace(ConsumerConfig config, string file, ModuleContext? context)
    {
        foreach (var root in config.ModulesRoots)
        {
            if (!SourceRoots.IsUnder(root, file))
            {
                continue;
            }

            var single = SourceRoots.IsSingleModulesProject(root);
            var tree = single && context is not null ? Path.Combine(root, context.Owner) : root;
            var head = context is not null && context.InContracts && !single ? context.ContractsDisplay : context?.Owner ?? SourceRoots.FolderName(root);
            var relative = PathText.Relative(tree, file);
            var cut = relative.LastIndexOf('/');

            if (cut < 0)
            {
                return head;
            }

            return $"{head}/{relative[..cut]}";
        }

        foreach (var root in SourceRoots.BackendRoots(config))
        {
            if (!SourceRoots.IsUnder(root, file))
            {
                continue;
            }

            var relative = PathText.Relative(root, file);
            var cut = relative.LastIndexOf('/');

            if (cut < 0)
            {
                return SourceRoots.FolderName(root);
            }

            return $"{SourceRoots.FolderName(root)}/{relative[..cut]}";
        }

        return Path.GetFileName(Path.GetDirectoryName(file) ?? file) ?? file;
    }

    /// <summary>Whether a decision record names the type in its text.</summary>
    /// <param name="model">The loaded project model that holds every decision record.</param>
    /// <param name="name">The simple type name to find.</param>
    /// <returns>True when a decision record carries the name between word boundaries.</returns>
    private static bool DecisionNames(ProjectModel model, string name)
    {
        var pattern = new Regex(@"\b" + Regex.Escape(name) + @"\b", RegexOptions.CultureInvariant);

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Decision || record.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var text in StringsOf(record.Raw))
            {
                if (pattern.IsMatch(text))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Yields each string a JSON value holds, descending into arrays and objects.</summary>
    /// <param name="value">The JSON value whose strings are collected.</param>
    /// <returns>Each string in document order; empty when the value holds no string.</returns>
    private static IEnumerable<string> StringsOf(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String && value.GetString() is { } text)
        {
            yield return text;
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in value.EnumerateArray())
            {
                foreach (var nested in StringsOf(entry))
                {
                    yield return nested;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                foreach (var nested in StringsOf(property.Value))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>The per-file type names of the backend, answering whether another module names an identifier.</summary>
    sealed class NamingIndex
    {
        /// <summary>Every backend file with its text.</summary>
        readonly List<(string File, string Text)> files;

        /// <summary>The parsed trees by file, filled as files are read.</summary>
        readonly Dictionary<string, SyntaxTree> trees = new(StringComparer.Ordinal);

        /// <summary>Initializes a new instance of the <see cref="NamingIndex" /> class over the backend files.</summary>
        /// <param name="files">Every backend file with its text.</param>
        public NamingIndex(List<(string File, string Text)> files)
        {
            this.files = files;
        }

        /// <summary>Whether code of another module names the identifier.</summary>
        /// <param name="config">The project configuration that holds the module projects.</param>
        /// <param name="identifier">The identifier type under test.</param>
        /// <param name="owner">The owner module folder name as written on disk, or null outside the module layout.</param>
        /// <returns>True when a file of another module names the identifier type.</returns>
        public bool SharedByAnotherModule(ConsumerConfig config, IdentifierType identifier, string? owner)
        {
            foreach (var (file, text) in files)
            {
                if (string.Equals(file, identifier.File, StringComparison.Ordinal)
                    || !text.Contains(identifier.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (FileModuleOf(config, file) is not { Length: > 0 } module
                    || (owner is not null && string.Equals(module, owner, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (!trees.TryGetValue(file, out var tree))
                {
                    tree = CSharpSyntaxTree.ParseText(text);
                    trees[file] = tree;
                }

                if (tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Any(use => use.Identifier.ValueText == identifier.Name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The module holding one file, or null under the Kernel, the Platform, a host, or outside every root.</summary>
        /// <param name="config">The project configuration that holds the module projects.</param>
        /// <param name="file">The full path of the file whose module is wanted.</param>
        /// <returns>The module folder name as written on disk, or null outside the module layout.</returns>
        private static string? FileModuleOf(ConsumerConfig config, string file)
        {
            if (SourceRoots.IsUnderKernelOrPlatform(config, file))
            {
                return null;
            }

            foreach (var root in config.ModulesRoots)
            {
                if (!SourceRoots.IsUnder(root, file))
                {
                    continue;
                }

                if (SourceRoots.IsSingleModulesProject(root))
                {
                    var segments = PathText.Relative(root, file).Split('/');

                    if (segments.Length < 2 || segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    return segments[0];
                }

                return SourceRoots.ModuleNameOf(root);
            }

            return null;
        }
    }
}
