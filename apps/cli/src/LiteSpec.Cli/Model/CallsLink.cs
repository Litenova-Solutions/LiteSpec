namespace LiteSpec.Model;

/// <summary>One computed calls link: a screen to a use case.</summary>
sealed record CallsLink(string Screen, string To, string Operation);
