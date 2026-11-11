namespace LiteSpec.Model;

/// <summary>
///     The record layer a provision or a finding code governs. <see cref="Any" /> governs every
///     claim; the other four name the record layer a project claims in <c>layers</c>.
/// </summary>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
public enum RecordLayer
{
    /// <summary>Applies whatever the project claims.</summary>
    Any = 0,

    /// <summary>The Intent layer: product, goals, problems, requirements and the other why records.</summary>
    Intent = 1,

    /// <summary>The Behavior layer: modules, aggregates, use cases, rules and scenarios.</summary>
    Behavior = 2,

    /// <summary>The Implementation layer: the code that serves the Behavior records.</summary>
    Implementation = 3,

    /// <summary>The Proof layer: the tests and runs that prove the Behavior scenarios.</summary>
    Proof = 4,
}
