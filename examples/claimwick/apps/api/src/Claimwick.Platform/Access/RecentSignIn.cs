namespace Claimwick.Platform.Access;

/// <summary>One permission the caller must hold with a sign-in younger than the recent sign-in window.</summary>
public sealed record RecentSignIn(string Id);
