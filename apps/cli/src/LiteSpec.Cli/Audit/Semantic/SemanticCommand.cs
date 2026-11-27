using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Serve;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Audit.Semantic;

/// <summary>
///     The <c>litespec audit semantic</c> command: maps the files changed since a revision
///     to the use cases whose marked code or records they touch, writes one review packet
///     per use case, and reports a stub marker on any changed line outside the tests.
/// </summary>
/// <follows>litespec/core/agent#review-changed-use-cases-from-an-independent-packet</follows>
static class SemanticCommand
{
    /// <summary>The usage text for the audit semantic command, printed on help and on usage errors.</summary>
    public const string Usage = """
        Usage: litespec audit semantic --since <ref> [--root <consumer>] [--help]

        Maps the files changed since the revision to the use cases whose marked
        code or records they touch, writes one review packet per use case to
        .litespec/audit/semantic/<use-case-name>.md, and reports code-unimplemented
        for a changed line outside the tests holding a stub marker. The packet
        is the task packet of the use case plus its changed code files, for an
        independent reviewer; the command judges nothing else.

          --root      The consumer repository to read. Defaults to the working directory.
          --since     The git revision to diff the working tree against, such as HEAD~1.
          --help      Print this text and exit.

        Exit codes: 0 no changed line holds a stub marker, 1 a changed line holds one, 2 the options are invalid, the changes cannot be read, or a file cannot be written.
        """;

    /// <summary>The stub markers a changed line outside the tests must not hold.</summary>
    static readonly IReadOnlyList<string> StubMarkers =
    [
        "NotImplementedException",
        "TODO",
        "throw new Error(\"not implemented\")",
    ];

    /// <summary>Runs the audit semantic command.</summary>
    /// <param name="args">The command arguments after the subcommand.</param>
    /// <returns>The exit code: 0 for no stub markers, 1 for a stub marker, 2 for a usage error or an unreadable change.</returns>
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

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        return Execute(config, parsed.Value("--since")!, CliHost.Out, CliHost.Error);
    }

    /// <summary>Runs the audit on explicit writers, so tests prove the packets and the exit code without the process console.</summary>
    /// <param name="config">The consumer configuration the model is built from.</param>
    /// <param name="sinceRef">The git revision to diff the working tree against.</param>
    /// <param name="out">The writer receiving the packet and finding lines.</param>
    /// <param name="error">The writer receiving the failure line.</param>
    /// <returns>The exit code: 0 for no stub markers, 1 for a stub marker, 2 when the changes or the model cannot be read or a file cannot be written.</returns>
    internal static int Execute(ConsumerConfig config, string sinceRef, TextWriter @out, TextWriter error)
    {
        var (files, gitError) = SemanticGit.ChangedFiles(config.Root, sinceRef);

        if (files is null)
        {
            error.WriteLine(gitError);
            return 2;
        }

        using var state = new ServeModel(config);
        var changed = files.OrderBy(path => path, StringComparer.Ordinal).ToList();
        var mapped = Map(state.Model, config, changed);
        var subjects = Subjects(mapped);
        var findings = Scan(config, subjects, changed);

        List<string> packets;

        try
        {
            packets = WritePackets(state, config, mapped);
            WriteReport(config, sinceRef, mapped, packets, findings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine($"Cannot write the audit output: {exception.Message}");
            return 2;
        }

        for (var index = 0; index < mapped.Count; index++)
        {
            var code = mapped[index].CodeFiles.Count == 1 ? "code file" : "code files";
            @out.WriteLine($"{mapped[index].UseCase.Id}: {packets[index]} ({mapped[index].CodeFiles.Count} changed {code})");
        }

        foreach (var finding in findings)
        {
            @out.WriteLine(FindingLine(config, finding));
        }

        var useWord = mapped.Count == 1 ? "use case" : "use cases";
        var findingWord = findings.Count == 1 ? "finding" : "findings";
        @out.WriteLine($"Wrote {ReportRelative()} ({mapped.Count} {useWord}, {findings.Count} {findingWord}).");

        return findings.Count > 0 ? 1 : 0;
    }

    /// <summary>One use case with changed files: the record with its changed code and record files.</summary>
    /// <param name="UseCase">The use-case record the changed files map to.</param>
    /// <param name="CodeFiles">The changed code files the use case's marks name, absolute and in file order.</param>
    /// <param name="RecordFiles">The changed record files holding the use case, absolute and in file order.</param>
    internal sealed record MappedUseCase(Record UseCase, IReadOnlyList<string> CodeFiles, IReadOnlyList<string> RecordFiles);

    /// <summary>Maps each changed file to the use cases whose marked code or records hold it.</summary>
    /// <param name="model">The built project model with the implements links.</param>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="changed">The changed absolute paths in file order.</param>
    /// <returns>The mapped use cases in identifier order, each with its changed files in file order.</returns>
    static List<MappedUseCase> Map(ProjectModel model, ConsumerConfig config, IReadOnlyList<string> changed)
    {
        var useCases = model.Records
            .Where(record => record.Type == RecordTypes.UseCase)
            .ToDictionary(record => record.Id, StringComparer.Ordinal);
        var codeFiles = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var recordFiles = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        void Add(string useCaseId, string file, Dictionary<string, HashSet<string>> into)
        {
            if (!into.TryGetValue(useCaseId, out var files))
            {
                files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                into[useCaseId] = files;
            }

            files.Add(file);
        }

        foreach (var file in changed)
        {
            foreach (var record in model.Records)
            {
                if (record.Type == RecordTypes.UseCase && SameFile(record.File, file))
                {
                    Add(record.Id, file, recordFiles);
                }
            }

            foreach (var link in model.Implements)
            {
                if (useCases.ContainsKey(link.To) && SameRootedPath(config, link.Path, file))
                {
                    Add(link.To, file, codeFiles);
                }
            }
        }

        var ids = codeFiles.Keys
            .Concat(recordFiles.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var mapped = new List<MappedUseCase>(ids.Count);

        foreach (var id in ids)
        {
            var codes = codeFiles.TryGetValue(id, out var code) ? code.OrderBy(path => path, StringComparer.Ordinal).ToList() : [];
            var records = recordFiles.TryGetValue(id, out var held) ? held.OrderBy(path => path, StringComparer.Ordinal).ToList() : [];
            mapped.Add(new MappedUseCase(useCases[id], codes, records));
        }

        return mapped;
    }

    /// <summary>The subject of each changed file: the first use case in identifier order that it maps to.</summary>
    /// <param name="mapped">The mapped use cases in identifier order.</param>
    /// <returns>The changed absolute paths with their use-case identifier.</returns>
    static Dictionary<string, string> Subjects(IReadOnlyList<MappedUseCase> mapped)
    {
        var subjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in mapped)
        {
            foreach (var file in entry.CodeFiles.Concat(entry.RecordFiles))
            {
                subjects.TryAdd(file, entry.UseCase.Id);
            }
        }

        return subjects;
    }

    /// <summary>Scans each changed file outside the tests for a stub marker, one finding per marked line.</summary>
    /// <param name="config">The consumer configuration holding the test roots.</param>
    /// <param name="subjects">The changed absolute paths with their use-case identifier.</param>
    /// <param name="changed">The changed absolute paths in file order.</param>
    /// <returns>The stub findings in path and line order.</returns>
    static List<Finding> Scan(ConsumerConfig config, Dictionary<string, string> subjects, IReadOnlyList<string> changed)
    {
        var findings = new List<Finding>();

        foreach (var file in changed)
        {
            if (IsTestFile(config, file))
            {
                continue;
            }

            string[] lines;

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            for (var index = 0; index < lines.Length; index++)
            {
                if (MarkerOf(lines[index]) is { } marker)
                {
                    subjects.TryGetValue(file, out var subject);
                    findings.Add(new Finding(
                        "code-unimplemented",
                        $"Changed line holds '{marker}'; implement the behavior or remove the line before review.",
                        file,
                        index + 1,
                        subject));
                }
            }
        }

        return findings;
    }

    /// <summary>Whether the file is a test file: under a test root, in a test folder, or with a test file name.</summary>
    /// <param name="config">The consumer configuration holding the test roots.</param>
    /// <param name="absolute">The absolute file path.</param>
    /// <returns>True when the file is a test file, so the stub scan skips it.</returns>
    static bool IsTestFile(ConsumerConfig config, string absolute)
    {
        foreach (var root in config.TestRoots)
        {
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var segments = PathText.Relative(config.Root, absolute).Split('/');

        if (segments.Any(segment => segment.Equals("test", StringComparison.OrdinalIgnoreCase) || segment.Equals("tests", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var name = Path.GetFileNameWithoutExtension(absolute);

        return name.EndsWith("Test", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
            || name.Contains(".test.", StringComparison.OrdinalIgnoreCase)
            || name.Contains(".spec.", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The stub marker the line holds, or null when it holds none.</summary>
    /// <param name="line">The source line to test.</param>
    /// <returns>The first marker the line holds, or null.</returns>
    static string? MarkerOf(string line)
    {
        foreach (var marker in StubMarkers)
        {
            if (line.Contains(marker, StringComparison.Ordinal))
            {
                return marker;
            }
        }

        return null;
    }

    /// <summary>Writes one packet per mapped use case: the task packet plus its changed files.</summary>
    /// <param name="state">The warm model the packet builder reads.</param>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="mapped">The mapped use cases in identifier order.</param>
    /// <returns>The packet paths relative to the consumer root, in use-case order.</returns>
    static List<string> WritePackets(ServeModel state, ConsumerConfig config, List<MappedUseCase> mapped)
    {
        var packets = new List<string>(mapped.Count);

        if (mapped.Count == 0)
        {
            return packets;
        }

        var folder = Path.Combine(config.Root, ".litespec", "audit", "semantic");
        Directory.CreateDirectory(folder);

        foreach (var entry in mapped)
        {
            var (text, _) = ServeTools.TaskPacketText(state, entry.UseCase.Id);
            var packet = new StringBuilder(text);

            if (!packet.ToString().EndsWith('\n'))
            {
                packet.AppendLine();
            }

            var total = entry.RecordFiles.Count + entry.CodeFiles.Count;
            packet.AppendLine($"Changed files ({total}):");

            foreach (var file in entry.RecordFiles)
            {
                packet.AppendLine($"- {PathText.Relative(config.Root, file)} (record)");
            }

            foreach (var file in entry.CodeFiles)
            {
                packet.AppendLine($"- {PathText.Relative(config.Root, file)} (code)");
            }

            if (entry.CodeFiles.Count > 0)
            {
                packet.AppendLine("Changed code:");

                foreach (var file in entry.CodeFiles)
                {
                    var relative = PathText.Relative(config.Root, file);
                    packet.AppendLine($"### {relative}");
                    packet.AppendLine("```");
                    packet.AppendLine(ReadContent(file).TrimEnd());
                    packet.AppendLine("```");
                }
            }

            var path = Path.Combine(folder, PacketFileName(entry.UseCase.Id));
            File.WriteAllText(path, packet.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            packets.Add(PathText.Relative(config.Root, path));
        }

        return packets;
    }

    /// <summary>Writes the JSON report beside the packet folder: the revision, the use cases, the packets and the findings.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="sinceRef">The git revision the change set was read from.</param>
    /// <param name="mapped">The mapped use cases in identifier order.</param>
    /// <param name="packets">The packet paths relative to the consumer root, in use-case order.</param>
    /// <param name="findings">The stub findings in path and line order.</param>
    static void WriteReport(ConsumerConfig config, string sinceRef, IReadOnlyList<MappedUseCase> mapped, IReadOnlyList<string> packets, IReadOnlyList<Finding> findings)
    {
        var report = new JsonObject
        {
            ["since"] = sinceRef,
            ["useCases"] = CliJson.Strings(mapped.Select(entry => entry.UseCase.Id)),
            ["packets"] = CliJson.Strings(packets),
            ["findings"] = new JsonArray([.. findings.Select(finding => (JsonNode?)new JsonObject
            {
                ["path"] = PathText.Relative(config.Root, finding.Path),
                ["line"] = (JsonNode?)finding.Line,
                ["code"] = finding.Code,
                ["subject"] = finding.Subject is null ? null : JsonValue.Create(finding.Subject),
                ["message"] = finding.Message,
            })]),
        };
        var path = Path.Combine(config.Root, ".litespec", "audit", "semantic.json");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, CliJson.Serialize(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Reads the current content of a changed code file, or a note when it cannot be read.</summary>
    /// <param name="file">The absolute file path.</param>
    /// <returns>The file content, or the note naming the unreadable file.</returns>
    static string ReadContent(string file)
    {
        try
        {
            return File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return $"(unreadable: {file})";
        }
    }

    /// <summary>The packet file name for a use case: its identifier without the type prefix.</summary>
    /// <param name="useCaseId">The use-case identifier, such as use-case/lending.register-copy.</param>
    /// <returns>The file name, such as lending.register-copy.md.</returns>
    static string PacketFileName(string useCaseId)
    {
        var name = useCaseId;
        var slash = name.IndexOf('/');

        if (slash >= 0)
        {
            name = name[(slash + 1)..];
        }

        var clean = new StringBuilder(name.Length);

        foreach (var character in name)
        {
            clean.Append(char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '-');
        }

        var text = clean.ToString().Trim('-');

        return (text.Length > 0 ? text : "use-case") + ".md";
    }

    /// <summary>One finding's printed line: its path, line, level, code and message.</summary>
    /// <param name="config">The consumer configuration, for the relative path.</param>
    /// <param name="finding">The finding to print.</param>
    /// <returns>The printed line for the finding.</returns>
    static string FindingLine(ConsumerConfig config, Finding finding) =>
        $"{PathText.Relative(config.Root, finding.Path)}:{finding.Line} [{finding.Tier}] [{finding.Code}] {finding.Message}";

    /// <summary>The report path relative to the consumer root, as the summary names it.</summary>
    /// <returns>The root-relative report path with forward slashes.</returns>
    static string ReportRelative() => ".litespec/audit/semantic.json";

    /// <summary>Whether two absolute paths name one file.</summary>
    /// <param name="first">The first absolute path.</param>
    /// <param name="second">The second absolute path.</param>
    /// <returns>True when both name one file.</returns>
    static bool SameFile(string first, string second)
    {
        try
        {
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>Whether a root-relative link path names the changed absolute file.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="linkPath">The implements-link path, relative to the root.</param>
    /// <param name="file">The changed absolute file.</param>
    /// <returns>True when both name one file.</returns>
    static bool SameRootedPath(ConsumerConfig config, string linkPath, string file) =>
        SameFile(Path.Combine(config.Root, linkPath), file);
}
