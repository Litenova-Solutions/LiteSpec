namespace LiteSpec.Model;

/// <summary>One computed proves link: a test to the record it claims.</summary>
sealed record ProvesLink(string Scenario, string Claim, bool Confirmed, bool? Passing, bool Indirect);
