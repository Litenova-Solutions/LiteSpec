namespace LiteSpec.Model;

/// <summary>One module's names: verbs, nouns, facts, events, codes, keys, and settings.</summary>
/// <param name="Module">The module name.</param>
/// <param name="UseCases">How many use cases the module holds.</param>
/// <param name="Verbs">The verbs in use with counts, most used first.</param>
/// <param name="Nouns">The nouns after the verb with counts, most used first.</param>
/// <param name="ReactionFacts">The facts the module's reactions answer.</param>
/// <param name="Events">The module's event ids.</param>
/// <param name="RefusalCodes">The refusal codes of the module's use cases.</param>
/// <param name="TemplateKeys">The module's notification template keys.</param>
/// <param name="Settings">The module's setting names.</param>
sealed record ModuleNames(
    string Module,
    int UseCases,
    IReadOnlyList<NameCount> Verbs,
    IReadOnlyList<NameCount> Nouns,
    IReadOnlyList<string> ReactionFacts,
    IReadOnlyList<string> Events,
    IReadOnlyList<string> RefusalCodes,
    IReadOnlyList<string> TemplateKeys,
    IReadOnlyList<string> Settings);
