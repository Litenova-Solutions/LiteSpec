using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>One import the application boundary refuses, with its finding text.</summary>
/// <param name="Pattern">The import specifier pattern refused.</param>
/// <param name="Message">The finding text for the refusal.</param>
sealed record ImportBoundary(Regex Pattern, string Message);
