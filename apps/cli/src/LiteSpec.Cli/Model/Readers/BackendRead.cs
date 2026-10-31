namespace LiteSpec.Model.Readers;

/// <summary>What the backend read found: the code model and the files it could not read.</summary>
/// <param name="Code">What the backend source says.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per file the reader could not read or parse.</param>
sealed record BackendRead(CodeModel Code, IReadOnlyList<Finding> Problems);
