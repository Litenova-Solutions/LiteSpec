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
    /// <summary>Use-case coordination: commands, queries, results, handlers, validators, pipeline positions, module Contracts folders.</summary>
    Application,
    /// <summary>Driven ports: the interfaces the module declares for what it does not compute in memory, with the types their signatures name.</summary>
    Ports,
    /// <summary>Technical adapters: repository implementations, mappings, module registration, store setup.</summary>
    Infrastructure,
    /// <summary>HTTP hosting: endpoints, request and response models, transport mappings, host projects.</summary>
    WebApi,
    /// <summary>Background hosting: the Worker host project.</summary>
    Worker,
    /// <summary>Test code, mirroring the module tree.</summary>
    Test,
}
