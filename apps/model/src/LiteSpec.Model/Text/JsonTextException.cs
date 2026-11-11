using System;

namespace LiteSpec.Model;

/// <summary>
///     The parse error a JSON text carries: what was wanted, and the 1-based line.
/// </summary>
public sealed class JsonTextException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="JsonTextException"/> class.Builds the error for the line.</summary>
    /// <param name="message">What was wanted, naming the line.</param>
    /// <param name="line">The 1-based line.</param>
    public JsonTextException(string message, int line)
        : base(message) => Line = line;

    /// <summary>Gets the 1-based line holding the error.</summary>
    public int Line { get; }
}
