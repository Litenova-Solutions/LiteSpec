namespace LiteSpec.Model;

/// <summary>The lookalike-term check: one concept, one spelling.</summary>
static partial class Checker
{
    /// <summary>Reports term-clash.</summary>
    private static void CheckTermLookalikes(ProjectModel model, List<Finding> findings)
    {
        foreach (var pair in TermLookalikes.FindPairs(model))
        {
            findings.Add(new Finding(
                "term-clash",
                TermLookalikes.MessageFor(pair),
                pair.Second.File,
                pair.Second.Line,
                pair.Second.Subject ?? pair.First.Subject));
        }
    }
}
