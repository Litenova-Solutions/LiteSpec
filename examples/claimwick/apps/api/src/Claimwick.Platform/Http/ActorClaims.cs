using System.Security.Claims;

namespace Claimwick.Platform.Http;

/// <summary>Reads the signed-in actor identity from verified claims for the generated endpoints.</summary>
/// <follows>litespec/quality/security#derive-the-actor-from-claims</follows>
public static class ActorClaims
{
    /// <summary>Reads the identity of the named actor from the verified name identifier claim. Claimwick issues every actor identity as the name identifier, so the actor name selects nothing today.</summary>
    /// <param name="user">The signed-in caller.</param>
    /// <param name="actor">The actor the use case reads, such as employee or manager.</param>
    /// <returns>The actor identity.</returns>
    public static Guid ReadActorId(ClaimsPrincipal user, string actor) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
