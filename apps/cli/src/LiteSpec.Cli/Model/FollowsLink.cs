namespace LiteSpec.Model;

/// <summary>One computed follows link: code to the provision, decision or exception that shapes it.</summary>
sealed record FollowsLink(string Path, string To, string Kind);
