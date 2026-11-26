namespace LiteSpec.Cli.Model.Refusals;

/// <summary>One value a refusal message varies by: its constructor parameter.</summary>
/// <param name="Name">The parameter name, a C# identifier named in the meaning as <c>{name}</c>.</param>
/// <param name="Meaning">What the value says, in one sentence.</param>
sealed record RefusalParameter(string Name, string Meaning);
