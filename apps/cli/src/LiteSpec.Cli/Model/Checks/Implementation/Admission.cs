namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>One admission: the admitted package id and the license its entry names, when it names one.</summary>
internal sealed record Admission(string Ecosystem, string Name, string? License);
