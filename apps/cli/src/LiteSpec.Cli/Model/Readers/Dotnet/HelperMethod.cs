namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>
///     One helper method the source tree declares: the type declaring it, its parameters by
///     name with their simple types, and the dispatches it makes through each receiver.
/// </summary>
/// <param name="Type">The type declaring the helper.</param>
/// <param name="Method">The helper method name.</param>
/// <param name="Parameters">The parameters by name with their simple types.</param>
/// <param name="Dispatches">The dispatches the helper makes through each receiver.</param>
/// <param name="QualifiedType">The namespace-qualified name of the declaring type.</param>
sealed record HelperMethod(
    string Type,
    string Method,
    Dictionary<string, string> Parameters,
    List<(string Receiver, CodeDispatch Dispatch)> Dispatches,
    string QualifiedType = "");
