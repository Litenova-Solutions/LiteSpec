namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>
///     One direct package in a bill of materials: the ecosystem and name its package URL names,
///     the shipped version, the concluded license, and the file that lists it.
/// </summary>
/// <param name="Ecosystem">The package ecosystem, such as nuget or npm.</param>
/// <param name="Name">The package name with its scope, such as PdfSharp or @scope/name.</param>
/// <param name="Version">The shipped version, or empty when the document states none.</param>
/// <param name="License">The concluded license, or empty when the document states none.</param>
/// <param name="File">The absolute path of the bill of materials.</param>
/// <follows>litespec/workspace/dependencies#match-every-shipped-package-to-its-pin</follows>
sealed record DirectPackage(string Ecosystem, string Name, string Version, string License, string File)
{
    /// <summary>The package id: the ecosystem and name joined by a colon.</summary>
    public string Id => $"{Ecosystem}:{Name}";
}
