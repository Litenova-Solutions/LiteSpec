using LiteBus.Extensions.AspNetCore;
using LiteBus.Messaging.Abstractions;
using __NAMESPACE__.Kernel.Domain;

namespace __NAMESPACE__.Platform.Messaging;

/// <summary>
/// Turns a refused domain operation into the denial it names. The reason is the
/// client-safe detail of a refusal that carries one, else the catalog title,
/// never the exception message, which is for logs only.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#keep-technical-capabilities-in-the-platform</follows>
internal sealed class DomainExceptionTranslator : IRefusalExceptionTranslator
{
    private readonly IRefusalProblemCatalog _catalog;

    /// <summary>Creates the translator over the refusal problem catalog.</summary>
    /// <param name="catalog">The refusal problem catalog.</param>
    public DomainExceptionTranslator(IRefusalProblemCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <inheritdoc />
    public ValueTask<Refusal?> TranslateAsync(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (exception is not DomainException refusal || string.IsNullOrWhiteSpace(refusal.Code))
        {
            return ValueTask.FromResult<Refusal?>(null);
        }

        var map = _catalog.Resolve(refusal.Code, MediationOutcome.Denied);
        return ValueTask.FromResult<Refusal?>(Refusal.Denied(map.Title, refusal.Code));
    }
}
