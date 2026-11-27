using System.Text;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Review;

/// <summary>
///     The <c>litespec docs review</c> command: runs the prose lints on the
///     Markdown and record files changed since a revision and writes a review
///     packet an independent reviewer answers. The command reads text only, so
///     it calls no model and no network.
/// </summary>
/// <follows>litespec/core/writing#write-human-docs-for-the-cold-reader</follows>
static class DocsReviewCommand
{
    /// <summary>The usage text for the docs review command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage:
          litespec docs review --since <ref> [--root <folder>]
          litespec docs review --help

        Runs the prose lints (paragraph length, banned LiteSpec-ese and promotional
        phrases, raw refusal codes in product copy) on the Markdown and record
        files changed since the revision, and writes a review packet to
        .litespec/review/docs-<timestamp>.md holding each changed file's diff,
        the Style Card rules, and the checklist an independent reviewer answers.

          --root              The repository to read. Defaults to the working directory.
          --since <ref>       The git revision to diff the working tree against, such as HEAD~1.
          --help              Print this text and exit.

        Exit codes: 0 no lint finding, 1 a lint finding, 2 usage error or unreadable changes.
        """;

    /// <summary>The prose extensions the review reads: Markdown pages and YAML record files.</summary>
    static readonly string[] ProseExtensions = [".md", ".yaml", ".yml"];

    /// <summary>Runs the docs review: lints the changed prose files and writes the review packet.</summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The exit code: 0 for no finding, 1 for a lint finding, 2 for a usage error.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root", "--since"], maxPositionals: 0);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is { } error)
        {
            return CommandFailure.UsageError(error, Usage, json: false);
        }

        if (string.IsNullOrEmpty(parsed.Value("--since")))
        {
            return CommandFailure.UsageError("Option --since needs a value.", Usage, json: false);
        }

        var root = FullPath(parsed.Value("--root") ?? CliHost.WorkingDirectory);

        if (!Directory.Exists(root))
        {
            return CommandFailure.Fail($"The root folder '{root}' does not exist.", json: false, code: 2);
        }

        var exit = Execute(root, parsed.Value("--since")!, CliHost.Out, CliHost.Error, out _);

        return exit;
    }

    /// <summary>Runs the review on explicit writers, so tests prove the packet and the exit code without the process console.</summary>
    /// <param name="root">The repository folder to read.</param>
    /// <param name="sinceRef">The git revision to diff the working tree against.</param>
    /// <param name="out">The writer receiving the summary lines.</param>
    /// <param name="error">The writer receiving the failure line.</param>
    /// <param name="packetPath">The absolute packet path written, or empty when nothing was written.</param>
    /// <returns>The exit code: 0 for no finding, 1 for a lint finding, 2 when the changes cannot be read or written.</returns>
    internal static int Execute(string root, string sinceRef, TextWriter @out, TextWriter error, out string packetPath)
    {
        packetPath = "";
        var (files, gitError) = ReviewGit.ChangedFiles(root, sinceRef);

        if (files is null)
        {
            error.WriteLine(gitError);
            return 2;
        }

        var prose = files
            .Where(path => ProseExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var findings = new List<ProseLint>();

        foreach (var file in prose)
        {
            findings.AddRange(DocsProseLints.LintFile(file, Path.GetRelativePath(root, file)));
        }

        findings.Sort((left, right) =>
        {
            var files = string.CompareOrdinal(left.File, right.File);

            return files != 0 ? files : left.Line.CompareTo(right.Line);
        });

        if (!WritePacket(root, sinceRef, prose, findings, out packetPath, out var writeError))
        {
            error.WriteLine(writeError);
            return 2;
        }

        foreach (var finding in findings)
        {
            @out.WriteLine($"{finding.File}:{finding.Line} [{finding.Code}] {finding.Message}");
        }

        @out.WriteLine($"Wrote {Path.GetRelativePath(root, packetPath)} ({prose.Count} files, {findings.Count} findings).");

        return findings.Count > 0 ? 1 : 0;
    }

    /// <summary>Writes the review packet: the changed files, their diffs, the Style Card rules, the reviewer checklist, and the lint findings.</summary>
    /// <param name="root">The repository folder the packet is written under.</param>
    /// <param name="sinceRef">The git revision the change set was read from.</param>
    /// <param name="prose">The changed prose files in order.</param>
    /// <param name="findings">The lint findings in file and line order.</param>
    /// <param name="packetPath">The absolute packet path written, or empty when nothing was written.</param>
    /// <param name="error">The write error, or empty when the packet was written.</param>
    /// <returns>True when the packet was written.</returns>
    static bool WritePacket(string root, string sinceRef, List<string> prose, List<ProseLint> findings, out string packetPath, out string error)
    {
        packetPath = "";
        error = "";

        try
        {
            var folder = Path.Combine(root, ".litespec", "review");
            Directory.CreateDirectory(folder);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var candidate = Path.Combine(folder, $"docs-{stamp}.md");
            var attempt = 1;

            while (File.Exists(candidate))
            {
                attempt++;
                candidate = Path.Combine(folder, $"docs-{stamp}-{attempt}.md");
            }

            var text = new StringBuilder();

            text.AppendLine("# Docs Review Packet");
            text.AppendLine();
            text.AppendLine($"Base revision: {sinceRef}");
            text.AppendLine($"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Changed prose files: {prose.Count}");
            text.AppendLine($"Lint findings: {findings.Count}");
            text.AppendLine();
            text.AppendLine("## Changed Files");
            text.AppendLine();

            if (prose.Count == 0)
            {
                text.AppendLine("No Markdown or record files changed since the base revision.");
            }
            else
            {
                foreach (var file in prose)
                {
                    text.AppendLine($"- {Path.GetRelativePath(root, file)}");
                }
            }

            text.AppendLine();
            text.AppendLine("## Diffs");
            text.AppendLine();

            if (prose.Count == 0)
            {
                text.AppendLine("No diffs: no prose files changed.");
            }
            else
            {
                foreach (var file in prose)
                {
                    text.AppendLine($"### {Path.GetRelativePath(root, file)}");
                    text.AppendLine();
                    text.AppendLine("```diff");
                    text.AppendLine(ReviewGit.DiffFor(root, sinceRef, file).TrimEnd());
                    text.AppendLine("```");
                    text.AppendLine();
                }
            }

            text.AppendLine("## Style Card Rules");
            text.AppendLine();
            text.AppendLine("The reviewer judges each changed file against these rules from the LiteSpec Style Card:");
            text.AppendLine();
            text.AppendLine("1. Lead with the answer: the task, the decision, or the fact first, then the reason and one example.");
            text.AppendLine("2. Write for the cold reader: name who the page concerns and what is at stake before any pointer.");
            text.AppendLine("3. Name the actor and the act: active voice, present tense, ordinary technical verbs.");
            text.AppendLine("4. Use literal domain words: the glossary term for each concept, no figures of speech.");
            text.AppendLine("5. State what the reader can check: the exact path, command, value, or limit instead of praise.");
            text.AppendLine("6. Keep one idea in each paragraph: two to four sentences per paragraph.");
            text.AppendLine("7. Keep normative strength intact: never soften a requirement or harden advice when shortening.");
            text.AppendLine("8. Show one example per rule from the product the page documents.");
            text.AppendLine();
            text.AppendLine("## Review Checklist");
            text.AppendLine();
            text.AppendLine("The independent reviewer answers each question with yes or no and quotes the line that decides it:");
            text.AppendLine();
            text.AppendLine("- [ ] Plain voice: does every changed paragraph read as plainspoken technical prose with no stilted or promotional phrase?");
            text.AppendLine("- [ ] No word shuffling: does every shortened sentence keep the words and conditions of the record it restates?");
            text.AppendLine("- [ ] Normative strength kept: does each requirement still read as required and each default still read as a default?");
            text.AppendLine("- [ ] Cold-reader clarity: does each changed page state who it concerns and what is at stake before it points elsewhere?");
            text.AppendLine();
            text.AppendLine("## Lint Findings");
            text.AppendLine();

            if (findings.Count == 0)
            {
                text.AppendLine("No findings: every changed line passes the prose lints.");
            }
            else
            {
                foreach (var finding in findings)
                {
                    text.AppendLine($"- {finding.File}:{finding.Line} [{finding.Code}] {finding.Message}");
                }
            }

            File.WriteAllText(candidate, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            packetPath = candidate;

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = $"Cannot write the review packet: {exception.Message}";
            return false;
        }
    }

    /// <summary>The absolute path of an option value: as written when absolute, else under the working directory.</summary>
    /// <param name="path">The option value.</param>
    /// <returns>The absolute path.</returns>
    static string FullPath(string path) =>
        Path.IsPathFullyQualified(path) ? path : Path.Combine(CliHost.WorkingDirectory, path);
}
