namespace LiteSpec.Model;

/// <summary>
///     The architecture layer a file belongs to, by the role its type plays.
/// </summary>
public enum ArchitectureLayer
{
    /// <summary>No layer: an unrecognized assembly, a file outside the module tree, or an explicit opt-out.</summary>
    None,
    /// <summary>Business rules: aggregates, state records, value objects, domain events, domain exceptions, repository contracts.</summary>
    Domain,
    /// <summary>Cross-module contracts in layer-projects consumers only: pipeline positions, cross-module ports, refusals, integration events.</summary>
    ApplicationAbstractions,
    /// <summary>Use-case coordination: commands, queries, results, handlers, validators, ports, pipeline positions, module Contracts folders.</summary>
    Application,
    /// <summary>Technical adapters: repository implementations, mappings, module registration, store setup.</summary>
    Infrastructure,
    /// <summary>HTTP hosting: endpoints, request and response models, transport mappings, host projects.</summary>
    WebApi,
    /// <summary>Background hosting: the Worker host project.</summary>
    Worker,
    /// <summary>Test code, mirroring the module tree.</summary>
    Test,
}
