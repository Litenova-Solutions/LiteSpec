namespace LiteSpec.Cli.Model.Screens;

/// <summary>One screen call: the operation a screen called and the use case it reaches.</summary>
sealed record ScreenCall(string Operation, string UseCase);
