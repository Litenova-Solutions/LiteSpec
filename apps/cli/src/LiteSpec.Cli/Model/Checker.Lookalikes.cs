namespace LiteSpec.Model;

/// <summary>The lookalike-term check: one concept, one spelling.</summary>
static partial class Checker
{
    /// <summary>Reports term-lookalike.</summary>
    private static void CheckTermLookalikes(ProjectModel model, List<Finding> findings)
    {
        foreach (var pair in TermLookalikes.FindPairs(model))
        {
            findings.Add(new Finding(
                "term-lookalike",
                TermLookalikes.MessageFor(pair),
                pair.Second.File,
                pair.Second.Line,
                pair.Second.Subject ?? pair.First.Subject));
        }
    }
}
