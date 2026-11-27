using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Readers.Seeds;

/// <summary>
///     Reads the seeds marks of the backend and frontend source: the <c>&lt;seeds&gt;</c> documentation
///     tags of C# files and the <c>// litespec: seeds</c> line comments of TypeScript files. Each mark
///     names the cast entries a development seed or fixture uses, through the shared mark parser.
/// </summary>
/// <follows>litespec/quality/development#seed-only-development-and-test-environments</follows>
static class SeedMarkReader
{
    /// <summary>The C# extension, which carries the documentation tags.</summary>
    private const string CSharp = ".cs";

    /// <summary>The TypeScript extensions, which carry the line comments.</summary>
    private static readonly string[] TypeScript = [".ts", ".tsx"];

    /// <summary>Reads every seeds mark under the configured backend, test and frontend roots.</summary>
    /// <param name="config">The consumer configuration that locates the source roots.</param>
    /// <returns>Every seeds mark, in file then line order.</returns>
    public static List<CodeMark> Read(ConsumerConfig config)
    {
        var marks = new List<CodeMark>();

        foreach (var root in Roots(config))
        {
            foreach (var file in SourceTree.Files(root, [CSharp, .. TypeScript]))
            {
                var text = SourceTree.Read(file);

                if (file.EndsWith(CSharp, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var mark in MarkParser.ReadSeedTags(text))
                    {
                        marks.Add(new CodeMark(mark.RecordId, MarkStyle.Xml, mark.Verb, file, LineOf(text, mark.Index)));
                    }
                }
                else
                {
                    foreach (var mark in MarkParser.ReadSeedComments(text))
                    {
                        marks.Add(new CodeMark(mark.RecordId, MarkStyle.Comment, mark.Verb, file, LineOf(text, mark.Index)));
                    }
                }
            }
        }

        marks.Sort((left, right) =>
        {
            var file = string.CompareOrdinal(left.File, right.File);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });

        return marks;
    }

    /// <summary>The source roots a seeds mark may sit under: the backend hosts, the kernel and platform, the tests and each frontend.</summary>
    /// <param name="config">The consumer configuration that locates the roots.</param>
    /// <returns>The distinct roots, in a stable order.</returns>
    private static List<string> Roots(ConsumerConfig config)
    {
        var roots = new List<string>();

        roots.AddRange(config.ModulesRoots);
        roots.AddRange(config.WebApiRoots);
        roots.AddRange(config.WorkerRoots);
        roots.AddRange(config.TestRoots);

        if (config.KernelRoot.Length > 0)
        {
            roots.Add(config.KernelRoot);
        }

        if (config.PlatformRoot.Length > 0)
        {
            roots.Add(config.PlatformRoot);
        }

        roots.AddRange(config.Frontends.Select(frontend => frontend.Path));

        return roots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The 1-based line of an offset in the text.</summary>
    /// <param name="text">The text the offset indexes.</param>
    /// <param name="index">The zero-based offset.</param>
    /// <returns>The line the offset sits on.</returns>
    private static int LineOf(string text, int index)
    {
        var line = 1;

        for (var at = 0; at < index && at < text.Length; at++)
        {
            if (text[at] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
