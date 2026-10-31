using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     Reads every recognized element of one file into the model: the element itself
///     with its marks, and the handler, reaction, job, event or endpoint row its kind owns.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class ElementReader
{
    /// <summary>
    ///     Reads every recognized element of one file into the model. Every record
    ///     link comes from a mark the element carries.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="elements">The marked elements of the file.</param>
    /// <param name="types">The type declarations by identifier span.</param>
    /// <param name="methods">The method declarations by identifier span.</param>
    /// <param name="model">The code model an element adds to.</param>
    /// <param name="helpers">The helper index of this read.</param>
    /// <param name="aggregateRoots">The simple names of known aggregate roots.</param>
    /// <param name="holders">The classes whose constructor takes a repository.</param>
    /// <param name="qualified">The namespace-qualified names of this read.</param>
    public static void ReadElements(
        string file,
        SyntaxTree tree,
        IReadOnlyList<MarkableElement> elements,
        IReadOnlyDictionary<TextSpan, TypeDeclarationSyntax> types,
        IReadOnlyDictionary<TextSpan, MethodDeclarationSyntax> methods,
        CodeModel model,
        Dictionary<string, List<HelperMethod>> helpers,
        HashSet<string> aggregateRoots,
        HashSet<string> holders,
        QualifiedKinds qualified)
    {
        foreach (var element in elements)
        {
            if (element.Kind is not CodeKind kind)
            {
                ReadStrayMark(file, element, methods, model);
                continue;
            }

            if (kind is CodeKind.Test or CodeKind.FrontendModule)
            {
                continue;
            }

            var marks = element.Marks
                .Select(mark => new CodeMark(mark.RecordId, mark.Style, mark.Verb, file, mark.Line))
                .ToList();

            if (element.IsType)
            {
                if (!types.TryGetValue(element.Span, out var declared))
                {
                    continue;
                }

                model.Elements.Add(new MarkedElement(
                    kind, element.Name, null, file, element.Line, marks, false,
                    RuntimeValueOf(declared, kind)));

                switch (kind)
                {
                    case CodeKind.Handler:
                        HandlerReader.ReadHandler(file, tree, declared, marks, model, helpers, aggregateRoots, holders, qualified);
                        break;
                    case CodeKind.EventHandler:
                        HandlerReader.ReadReaction(file, tree, declared, marks, model, helpers, holders, qualified);
                        break;
                    case CodeKind.Job:
                        HandlerReader.ReadJob(file, tree, declared, marks, model, helpers, holders, qualified);
                        break;
                    case CodeKind.DomainEvent:
                        model.Events.Add(new DomainEventInfo(
                            element.Name,
                            file,
                            tree.GetLineSpan(declared.Span).StartLinePosition.Line + 1,
                            SingleMark(marks, "event/")));
                        break;
                    case CodeKind.Endpoint:
                        EndpointReader.ReadEndpoint(file, tree, declared, marks, model);
                        break;
                }
            }
            else
            {
                if (!methods.TryGetValue(element.Span, out var method))
                {
                    continue;
                }

                var containing = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

                model.Elements.Add(new MarkedElement(
                    kind, containing?.Identifier.ValueText ?? "", element.Name,
                    file, element.Line, marks, false));
            }
        }
    }

    /// <summary>
    ///     Reads a marked element of no known kind into the stray marks: a reading
    ///     method, a private helper, or an unrecognized class carrying a mark.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="element">The marked element of no known kind.</param>
    /// <param name="methods">The method declarations by identifier span.</param>
    /// <param name="model">The code model a stray mark adds to.</param>
    public static void ReadStrayMark(
        string file,
        MarkableElement element,
        IReadOnlyDictionary<TextSpan, MethodDeclarationSyntax> methods,
        CodeModel model)
    {
        var marks = element.Marks
            .Select(mark => new CodeMark(mark.RecordId, mark.Style, mark.Verb, file, mark.Line))
            .ToList();

        if (element.IsType)
        {
            model.StrayMarks.Add(new StrayMark(element.Name, null, file, element.Line, marks));
            return;
        }

        if (!methods.TryGetValue(element.Span, out var method))
        {
            return;
        }

        var containing = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

        model.StrayMarks.Add(new StrayMark(
            containing?.Identifier.ValueText ?? "", element.Name, file, element.Line, marks));
    }

    /// <summary>Whether the type is class-like, as the shared kind reader decides: a class or a non-struct record.</summary>
    /// <param name="type">The type declaration.</param>
    /// <returns>True when the type is class-like.</returns>
    public static bool IsClassLike(TypeDeclarationSyntax type) =>
        type switch
        {
            ClassDeclarationSyntax => true,
            RecordDeclarationSyntax record => !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
            _ => false,
        };

    /// <summary>Whether the name is a job name, as the shared kind reader decides.</summary>
    /// <param name="name">The type name.</param>
    /// <returns>True when the name is a job name.</returns>
    public static bool HasJobName(string name) =>
        name.EndsWith("Job", StringComparison.Ordinal) || name.EndsWith("Worker", StringComparison.Ordinal);

    /// <summary>The one mark naming the prefix, or null when the element carries none or several.</summary>
    /// <param name="marks">The marks the element carries.</param>
    /// <param name="prefix">The mark prefix.</param>
    /// <returns>The one mark naming the prefix, or null when the element carries none or several.</returns>
    public static string? SingleMark(IReadOnlyList<CodeMark> marks, string prefix)
    {
        string? found = null;

        foreach (var mark in marks)
        {
            if (mark.RecordId.StartsWith(prefix, StringComparison.Ordinal))
            {
                if (found is not null)
                {
                    return null;
                }

                found = mark.RecordId;
            }
        }

        return found;
    }

    /// <summary>
    ///     The hand-written runtime value the type declares: an event's Classification
    ///     literal or a rule exception's Code literal. Null when the type declares none.
    /// </summary>
    /// <param name="declared">The type declaration.</param>
    /// <param name="kind">The code kind of the type.</param>
    /// <returns>The hand-written runtime value the type declares, or null when it declares none.</returns>
    public static string? RuntimeValueOf(TypeDeclarationSyntax declared, CodeKind kind)
    {
        var wanted = kind switch
        {
            CodeKind.DomainEvent => "Classification",
            CodeKind.RuleException => "Code",
            _ => null,
        };

        if (wanted is null)
        {
            return null;
        }

        foreach (var member in declared.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (variable.Identifier.ValueText == wanted
                            && ValueOf(variable.Initializer?.Value) is { } fieldValue)
                        {
                            return fieldValue;
                        }
                    }

                    break;
                case PropertyDeclarationSyntax property when property.Identifier.ValueText == wanted:
                    if (ValueOf(property.ExpressionBody?.Expression) is { } body)
                    {
                        return body;
                    }

                    if (ValueOf(property.Initializer?.Value) is { } initial)
                    {
                        return initial;
                    }

                    if (property.AccessorList is { } accessors)
                    {
                        foreach (var accessor in accessors.Accessors)
                        {
                            if (ValueOf(accessor.ExpressionBody?.Expression) is { } held)
                            {
                                return held;
                            }
                        }
                    }

                    break;
            }
        }

        return null;

        static string? ValueOf(ExpressionSyntax? expression) =>
            expression is LiteralExpressionSyntax literal && literal.Token.Value is string text
                ? text
                : null;
    }
}
