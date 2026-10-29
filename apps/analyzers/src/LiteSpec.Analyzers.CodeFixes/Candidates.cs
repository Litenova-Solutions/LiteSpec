using System;
using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Derives the records a fix offers: the candidates for a missing mark from
///     the folder and the type or member name, and the nearest records for a
///     mistyped id by edit distance.
/// </summary>
internal static class Candidates
{
    /// <summary>
    ///     The candidate records for a missing mark: records of the kind's
    ///     expected types whose identifier the folders and the type or member
    ///     name derive, or scenarios only for a test. An exact home-and-name
    ///     match comes first, then name-part matches, each in identifier order.
    /// </summary>
    /// <param name="kind">The kind of the unmarked element.</param>
    /// <param name="folders">The nearest folder names, containing first.</param>
    /// <param name="typeName">The simple type name.</param>
    /// <param name="memberName">The member name for a method, else null.</param>
    /// <param name="index">The record index.</param>
    /// <returns>The candidates, exact matches first.</returns>
    public static IReadOnlyList<IRecordFacts> Derive(
        CodeKind kind, IReadOnlyList<string> folders, string typeName, string? memberName, RecordIndex index)
    {
        var stem = Kebab(StripSuffix(kind, memberName ?? typeName));
        var exacts = new HashSet<string>(StringComparer.Ordinal);

        foreach (var folder in folders)
        {
            var home = Kebab(folder);

            if (home.Length > 0 && stem.Length > 0)
            {
                exacts.Add(home + "." + stem);
            }
        }

        if (exacts.Count == 0 && stem.Length > 0)
        {
            exacts.Add(stem);
        }

        var exactMatches = new List<IRecordFacts>();
        var nameMatches = new List<IRecordFacts>();

        // A test proves the lowest record it verifies, from any of its kinds, but
        // the fix offers scenarios: the other proof targets are named by hand.
        var types = kind == CodeKind.Test
            ? new string[] { "scenario" }
            : MarkRules.RecordTypes(kind);

        foreach (var type in types)
        {
            foreach (var record in index.ByType(type))
            {
                if (IsExact(record.Id, type, exacts))
                {
                    exactMatches.Add(record);
                }
                else if (stem.Length > 0 && NameMatches(record.Id, stem))
                {
                    nameMatches.Add(record);
                }
            }
        }

        exactMatches.Sort(CompareIds);
        nameMatches.Sort(CompareIds);
        exactMatches.AddRange(nameMatches);

        return exactMatches;
    }

    /// <summary>
    ///     The three nearest records to the mistyped id among the candidates, by
    ///     edit distance over the identifier, then in identifier order.
    /// </summary>
    /// <param name="typed">The mistyped identifier.</param>
    /// <param name="candidates">The records of the expected types.</param>
    /// <returns>Up to three records, nearest first.</returns>
    public static IReadOnlyList<IRecordFacts> NearestThree(string typed, IReadOnlyList<IRecordFacts> candidates)
    {
        var ranked = new List<IRecordFacts>(candidates);
        ranked.Sort((left, right) =>
        {
            var distance = Distance(typed, left.Id).CompareTo(Distance(typed, right.Id));

            return distance != 0 ? distance : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        });

        if (ranked.Count > 3)
        {
            ranked.RemoveRange(3, ranked.Count - 3);
        }

        return ranked;
    }

    /// <summary>The edit distance between two identifiers: insertions, deletions and substitutions.</summary>
    public static int Distance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= right.Length; j++)
            {
                var substitution = previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
            }

            var swap = previous;
            previous = current;
            current = swap;
        }

        return previous[right.Length];
    }

    private static int CompareIds(IRecordFacts left, IRecordFacts right) =>
        string.Compare(left.Id, right.Id, StringComparison.Ordinal);

    private static bool IsExact(string recordId, string type, HashSet<string> exacts)
    {
        var prefix = type + "/";

        return recordId.StartsWith(prefix, StringComparison.Ordinal)
            && exacts.Contains(recordId.Substring(prefix.Length));
    }

    /// <summary>Whether the record's name part matches the derived stem, wholly or as a tail.</summary>
    private static bool NameMatches(string recordId, string stem)
    {
        var dot = recordId.LastIndexOf('.');
        var name = dot < 0 ? recordId : recordId.Substring(dot + 1);

        return string.Equals(name, stem, StringComparison.OrdinalIgnoreCase)
            || stem.EndsWith("-" + name, StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("-" + stem, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The name without the kind's suffix, or the whole name when nothing strips.</summary>
    private static string StripSuffix(CodeKind kind, string name)
    {
        string[] suffixes;

        switch (kind)
        {
            case CodeKind.Handler:
                suffixes = new string[] { "CommandHandler", "QueryHandler", "Handler" };
                break;
            case CodeKind.Endpoint:
                suffixes = new string[] { "Endpoint" };
                break;
            case CodeKind.EventHandler:
                suffixes = new string[] { "Handler" };
                break;
            case CodeKind.Job:
                suffixes = new string[] { "Job", "Worker" };
                break;
            case CodeKind.DomainEvent:
                suffixes = new string[] { "Event" };
                break;
            case CodeKind.RuleException:
                suffixes = new string[] { "Exception" };
                break;
            default:
                return name;
        }

        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length)
            {
                return name.Substring(0, name.Length - suffix.Length);
            }
        }

        return name;
    }

    /// <summary>PascalCase, underscores and dots in kebab-case: SubmitExpense reads submit-expense.</summary>
    private static string Kebab(string name)
    {
        var built = new System.Text.StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];

            if (character == '_' || character == '.' || character == '-' || character == ' ')
            {
                if (built.Length > 0 && built[built.Length - 1] != '-')
                {
                    built.Append('-');
                }

                continue;
            }

            if (character >= 'A' && character <= 'Z')
            {
                if (built.Length > 0 && built[built.Length - 1] != '-')
                {
                    var previous = name[i - 1];
                    var next = i + 1 < name.Length ? name[i + 1] : 'a';

                    if ((previous >= 'a' && previous <= 'z')
                        || (previous >= '0' && previous <= '9')
                        || (next >= 'a' && next <= 'z'))
                    {
                        built.Append('-');
                    }
                }

                built.Append(char.ToLowerInvariant(character));
                continue;
            }

            built.Append(char.ToLowerInvariant(character));
        }

        var kebab = built.ToString().Trim('-');

        while (kebab.IndexOf("--", StringComparison.Ordinal) >= 0)
        {
            kebab = kebab.Replace("--", "-");
        }

        return kebab;
    }
}
