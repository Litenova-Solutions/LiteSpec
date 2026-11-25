using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Readers;

/// <summary>What the backend read found: the code model and the files it could not read.</summary>
/// <param name="Code">What the backend source says.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per file the reader could not read or parse.</param>
/// <param name="Semantics">The compilation the read was built from, or null when the syntax-only path ran.</param>
sealed record BackendRead(CodeFacts Code, IReadOnlyList<Finding> Problems, BackendSemantics? Semantics = null);
