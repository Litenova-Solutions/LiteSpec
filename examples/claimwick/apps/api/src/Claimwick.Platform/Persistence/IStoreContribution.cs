using Marten;

namespace Claimwick.Platform.Persistence;

/// <summary>One module's contribution to the shared document store: its stored aggregates.</summary>
public interface IStoreContribution
{
    /// <summary>Registers the module's stored aggregates on the shared store options.</summary>
    void Configure(StoreOptions options);
}
