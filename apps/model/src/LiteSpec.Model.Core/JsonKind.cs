namespace LiteSpec.Model;

/// <summary>
///     The JSON kinds a value carries.
/// </summary>
public enum JsonKind
{
    /// <summary>An object: named values in braces.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "JsonKind.Object names the JSON object kind; renaming the member breaks the public record model and every reader that switches on it.")]
    Object,
    /// <summary>An array: values in brackets.</summary>
    Array,
    /// <summary>A string.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "JsonKind.String names the JSON string kind; renaming the member breaks the public record model and every reader that switches on it.")]
    String,
    /// <summary>A number, kept as written.</summary>
    Number,
    /// <summary>The literal true.</summary>
    True,
    /// <summary>The literal false.</summary>
    False,
    /// <summary>The literal null.</summary>
    Null,
}
