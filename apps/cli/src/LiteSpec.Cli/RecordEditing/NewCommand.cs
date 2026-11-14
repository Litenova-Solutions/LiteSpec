using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.RecordEditing;

/// <summary>
///     The <c>litespec new</c> command: scaffolds one record file from the
///     template for its type, with the identifier filled in and reserved, and
///     one document from the template for a document style. A decision takes
///     the next free number from the one decision sequence.
/// </summary>
/// <follows>litespec/core/model#scaffold-pages-and-reserve-identifiers</follows>
/// <follows>litespec/core/agent#search-the-provisions-before-recording-a-decision</follows>
static class NewCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec new <type> <id> [--root <path>] [--help]
               litespec new <document-style> <name> [--root <path>]
               litespec new decision <name> [--root <path>]
               litespec new requirement <feature>.<name> [--root <path>]
               litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
               litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
               litespec new entry <page-id> <type> <name> [--root <path>]

        Creates one record file from the template for its type, with every
        required field already filled. It reserves the identifier before any
        prose exists, so two authors working in parallel never mint the same
        identifier twice. <type> is the record type, such as use-case, reaction,
        or decision. <id> is the home.name part of the identifier, such as
        lending.notify-reader; the full identifier is <type>/<id>. The file is
        written at the home its type owns, under behavior/: a reaction under
        modules/<module>/reactions/, a job under modules/<module>/jobs/,
        a notification under modules/<module>/notifications/, a decision under
        the decisions folder. A named record is written in its own folder beside
        its explanation file: modules/<module>/reactions/<name>/<name>.yaml and
        modules/<module>/reactions/<name>/<name>.md. A module file is
        modules/<module>/module.yaml; an aggregate file is
        modules/<module>/aggregates/<aggregate>/aggregate.yaml. A use-case file starts
        under modules/<module>/use-cases/<name>/<name>.yaml; move the folder
        under its aggregate's use-cases/ folder once the use case names one. The
        title is one Title Case title, written to the YAML title and to the
        explanation H1. For an article type
        (research or decision) or for a type on a layer listed in
        explanations.layers, the command also writes the explanation file beside
        the record, from the template for its type (templates/consumer/<type>.md),
        with the title filled in and the fixed sections of the type. The command
        refuses an identifier a record already carries.

        new requirement <feature>.<name> writes a requirement file of its own in its
        feature's requirements/ folder: intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.yaml. The
        feature record <feature> must exist; the command refuses an unknown feature.
        A requirement takes no explanation file, because its statement is its lead;
        add <name>.md beside it only when the requirement needs an example.

        new decision writes a decision file with the next free number from the
        one decision sequence across the product and technical decisions.
        <name> is the kebab-case name without the number, such as
        receipt-threshold; the full identifier is decision/<number>-<name>.
        Before it writes, it prints the provisions whose statement matches
        the name's words, so a choice the standard already makes is followed
        instead of recorded. The command refuses a name that already carries
        a number.

        new <document-style> writes the .md document from its template: a tutorial
        or how-to under guide/, a command or configuration under tools/, a reference
        under reference/, an index as the README.md of its section folder,
        a glossary or example cast under behavior/.
        The title follows <name>. The command refuses a name whose file already exists.

        new result and new scenario append one entry to a use-case file's
        records instead of scaffolding a file. The entry lands in house order,
        successes first, then refusals in precedence order, then scenarios, so a
        new refusal follows the existing refusals and a new scenario lands last;
        the file is written back in canonical form. The entry id is
        result/<use-case>.<name> or scenario/<use-case>.<name> from the use case's
        own name. --code is required for a refusal and carries <module>.<reason>;
        on a success it becomes the first of codes. The command refuses a duplicate
        id and an unknown use case.

        new entry appends one rule, permission, or setting entry to its owning list file's records
        instead of scaffolding a file. <page-id> is the file's consumer-relative path, because list
        files carry no record identifier. The type must belong on the file: a rule on a rule-set file,
        a permission on the permission list, a setting on a setting set. A requirement is a page of
        its own: write it with new requirement. The entry id is <type>/<home>.<name> from the file's
        home, or permission/<name>; the home is the file's existing entries' home, else the file stem
        for a rule and the parent modules folder for a setting. The entry lands last with every required field filled
        with its template example value; the file is written back in canonical
        form. The command refuses a duplicate id and an unknown file.

          --root    The consumer repository to write into. Defaults to the working directory.
          --code    The refusal code, for new result.
          --outcome success or refusal, for new result.
          --result  The scenario's result, for new scenario.
          --help    Print this text and exit.

        Exit codes: 0 written, 1 the release holds no readable template for the type, 2 the identifier is taken, the use case or file is unknown, the type is unknown or does not belong on the file, or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the type and identifier or the append form and its options.</param>
    /// <returns>The process exit code: 0 when the file or entry is written, 1 when no template or repository can be read, and 2 on invalid input or a taken identifier.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root", "--code", "--outcome", "--result"], maxPositionals: 4);

        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return CommandFailure.UsageError(parsed.Error, Usage, json: false);
        }

        if (parsed.Positionals is [var first, ..] && first is "result" or "scenario")
        {
            return AppendEntry.Run(parsed);
        }

        if (parsed.Positionals is ["entry", ..])
        {
            return AppendListEntry.Run(parsed);
        }

        if (parsed.Positionals is not [var type, var id])
        {
            return CommandFailure.UsageError("New needs a type and an id, or a document style and a name.", Usage, json: false);
        }

        foreach (var option in new[] { "--code", "--outcome", "--result" })
        {
            if (parsed.Value(option) is not null)
            {
                CliHost.Error.WriteLine($"Option {option} applies only to new result and new scenario.");
                return 2;
            }
        }

        if (!RecordTypes.All.Contains(type) && !RecordTypes.PageStyles.Contains(type))
        {
            CliHost.Error.WriteLine($"Unknown type '{type}'.");
            return 2;
        }

        if (RecordPages.DocumentStyles.Contains(type))
        {
            return RunDocument(parsed, type, id);
        }

        if (!RecordPages.PageTypes.Contains(type))
        {
            if (RecordPages.EntryHomes.TryGetValue(type, out var home))
            {
                CliHost.Error.WriteLine($"Type '{type}' lives as an entry in {home}; add the entry there instead of scaffolding a file.");
            }
            else
            {
                CliHost.Error.WriteLine($"Type '{type}' is not scaffolded; copy templates/consumer/{type}.yaml to its home.");
            }

            return 2;
        }

        var isDecision = type == "decision";
        string full;
        string? homeName;
        string name;

        if (!isDecision)
        {
            full = $"{type}/{id}";

            if (!Identifiers.TrySplit(full, out var prefix, out homeName, out name) || prefix != type)
            {
                CliHost.Error.WriteLine($"Identifier '{full}' is not a valid {type} identifier.");
                return 2;
            }

            if (type == "feature" && homeName is null)
            {
                return CommandFailure.UsageError("New feature needs a problem: use litespec new feature <problem>.<name>.", Usage, json: false);
            }

            if (type == RecordTypes.Requirement && homeName is null)
            {
                return CommandFailure.UsageError("New requirement needs a feature: use litespec new requirement <feature>.<name>.", Usage, json: false);
            }

            // A feature lives in its problem's folder, but its identifier carries no home:
            // the problem picks the folder and the identifier is feature/<name>.
            var identifierHome = homeName;

            if (type == "feature")
            {
                if (!Identifiers.ValidName(homeName!, false) || Identifiers.AllDigitsName(homeName!))
                {
                    CliHost.Error.WriteLine($"'{homeName}' is not a valid problem name.");
                    return 2;
                }

                full = $"feature/{name}";
                identifierHome = null;
            }

            if (RecordPages.GrammarError(type, identifierHome, name, full) is { } grammar)
            {
                CliHost.Error.WriteLine(grammar);
                return 2;
            }
        }
        else
        {
            if (HasDecisionNumber(id))
            {
                CliHost.Error.WriteLine("New decision takes a name without a number; the tool allocates the next number.");
                return 2;
            }

            if (!Identifiers.ValidName(id, false) || Identifiers.AllDigitsName(id))
            {
                CliHost.Error.WriteLine($"'{id}' is not a valid decision name: use kebab-case without the number.");
                return 2;
            }

            if (id.Split('-', StringSplitOptions.RemoveEmptyEntries).Length > 5)
            {
                CliHost.Error.WriteLine($"Decision name '{id}' is too long: use at most five words; the number is allocated.");
                return 2;
            }

            full = "";
            homeName = null;
            name = "";
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        IReadOnlyList<Record> records;

        try
        {
            records = RecordReader.Read(config).Records;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            CliHost.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        if (isDecision)
        {
            name = $"{NextDecisionNumber(records):D4}-{id}";
            full = $"decision/{name}";
            homeName = null;

            if (RecordPages.GrammarError(type, homeName, name, full) is { } composed)
            {
                CliHost.Error.WriteLine(composed);
                return 2;
            }
        }

        var holder = records.FirstOrDefault(record => record.Id == full);

        if (holder is not null)
        {
            CliHost.Error.WriteLine($"Identifier '{full}' is already taken by {PathText.Relative(config.Root, holder.File)}.");
            return 2;
        }

        // A requirement lives in its feature's folder, so the feature record must exist first.
        var requirementFolder = type == RecordTypes.Requirement ? RecordPages.FeatureFolder(records, homeName!) : null;

        if (type == RecordTypes.Requirement && requirementFolder is null)
        {
            CliHost.Error.WriteLine($"No feature '{homeName}' holds the requirement; write feature/{homeName} first with litespec new feature <problem>.{homeName}.");
            return 2;
        }

        var templateFile = $"{type}.yaml";

        if (ReadTemplate(templateFile) is not { } template)
        {
            return 1;
        }

        if (!RecordFile.TryParse(template, out _, out var tree, out var parseError))
        {
            CliHost.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{templateFile}: {parseError}");
            return 1;
        }

        if (RecordFile.NodeOf(tree) is not JsonObject node)
        {
            CliHost.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{templateFile}.");
            return 1;
        }

        // One title serves the YAML title and the explanation H1, so the two cannot differ. A decision's
        // title leaves out the number the tool allocates.
        var title = RecordTitle(isDecision ? id : name);

        node["id"] = full;

        if (node.ContainsKey("title"))
        {
            node["title"] = title;
        }

        var filled = RecordFile.Write(node);
        string? explanation = null;

        // An article type, and a type on a layer the project lists, gets the explanation template for its own
        // type, with the title filled in, so the record starts with its fixed sections. A requirement takes none:
        // its statement is its lead.
        if (type != RecordTypes.Requirement
            && (RecordFile.ArticleTypes.Contains(type)
                || (RecordTypes.LayerByType.TryGetValue(type, out var layer)
                    && config.ExplanationLayers.Contains(layer, StringComparer.Ordinal))))
        {
            if (ReadTemplate($"{type}.md") is not { } explanationTemplate)
            {
                return 1;
            }

            explanation = FillTitle(explanationTemplate, title);
        }

        var target = requirementFolder is null
            ? RecordPages.PagePath(config, type, homeName, name)
            : RecordPages.PlaceIn(requirementFolder, name, RecordFiles.Extension);

        if (File.Exists(target))
        {
            CliHost.Error.WriteLine($"'{PathText.Relative(config.Root, target)}' already exists; pick another name.");
            return 2;
        }

        var explanationTarget = explanation is null ? null : RecordFile.ExplanationPathOf(target);

        if (explanationTarget is not null && File.Exists(explanationTarget))
        {
            CliHost.Error.WriteLine($"'{PathText.Relative(config.Root, explanationTarget)}' already exists; pick another name.");
            return 2;
        }

        if (isDecision)
        {
            PrintMatchingProvisions(id);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, filled);

            if (explanation is not null)
            {
                File.WriteAllText(explanationTarget!, explanation);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to write '{PathText.Relative(config.Root, target)}': {exception.Message}");
            return 1;
        }

        CliHost.Out.WriteLine($"Wrote {PathText.Relative(config.Root, target)} with {full}.");

        if (explanationTarget is not null)
        {
            CliHost.Out.WriteLine($"Wrote {PathText.Relative(config.Root, explanationTarget)}.");
        }

        return 0;
    }

    /// <summary>Scaffolds one document from the template for its style, with the title following the name.</summary>
    /// <param name="parsed">The parsed command line, holding the document style, the name and the options.</param>
    /// <param name="style">The document style to scaffold.</param>
    /// <param name="name">The kebab-case name, which becomes the file stem or the section folder.</param>
    /// <returns>The process exit code: 0 when the document is written, 1 when no template or repository can be read, and 2 on invalid input or an existing file.</returns>
    static int RunDocument(CliArgs parsed, string style, string name)
    {
        if (!Identifiers.ValidName(name, false) || Identifiers.AllDigitsName(name))
        {
            CliHost.Error.WriteLine($"'{name}' is not a valid {style} name: use kebab-case.");
            return 2;
        }

        var config = ConsumerRoot.Open(parsed.Value("--root"), json: false, out var openExit);

        if (config is null)
        {
            return openExit;
        }

        var root = config.Root;
        var templateFile = $"{style}.md";

        if (ReadTemplate(templateFile) is not { } template)
        {
            return 1;
        }

        var filled = FillTitle(template, AppendEntry.TitleFrom(name));
        var target = RecordPages.DocumentPath(config, style, name);

        if (File.Exists(target))
        {
            CliHost.Error.WriteLine($"'{PathText.Relative(config.Root, target)}' already exists; pick another name.");
            return 2;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, filled);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"Failed to write '{PathText.Relative(config.Root, target)}': {exception.Message}");
            return 1;
        }

        CliHost.Out.WriteLine($"Wrote {PathText.Relative(config.Root, target)}.");
        return 0;
    }

    /// <summary>Whether the release the tool carries holds the template file.</summary>
    /// <param name="file">The template file name under <c>templates/consumer/</c>.</param>
    /// <returns>True when the template file exists in the release; otherwise false.</returns>
    static bool TemplateExists(string file)
    {
        try
        {
            return File.Exists(LiteSpecHome.PathTo($"templates/consumer/{file}"));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Reads one template from the release the tool carries, reporting an unreadable one.</summary>
    /// <param name="file">The template file name under <c>templates/consumer/</c>.</param>
    /// <returns>The template text, or null after reporting why the release holds no readable template.</returns>
    static string? ReadTemplate(string file)
    {
        try
        {
            var templatePath = LiteSpecHome.PathTo($"templates/consumer/{file}");

            if (!File.Exists(templatePath))
            {
                CliHost.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{file}.");
                return null;
            }

            return File.ReadAllText(templatePath);
        }
        catch (InvalidOperationException exception)
        {
            CliHost.Error.WriteLine(exception.Message);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            CliHost.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{file}: {exception.Message}");
            return null;
        }
    }

    /// <summary>
    ///     The one title a new record takes: its kebab-case name in Title Case, the first word capitalized and
    ///     the minor words lowercase, as the consumer templates carry it.
    /// </summary>
    /// <param name="name">The kebab-case name of the record, without the decision number.</param>
    /// <returns>The title, with its words separated by spaces.</returns>
    static string RecordTitle(string name)
    {
        var words = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return name;
        }

        for (var at = 0; at < words.Length; at++)
        {
            if (at > 0 && MinorTitleWords.Contains(words[at]))
            {
                continue;
            }

            words[at] = char.ToUpperInvariant(words[at][0]) + words[at][1..];
        }

        return string.Join(' ', words);
    }

    /// <summary>The words a Title Case title keeps lowercase unless they open it.</summary>
    static readonly HashSet<string> MinorTitleWords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "nor",
        "of", "on", "or", "the", "to", "vs", "with",
    };

    /// <summary>Whether the decision name already carries a number: four digits and a hyphen first.</summary>
    /// <param name="name">The name from the command line.</param>
    /// <returns>True when the name opens with four digits and a hyphen; otherwise false.</returns>
    static bool HasDecisionNumber(string name) =>
        name.Length > 5
        && char.IsAsciiDigit(name[0]) && char.IsAsciiDigit(name[1])
        && char.IsAsciiDigit(name[2]) && char.IsAsciiDigit(name[3])
        && name[4] == '-';

    /// <summary>The next free decision number: one past the highest number any decision carries.</summary>
    /// <param name="records">Every record the model read, holding the product and technical decisions.</param>
    /// <returns>One past the highest decision number in use, or 1 when no decision exists.</returns>
    static int NextDecisionNumber(IReadOnlyList<Record> records)
    {
        var max = 0;

        foreach (var record in records)
        {
            if (record.Type != "decision"
                || !Identifiers.TrySplit(record.Id, out var prefix, out _, out var recordName)
                || prefix != "decision")
            {
                continue;
            }

            var dash = recordName.IndexOf('-');

            if (dash > 0
                && int.TryParse(recordName.AsSpan(0, dash), out var number)
                && number > max)
            {
                max = number;
            }
        }

        return max + 1;
    }

    /// <summary>Prints the provisions whose statement matches the decision name's words, in identifier order, before the files are written.</summary>
    /// <param name="name">The kebab-case decision name from the command line, without the number.</param>
    static void PrintMatchingProvisions(string name)
    {
        var words = name.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length >= 3)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (words.Count == 0)
        {
            return;
        }

        var home = LiteSpecHome.Root;

        if (home is null || !Directory.Exists(Path.Combine(home, "docs")))
        {
            return;
        }

        var matches = Provisions.Collect(home)
            .Where(row => words.Any(word => StatementMatches(row.Statement, word)))
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        CliHost.Out.WriteLine($"Provisions matching '{name}':");

        foreach (var row in matches)
        {
            CliHost.Out.WriteLine($"- {row.Id}: {row.Statement}");
        }
    }

    /// <summary>Whether the statement matches the name word: it contains the word or, for a plural word, its singular.</summary>
    /// <param name="statement">The provision statement.</param>
    /// <param name="word">The lowercased name word of at least three letters.</param>
    /// <returns>True when the statement contains the word or its singular; otherwise false.</returns>
    static bool StatementMatches(string statement, string word) =>
        statement.Contains(word, StringComparison.OrdinalIgnoreCase)
        || (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal)
            && statement.Contains(word[..^1], StringComparison.OrdinalIgnoreCase));

    /// <summary>Fills the document template's frontmatter title and opening heading with the new title.</summary>
    /// <param name="template">The document template text.</param>
    /// <param name="title">The display title derived from the document name.</param>
    /// <returns>The template with its title line and opening heading carrying the new title.</returns>
    static string FillTitle(string template, string title)
    {
        var lines = template.Split('\n');
        var titleDone = false;
        var headingDone = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var end = lines[index].EndsWith('\r') ? "\r" : "";
            var bare = lines[index].TrimEnd('\r');

            if (!titleDone && bare.StartsWith("title: ", StringComparison.Ordinal))
            {
                lines[index] = $"title: {title}{end}";
                titleDone = true;
            }
            else if (!headingDone && bare.StartsWith("# ", StringComparison.Ordinal))
            {
                lines[index] = $"# {title}{end}";
                headingDone = true;
            }
        }

        return string.Join('\n', lines);
    }
}
