using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The lookalike-term check: one concept, one spelling.</summary>
/// <follows>litespec/core/authoring#review-each-term-for-its-lookalikes</follows>
internal static class TermLookalikeChecks
{
    /// <summary>Reports term-clash.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var pair in TermLookalikes.FindPairs(model))
        {
            findings.Add(new Finding(
                "term-clash",
                LookalikeMessage(pair),
                pair.Second.File,
                pair.Second.Line,
                pair.Second.Subject ?? pair.First.Subject));
        }
    }

    /// <summary>The B8 finding message for one pair, starting with the second term and its holder.</summary>
    /// <param name="pair">The two lookalike term occurrences whose clash is described.</param>
    /// <returns>The finding message that names both spellings and the records that hold them.</returns>
    private static string LookalikeMessage(LookalikePair pair) => pair.Kind switch
    {
        "spelling" => $"Term '{pair.Second.Seen}' holds '{pair.Second.Form}' which mixes British and American spelling with '{pair.First.Form}' in '{pair.First.Seen}'.",
        "plurality" => $"Term '{pair.Second.Seen}' holds '{pair.Second.Form}' which is the singular or plural of '{pair.First.Form}' in '{pair.First.Seen}'.",
        _ => $"Term '{pair.Second.Seen}' holds '{pair.Second.Form}' which differs only in hyphenation from '{pair.First.Form}' in '{pair.First.Seen}'.",
    };
}
