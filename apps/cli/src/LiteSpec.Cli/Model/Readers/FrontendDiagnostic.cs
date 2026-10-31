namespace LiteSpec.Model.Readers;

/// <summary>One file a frontend reader could not parse.</summary>
/// <param name="File">The file, relative to the frontend root.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Message">What the reader could not parse.</param>
sealed record FrontendDiagnostic(string File, int Line, string Message);
