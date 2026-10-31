using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>The parse cache of one backend read: every file parsed once, in parallel.</summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class SyntaxTreeCache
{
    /// <summary>
    ///     Parses every file once, in parallel, into a cache from file path to
    ///     syntax tree. A file that cannot be read or parsed is left out and
    ///     reported as one <c>reader-missing</c> problem naming the file.
    /// </summary>
    /// <param name="files">The files to parse.</param>
    /// <param name="onParsed">Invoked with each file after it parses, or null for no callback.</param>
    /// <returns>The parsed syntax trees by file path and the reader problems.</returns>
    public static (IReadOnlyDictionary<string, SyntaxTree> Trees, List<Finding> Problems) Parse(
        IReadOnlyList<string> files, Action<string>? onParsed)
    {
        var trees = new Dictionary<string, SyntaxTree>(files.Count, StringComparer.Ordinal);
        var problems = new List<Finding>();
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => new List<Finding>(),
            (file, _, local) =>
            {
                SyntaxTree tree;

                try
                {
                    tree = CSharpSyntaxTree.ParseText(SourceTree.Read(file), new CSharpParseOptions(LanguageVersion.Preview), file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    local.Add(new Finding(
                        "reader-missing",
                        $"Backend source file '{file}' could not be read: {exception.Message}",
                        file,
                        1,
                        null)
                    {
                        Reader = Finding.RoslynReader,
                        ReaderIsQualified = true,
                    });

                    return local;
                }

                var parseError = tree.GetDiagnostics()
                    .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

                if (parseError is not null)
                {
                    local.Add(new Finding(
                        "reader-missing",
                        $"Backend source file '{file}' could not be parsed: {parseError.GetMessage()}",
                        file,
                        1,
                        null)
                    {
                        Reader = Finding.RoslynReader,
                        ReaderIsQualified = true,
                    });

                    return local;
                }

                lock (gate)
                {
                    trees[file] = tree;
                }

                onParsed?.Invoke(file);

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    problems.AddRange(local);
                }
            });

        return (trees, problems);
    }
}
