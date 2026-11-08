namespace LiteSpec.Model;

/// <summary>
///     The part of the LiteSpec contract a project claims in the <c>conformance</c>
///     field of <c>.litespec/project.json</c>. The levels nest: each one claims
///     everything the level before it claims.
/// </summary>
/// <follows>litespec/core/scope#declare-the-conformance-level</follows>
public enum ConformanceLevel
{
    /// <summary>The Intent, Behavior, governance and glossary records follow the record model.</summary>
    Records = 0,

    /// <summary>Also, code and tests mark the records they serve and test runs prove scenarios, in the project's own code layout.</summary>
    Traced = 1,

    /// <summary>Also, the selected stacks, LiteSpec's code layout, the analyzers and the operations provisions.</summary>
    Full = 2,
}
