using System.Globalization;
using System.Security.Claims;

namespace Claimwick.Platform.Http;

/// <summary>Reads the signed-in actor identity and claim values from verified claims for the generated endpoints.</summary>
/// <follows>litespec/quality/security#derive-the-actor-from-claims</follows>
public static class ActorClaims
{
    /// <summary>Reads the identity of the named actor from the verified name identifier claim, answering it as the requested identity type. Claimwick issues every actor identity as the name identifier, so the actor name selects nothing today. A typed identity converts from the claim GUID through its factory.</summary>
    /// <param name="user">The signed-in caller.</param>
    /// <param name="actor">The actor the use case reads, such as employee or manager.</param>
    /// <returns>The actor identity as <typeparamref name="T"/>.</returns>
    public static T ReadActorId<T>(ClaimsPrincipal user, string actor)
    {
        var id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (typeof(T) == typeof(Guid))
        {
            return (T)(object)id;
        }

        var from = typeof(T).GetMethod("From", new[] { typeof(Guid) })!;

        return (T)from.Invoke(null, new object[] { id })!;
    }

    /// <summary>Reads the value of the named claim, parsed as the requested scalar type. Claimwick verifies every claim at sign-in, so a missing claim is a deployment fault.</summary>
    /// <param name="user">The signed-in caller.</param>
    /// <param name="claim">The claim name the use case reads, such as approverIsFinance.</param>
    /// <returns>The claim value parsed as <typeparamref name="T"/>.</returns>
    public static T ReadClaimValue<T>(ClaimsPrincipal user, string claim)
    {
        var text = user.FindFirstValue(claim)!;
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (target == typeof(DateOnly))
        {
            return (T)(object)DateOnly.Parse(text, CultureInfo.InvariantCulture);
        }

        if (target == typeof(DateTimeOffset))
        {
            return (T)(object)DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
        }

        if (target == typeof(TimeSpan))
        {
            return (T)(object)TimeSpan.Parse(text, CultureInfo.InvariantCulture);
        }

        return (T)Convert.ChangeType(text, target, CultureInfo.InvariantCulture);
    }
}
