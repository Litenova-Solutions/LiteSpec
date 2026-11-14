using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The access checks: each role scope has a glossary term that states its reach, and each permission name opens with a use-case verb.</summary>
/// <follows>litespec/core/access#define-each-scope-level-in-the-glossary</follows>
/// <follows>litespec/core/access#name-each-permission-by-a-use-case-verb</follows>
internal static class AccessChecks
{
    /// <summary>The prefix every permission identifier carries before its name.</summary>
    private const string PermissionPrefix = "permission/";

    /// <summary>Runs every access check.</summary>
    /// <param name="model">The loaded project model that holds every record and the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckScopeTerms(model, findings);
        CheckPermissionVerbs(model, findings);
    }

    /// <summary>Reports each role whose scope has no glossary term titled with the level and Scope.</summary>
    /// <param name="model">The loaded project model that holds every record and the glossary terms.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckScopeTerms(ProjectModel model, List<Finding> findings)
    {
        var titles = model.Records
            .Where(record => record.Type == RecordTypes.Term)
            .Select(record => record.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var role in model.Records.Where(record => record.Type == RecordTypes.Role && !record.Derived))
        {
            var scope = role.Field(RecordFields.Scope);

            if (string.IsNullOrEmpty(scope))
            {
                continue;
            }

            var title = ScopeTitle(scope);

            if (!titles.Contains(title))
            {
                findings.Add(new Finding("scope-term-missing", $"Role '{role.Id}' names scope '{scope}', and no glossary term is titled '{title}'.", role.File, role.Line, SubjectOf(role)));
            }
        }
    }

    /// <summary>Reports each permission whose name opens with no use-case verb and not with manage.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPermissionVerbs(ProjectModel model, List<Finding> findings)
    {
        var verbs = UseCaseVerbChecks.GlossaryVerbs(model);

        foreach (var permission in model.Records.Where(record => record.Type == RecordTypes.Permission && !record.Derived))
        {
            var name = NameOf(permission.Id);

            if (name is null)
            {
                continue;
            }

            var first = name.Split('-')[0];

            if (first.Length == 0 || first == UseCaseVerbs.Manage)
            {
                continue;
            }

            var verb = UseCaseVerbChecks.DeclaredVerbOf(name, verbs);

            if (verb is null)
            {
                findings.Add(new Finding("permission-verb-unknown", $"Permission '{permission.Id}' opens with '{first}', which is no verb of the use-case verb table.", permission.File, permission.Line, SubjectOf(permission)));
                continue;
            }

            // A baseline verb of any kind opens a permission; a glossary verb opens one only when it changes.
            if (UseCaseVerbs.Kinds.ContainsKey(verb))
            {
                continue;
            }

            if (verbs.TryGetValue(verb, out var kind) && kind != "query")
            {
                continue;
            }

            findings.Add(new Finding("permission-verb-unknown", $"Permission '{permission.Id}' opens with '{verb}', which is a glossary verb of kind query.", permission.File, permission.Line, SubjectOf(permission)));
        }
    }

    /// <summary>The glossary title a scope level takes: each kebab word capitalized, then Scope, as in Platform Scope.</summary>
    /// <param name="scope">The kebab-case scope level a role names.</param>
    /// <returns>The title the glossary term must carry.</returns>
    private static string ScopeTitle(string scope) =>
        string.Join(' ', scope.Split('-').Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]))
        + " Scope";

    /// <summary>The name a permission identifier carries after the prefix.</summary>
    /// <param name="id">The permission identifier.</param>
    /// <returns>The name after the prefix, or null when the identifier carries none.</returns>
    private static string? NameOf(string id)
    {
        if (!id.StartsWith(PermissionPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var name = id[PermissionPrefix.Length..];

        return name.Length == 0 ? null : name;
    }
}
