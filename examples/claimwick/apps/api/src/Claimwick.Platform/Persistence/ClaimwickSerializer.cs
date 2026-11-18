using Claimwick.Platform.Http;
using Marten.Services;

namespace Claimwick.Platform.Persistence;

/// <summary>The shared document serializer: money converts through the platform converter.</summary>
internal sealed class ClaimwickSerializer : SystemTextJsonSerializer
{
    public ClaimwickSerializer()
    {
        Configure(options => options.Converters.Add(new MoneyJsonConverter()));
    }
}
