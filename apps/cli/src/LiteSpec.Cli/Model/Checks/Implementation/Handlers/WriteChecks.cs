using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The write check: every repository a handler, reaction or job takes belongs to an aggregate its record declares it changes or reads.</summary>
/// <follows>litespec/backend/application#take-repositories-from-declared-changes-and-reads</follows>
/// <follows>litespec/core/use-cases#name-every-aggregate-a-use-case-changes-or-reads</follows>
internal static class WriteChecks
{
    /// <summary>Reports each repository a handler or host takes whose aggregate its record leaves undeclared.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        // Each repository interface's implementing classes are looked up once, however many holders take it.
        var marked = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Repositories is not { Count: > 0 }
                || !model.ById.TryGetValue(handler.UseCaseId, out var useCase)
                || useCase.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var declared = new HashSet<string>(useCase.Ids(RecordFields.Changes), StringComparer.Ordinal);
            declared.UnionWith(useCase.Ids(RecordFields.Reads));

            foreach (var repository in handler.Repositories)
            {
                if (!RepositoryDeclared(model, repository.Interface, declared, marked))
                {
                    findings.Add(new Finding(
                        "write-undeclared",
                        $"Use case '{handler.UseCaseId}' takes repository '{repository.Interface}', whose aggregate is outside its changes and reads.",
                        repository.File,
                        repository.Line,
                        handler.UseCaseId));
                }
            }
        }

        foreach (var host in HostRow.All(model))
        {
            if (host.Repositories is not { Count: > 0 }
                || host.RecordId is null
                || !model.ById.TryGetValue(host.RecordId, out var record)
                || record.Type is not (RecordTypes.Reaction or RecordTypes.Job))
            {
                continue;
            }

            var kind = record.Type == RecordTypes.Job ? "Job" : "Reaction";
            var declared = new HashSet<string>(record.Ids(RecordFields.Reads), StringComparer.Ordinal);

            foreach (var repository in host.Repositories)
            {
                if (!RepositoryDeclared(model, repository.Interface, declared, marked))
                {
                    findings.Add(new Finding(
                        "write-undeclared",
                        $"{kind} '{record.Id}' takes repository '{repository.Interface}', whose aggregate is outside its reads.",
                        repository.File,
                        repository.Line,
                        record.Id));
                }
            }
        }
    }

    /// <summary>
    ///     Whether the repository interface serves an aggregate the declared ids hold. The
    ///     aggregate an implementing class names in its implements mark decides; the interface
    ///     name decides only where no implementation names one.
    /// </summary>
    /// <param name="model">The loaded project model that holds every aggregate record and implementation.</param>
    /// <param name="face">The repository interface name under test.</param>
    /// <param name="declared">The aggregate identifiers the record declares in changes and reads.</param>
    /// <param name="marked">The aggregates each repository interface's implementing classes name, filled as interfaces are first seen.</param>
    /// <returns>True when the interface serves a declared aggregate or names no aggregate; otherwise false.</returns>
    private static bool RepositoryDeclared(ProjectModel model, string face, HashSet<string> declared, Dictionary<string, List<string>> marked)
    {
        if (!marked.TryGetValue(face, out var aggregates))
        {
            aggregates = MarkedRepositoryAggregates(model, face);
            marked[face] = aggregates;
        }

        if (aggregates.Count > 0)
        {
            return aggregates.Any(declared.Contains);
        }

        if (RepositoryAggregate(face) is not { } stem)
        {
            return true;
        }

        return model.RecordsByName[stem].Any(record => record.Type == RecordTypes.Aggregate && declared.Contains(record.Id));
    }

    /// <summary>The aggregates the classes implementing the repository interface name in their implements marks.</summary>
    /// <param name="model">The loaded project model that holds each implementation element and its marks.</param>
    /// <param name="face">The repository interface whose implementations are read.</param>
    /// <returns>The aggregate identifiers the implementing classes name in their implements marks.</returns>
    private static List<string> MarkedRepositoryAggregates(ProjectModel model, string face)
    {
        var aggregates = new List<string>();

        foreach (var implementation in model.Code.RepositoryImplementations.Where(entry => entry.Interface == face))
        {
            foreach (var element in model.Code.Elements.Where(element =>
                         element.Class == implementation.Class && element.File == implementation.File))
            {
                aggregates.AddRange(element.Marks
                    .Where(mark => mark.Verb == MarkVerb.Implements
                        && model.ById.TryGetValue(mark.RecordId, out var record)
                        && record.Type == RecordTypes.Aggregate)
                    .Select(mark => mark.RecordId));
            }
        }

        return aggregates;
    }

    /// <summary>The aggregate name a repository interface serves, in kebab-case, or null when it names none.</summary>
    /// <param name="face">The repository interface name, generic or plain.</param>
    /// <returns>The aggregate name in kebab-case, or null when the interface names no aggregate.</returns>
    private static string? RepositoryAggregate(string face)
    {
        var type = face;
        var generic = type.IndexOf('<');

        // A generic repository serves its first type argument.
        if (generic >= 0)
        {
            var args = type[(generic + 1)..].TrimEnd('>');
            type = args.Split(',')[0].Trim();

            var dot = type.LastIndexOf('.');

            if (dot >= 0)
            {
                type = type[(dot + 1)..];
            }

            return type.Length > 0 ? Naming.Kebab(type) : null;
        }

        if (type.StartsWith('I') && type.Length > 1 && char.IsUpper(type[1]))
        {
            type = type[1..];
        }

        if (!type.EndsWith("Repository", StringComparison.Ordinal) || type.Length <= "Repository".Length)
        {
            return null;
        }

        return Naming.Kebab(type[..^"Repository".Length]);
    }
}
