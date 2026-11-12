using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Model.Readers;

/// <summary>Reads the backend source for the configured backend roots.</summary>
interface IBackendReader
{
    /// <summary>Reads handlers, reactions, jobs, events, failure-code literals, comments and endpoint operations.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The backend read: the code model and the files it could not read.</returns>
    BackendRead Read(ConsumerConfig config);
}
