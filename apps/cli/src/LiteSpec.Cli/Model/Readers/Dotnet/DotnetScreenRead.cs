namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>What the .NET screen read found: route facts and one problem per file it could not read or parse.</summary>
/// <param name="Routes">The route entries.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per unreadable or unparsable file.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record DotnetScreenRead(IReadOnlyList<RouteFact> Routes, IReadOnlyList<Finding> Problems);
