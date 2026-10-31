namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>One type declaration the reader found, with its namespace-qualified name.</summary>
/// <param name="Qualified">The namespace-qualified name, including each containing type.</param>
/// <param name="Namespace">The namespace holding the declaration.</param>
/// <param name="File">The file declaring the type.</param>
sealed record DeclaredType(string Qualified, string Namespace, string File);
