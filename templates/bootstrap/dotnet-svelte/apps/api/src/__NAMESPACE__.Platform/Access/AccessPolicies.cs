namespace __NAMESPACE__.Platform.Access;

/// <summary>
/// The authorization policies every use case declares. The sample signs people in
/// through the development route and authorizes the signed-in person; a product
/// adds its own policies beside this one.
/// </summary>
public static class AccessPolicies
{
    /// <summary>The policy a signed-in person satisfies.</summary>
    public const string SignedIn = "SignedIn";

    /// <summary>The refusal code of a caller with no sign-in. Mapped to 401.</summary>
    public const string SignInRequiredCode = "sample.sign-in-required";
}
