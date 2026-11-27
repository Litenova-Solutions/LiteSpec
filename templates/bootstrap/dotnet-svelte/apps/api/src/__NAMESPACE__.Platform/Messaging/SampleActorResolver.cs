using LiteBus.Auditing.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace __NAMESPACE__.Platform.Messaging;

/// <summary>
/// Names the audit actor from the signed-in person. The development sign-in carries
/// the person slug as the user name, so every audit record in a fresh product
/// names the person who acted; callers with no signed-in person audit as the sample.
/// </summary>
/// <follows>decision/0001-audit-trail</follows>
internal sealed class SampleActorResolver : IAuditActorResolver
{
    /// <inheritdoc />
    public AuditActor Resolve(MessageCompletionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var name = context.Identity.Principal?.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? AuditActor.System("sample") : AuditActor.User(name);
    }
}
