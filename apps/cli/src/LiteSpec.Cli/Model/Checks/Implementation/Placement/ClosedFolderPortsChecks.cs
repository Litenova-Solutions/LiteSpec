using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the ports: each module Ports folder with one folder per port, each port folder with its interface and models, and the declared types each port folder names.</summary>
internal static partial class ClosedFolderChecks
{
    /// <summary>
    ///     Reports every file directly in a module Ports folder, which holds only one folder
    ///     per port, every port folder with no adapter folder of the same name, every port
    ///     folder naming a provider module, and every port folder spelling its external-system
    ///     record under another name, and checks each port folder for its interface and named types.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="ports">The absolute Ports folder of one module.</param>
    /// <param name="adapterNames">The adapter folder names under the module Infrastructure folder, without Persistence.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortsLevel(ConsumerConfig config, string ports, IReadOnlyList<string> adapterNames, List<string> modules, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, List<Finding> findings)
    {
        foreach (var child in ChildFolders(ports))
        {
            var name = Path.GetFileName(child);

            if (modules.Count > 0 && modules.FirstOrDefault(declared => SameName(name, declared)) is { } provider)
            {
                findings.Add(new Finding("code-placement-drift", $"Port folder '{PathText.Relative(config.Root, child)}' names the module '{provider}'; a module reaches another module through its Contracts, not a port.", child, 1, null));
            }

            CheckPortRecordName(config, child, name, portMarks, externalNames, findings);
            CheckPortFolder(config, child, findings);

            if (!adapterNames.Any(adapter => SameName(name, adapter)))
            {
                findings.Add(new Finding("code-placement-drift", $"Port folder '{PathText.Relative(config.Root, child)}' names no adapter folder; its adapter sits in 'Infrastructure/{name}/'.", child, 1, null));
            }
        }

        foreach (var file in ChildFiles(ports))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Ports/, which holds only one folder per port.", file, 1, null));
        }
    }

    /// <summary>Reports the port folder whose interface marks an external-system record the folder spells under another name: the folder spells the record name in PascalCase. An in-process port carries no mark and keeps the name of its capability.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <param name="name">The port folder name as written on disk.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPortRecordName(ConsumerConfig config, string port, string name, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, List<Finding> findings)
    {
        var marked = portMarks
            .Where(mark => SourceRoots.IsUnder(port, mark.File))
            .Select(mark => mark.RecordId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (marked.Count == 0)
        {
            return;
        }

        foreach (var id in marked)
        {
            if (externalNames.TryGetValue(id, out var record) && SameName(name, record))
            {
                return;
            }
        }

        if (!externalNames.TryGetValue(marked[0], out var first))
        {
            return;
        }

        findings.Add(new Finding("code-placement-drift", $"Port folder '{PathText.Relative(config.Root, port)}' carries the port for '{marked[0]}', so it belongs in 'Ports/{Naming.Pascal(first)}/'.", port, 1, null));
    }

    /// <summary>
    ///     Reports the shape of one port folder: a subfolder outside Models/, no interface or more than
    ///     one, an interface whose name is not the folder name with an I prefix, a type beside the
    ///     interface instead of Models/, and a Models/ type no member signature of the interface names,
    ///     directly or through one named type. The top keeps the interface; every type its signatures
    ///     name sits in Models/.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortFolder(ConsumerConfig config, string port, List<Finding> findings)
    {
        foreach (var child in ChildFolders(port))
        {
            if (string.Equals(Path.GetFileName(child), "Models", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in port '{Path.GetFileName(port)}', which holds only its Models/ grouping.", child, 1, null));
        }

        var name = Path.GetFileName(port);
        var relative = PathText.Relative(config.Root, port);
        var types = PortTypesOf(port);
        var faces = types.Where(type => type.IsInterface && IsDirectChild(port, type.File)).OrderBy(type => type.Name, StringComparer.Ordinal).ToList();

        if (faces.Count == 0)
        {
            findings.Add(new Finding("code-placement-drift", $"Port folder '{relative}' holds no interface; it holds the interface 'I{name}' with the types its signatures name in Models/.", port, 1, null));
            return;
        }

        if (faces.Count > 1)
        {
            var held = string.Join(", ", faces.Select(face => $"'{face.Name}'"));
            findings.Add(new Finding("code-placement-drift", $"Port folder '{relative}' holds {faces.Count} interfaces ({held}); it holds only the interface 'I{name}' with the types its signatures name in Models/.", port, 1, null));
            return;
        }

        var face = faces[0];

        if (!string.Equals(face.Name, "I" + name, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding("code-placement-drift", $"Interface '{face.Name}' sits in port folder '{relative}', whose interface is 'I{name}'.", face.File, face.Line, null));
        }

        // A named type keeps its own parts: the check follows the interface signatures one level
        // into the types they name, so a result part the interface names only through its result stays.
        // Only the interface's own names open the second level; names found there open no third.
        var direct = new HashSet<string>(face.SignatureNames, StringComparer.Ordinal);
        var named = new HashSet<string>(direct, StringComparer.Ordinal);

        foreach (var type in types)
        {
            if (!type.IsInterface && direct.Contains(type.Name))
            {
                named.UnionWith(type.SignatureNames);
            }
        }

        foreach (var type in types.OrderBy(candidate => candidate.File, StringComparer.Ordinal).ThenBy(candidate => candidate.Line))
        {
            if (string.Equals(type.Name, face.Name, StringComparison.Ordinal) && string.Equals(type.File, face.File, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsDirectChild(port, type.File))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in port folder '{relative}' sits beside the interface '{face.Name}'; it belongs in 'Models/'.", type.File, type.Line, null));
            }
            else if (!named.Contains(type.Name))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in port folder '{relative}' is named by no signature of '{face.Name}'; move it to Domain/{{Aggregates}}/ or the use case folder that uses it.", type.File, type.Line, null));
            }
        }
    }

    /// <summary>Whether the file sits directly in the folder rather than in one of its groupings.</summary>
    /// <param name="folder">The absolute folder.</param>
    /// <param name="file">The absolute file path.</param>
    /// <returns>True when the file's folder is the folder itself.</returns>
    private static bool IsDirectChild(string folder, string file) =>
        string.Equals(Path.GetDirectoryName(file), folder, StringComparison.OrdinalIgnoreCase);

    /// <summary>One top-level type declared in a port or adapter folder: its name, file, line, whether it is an interface, the simple type names its member signatures name, and the simple names its base list states.</summary>
    /// <param name="Name">The simple type name as declared.</param>
    /// <param name="File">The file declaring the type.</param>
    /// <param name="Line">The 1-based line of the type name.</param>
    /// <param name="IsInterface">Whether the declaration is an interface.</param>
    /// <param name="SignatureNames">The simple type names its members name: parameter and return types with their type arguments, then property, field and event types.</param>
    /// <param name="BaseNames">The simple type names its base list states: base classes and implemented interfaces with their type arguments.</param>
    sealed record PortDeclaredType(string Name, string File, int Line, bool IsInterface, HashSet<string> SignatureNames, HashSet<string> BaseNames);

    /// <summary>The top-level types each C# file in a port folder declares, in file order, with the simple type names each member signature names: the interface beside its Models/ grouping.</summary>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> PortTypesOf(string port)
    {
        var files = ChildFiles(port);
        var models = ChildFolders(port).FirstOrDefault(child => string.Equals(Path.GetFileName(child), "Models", StringComparison.OrdinalIgnoreCase));

        if (models is not null)
        {
            files.AddRange(ChildFiles(models));
        }

        return DeclaredTypesOf(files);
    }

    /// <summary>The top-level types each C# file in an adapter folder declares, in file order: the adapter and options files beside the Models/ and Parts/ groupings.</summary>
    /// <param name="adapter">The absolute adapter folder under Infrastructure.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> AdapterTypesOf(string adapter)
    {
        var files = ChildFiles(adapter);

        foreach (var child in ChildFolders(adapter))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "Models", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Parts", StringComparison.OrdinalIgnoreCase))
            {
                files.AddRange(ChildFiles(child));
            }
        }

        return DeclaredTypesOf(files);
    }

    /// <summary>The top-level types the C# files declare, in file order, with the simple type names each member signature names.</summary>
    /// <param name="files">The absolute files to read.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> DeclaredTypesOf(List<string> files)
    {
        var types = new List<PortDeclaredType>();

        foreach (var file in files)
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SyntaxTree tree;

            try
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var root = tree.GetRoot();

            foreach (var declared in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (declared is not TypeDeclarationSyntax and not EnumDeclarationSyntax and not DelegateDeclarationSyntax)
                {
                    continue;
                }

                if (declared.Parent is TypeDeclarationSyntax)
                {
                    continue;
                }

                // A C# 14 extension block is a type declaration with no name, so it names no type.
                if (declared is TypeDeclarationSyntax extension
                    && (extension.Identifier.IsMissing || extension.Identifier.Span.Length == 0))
                {
                    continue;
                }

                var names = new HashSet<string>(StringComparer.Ordinal);
                var bases = new HashSet<string>(StringComparer.Ordinal);

                if (declared is TypeDeclarationSyntax type)
                {
                    if (type.BaseList is not null)
                    {
                        foreach (var baseType in type.BaseList.Types)
                        {
                            AddSignatureNames(baseType.Type, bases);
                        }
                    }

                    if (type.ParameterList is not null)
                    {
                        foreach (var parameter in type.ParameterList.Parameters)
                        {
                            AddSignatureNames(parameter.Type, names);
                        }
                    }

                    foreach (var member in type.Members)
                    {
                        AddMemberNames(member, names);
                    }
                }
                else if (declared is DelegateDeclarationSyntax delegateType)
                {
                    AddSignatureNames(delegateType.ReturnType, names);

                    foreach (var parameter in delegateType.ParameterList.Parameters)
                    {
                        AddSignatureNames(parameter.Type, names);
                    }
                }

                var identifier = declared switch
                {
                    TypeDeclarationSyntax named => named.Identifier,
                    EnumDeclarationSyntax named => named.Identifier,
                    DelegateDeclarationSyntax named => named.Identifier,
                    _ => default,
                };
                var line = tree.GetLineSpan(identifier.Span).StartLinePosition.Line + 1;
                types.Add(new PortDeclaredType(identifier.ValueText, file, line, declared is InterfaceDeclarationSyntax, names, bases));
            }
        }

        return types;
    }

    /// <summary>Adds the simple type names one member signature names: a method's return and parameter types, a property, field or event type, or an indexer's type and parameter types.</summary>
    /// <param name="member">The member whose signature is read.</param>
    /// <param name="names">The names collected so far.</param>
    private static void AddMemberNames(MemberDeclarationSyntax member, HashSet<string> names)
    {
        switch (member)
        {
            case MethodDeclarationSyntax method:
                AddSignatureNames(method.ReturnType, names);

                foreach (var parameter in method.ParameterList.Parameters)
                {
                    AddSignatureNames(parameter.Type, names);
                }

                break;
            case PropertyDeclarationSyntax property:
                AddSignatureNames(property.Type, names);
                break;
            case FieldDeclarationSyntax field:
                AddSignatureNames(field.Declaration.Type, names);
                break;
            case EventFieldDeclarationSyntax fieldEvent:
                AddSignatureNames(fieldEvent.Declaration.Type, names);
                break;
            case EventDeclarationSyntax declaredEvent:
                AddSignatureNames(declaredEvent.Type, names);
                break;
            case IndexerDeclarationSyntax indexer:
                AddSignatureNames(indexer.Type, names);

                foreach (var parameter in indexer.ParameterList.Parameters)
                {
                    AddSignatureNames(parameter.Type, names);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>Adds the simple names one type use states: its own name with each nested type argument name, unqualified and without nullability or array ranks.</summary>
    /// <param name="type">The type syntax as written, or null when the signature states none.</param>
    /// <param name="names">The names collected so far.</param>
    private static void AddSignatureNames(TypeSyntax? type, HashSet<string> names)
    {
        switch (type)
        {
            case null:
                break;
            case IdentifierNameSyntax plain:
                names.Add(plain.Identifier.ValueText);
                break;
            case GenericNameSyntax generic:
                names.Add(generic.Identifier.ValueText);

                foreach (var argument in generic.TypeArgumentList.Arguments)
                {
                    AddSignatureNames(argument, names);
                }

                break;
            case QualifiedNameSyntax qualified:
                AddSignatureNames(qualified.Right, names);
                break;
            case AliasQualifiedNameSyntax alias:
                AddSignatureNames(alias.Name, names);
                break;
            case NullableTypeSyntax nullable:
                AddSignatureNames(nullable.ElementType, names);
                break;
            case ArrayTypeSyntax array:
                AddSignatureNames(array.ElementType, names);
                break;
            case TupleTypeSyntax tuple:
                foreach (var element in tuple.Elements)
                {
                    AddSignatureNames(element.Type, names);
                }

                break;
            case PointerTypeSyntax pointer:
                AddSignatureNames(pointer.ElementType, names);
                break;
            case FunctionPointerTypeSyntax pointer:
                // The last function pointer parameter states the return type.
                foreach (var parameter in pointer.ParameterList.Parameters)
                {
                    AddSignatureNames(parameter.Type, names);
                }

                break;
            default:
                break;
        }
    }
}
