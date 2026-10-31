using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Tests.Support;

/// <summary>The outcome of one generator run.</summary>
/// <param name="Sources">The generated sources by hint name.</param>
/// <param name="ReportedDiagnostics">The diagnostics the generator reported.</param>
/// <param name="CompileErrors">The errors of the user code with the generated sources.</param>
/// <param name="RunResult">The full run result, with the tracked steps.</param>
internal sealed record GeneratorRun(
    IReadOnlyDictionary<string, string> Sources,
    ImmutableArray<Diagnostic> ReportedDiagnostics,
    IReadOnlyList<Diagnostic> CompileErrors,
    GeneratorDriverRunResult RunResult);
