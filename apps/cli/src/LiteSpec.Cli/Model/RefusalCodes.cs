namespace LiteSpec.Model;

/// <summary>
///     The refusal codes of the model: the code each rule refuses with, and the
///     codes each class carries, indexed from the literals the code read found.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RefusalCodes
{
    /// <summary>
    ///     The refusal code of the rule: the code of the refusal result it names,
    ///     or its bare refusal code. The shared refusal rule over the model.
    /// </summary>
    /// <param name="model">The project model.</param>
    /// <param name="rule">The rule record.</param>
    /// <returns>The refusal code of the rule, or null when it refuses with none.</returns>
    public static string? ResultCode(ProjectModel model, Record rule)
    {
        return RefusalCode.Of(rule, ById);

        IRecordFacts? ById(string id) => model.ById.TryGetValue(id, out var result) ? result : null;
    }

    /// <summary>Indexes the refusal codes each class carries from the literals the code read found.</summary>
    /// <param name="model">The project model.</param>
    public static void Compute(ProjectModel model)
    {
        foreach (var literal in model.Code.Literals)
        {
            if (literal.Class is null)
            {
                continue;
            }

            if (!model.RefusalCodes.TryGetValue(literal.Class, out var codes))
            {
                codes = new HashSet<string>(StringComparer.Ordinal);
                model.RefusalCodes[literal.Class] = codes;
            }

            codes.Add(literal.Value);
        }
    }
}
