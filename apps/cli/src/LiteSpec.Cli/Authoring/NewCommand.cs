using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec new</c> command: scaffolds one record file from the
///     template for its type, with the identifier filled in and reserved, and
///     one document from the template for a document style.
/// </summary>
/// <follows>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</follows>
static class NewCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec new <type> <id> [--root <path>] [--help]
               litespec new <document-style> <name> [--root <path>]
               litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
               litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
               litespec new entry <page-id> <type> <name> [--root <path>]

        Creates one record file from the template for its type, with every
        required field already filled. It reserves the identifier before any
        prose exists, so two authors working in parallel never mint the same
        identifier twice. <type> is the record type, such as topic, use-case,
        or decision. <id> is the home.name part of the identifier, such as
        lending.notify-reader; the full identifier is <type>/<id>. The file is
        written at the home its type owns: a topic under .litespec/intent/, a reaction
        under modules/<module>/reactions/, a job under modules/<module>/jobs/,
        a notification under modules/<module>/notifications/, a decision under
        the decisions folder with its explanation file beside it. A module file
        is modules/<module>/module.yaml; an aggregate file is
        modules/<module>/<aggregate>/aggregate.yaml. A use-case file starts
        under reads/ beside its module; move the file beside its aggregate once
        the use case names one. A topic file starts directly under .litespec/intent/;
        move it into its area folder. For an article type (research, decision, or
        topic) the command also writes the explanation file from its explanation
        template. The command refuses an identifier a record already carries.

        new <document-style> writes the .md document from its template: a tutorial
        or how-to under guide/, a command or configuration under tools/, a reference
        under reference/, a review report under proof/reviews/, an index as the
        README.md of its section folder, a glossary or example cast under behavior/.
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

        new entry appends one requirement, rule, permission, or setting entry
        to its owning list file's records instead of scaffolding a file. <page-id>
        is the file's consumer-relative path, because list files carry no record
        identifier. The type must belong on the file: a requirement on a topic file,
        a rule on a rule-set file, a permission on the permission list, a setting
        on a setting set. The entry id is <type>/<home>.<name> from the file's home,
        or permission/<name>; the home is the file's existing entries' home, else
        the file stem for a requirement or rule and the parent modules folder
        for a setting. The entry lands last with every required field filled
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
            Console.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            Console.Error.WriteLine(parsed.Error);
            Console.Error.WriteLine(Usage);
            return 2;
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
            Console.Error.WriteLine("New needs a type and an id, or a document style and a name.");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        foreach (var option in new[] { "--code", "--outcome", "--result" })
        {
            if (parsed.Value(option) is not null)
            {
                Console.Error.WriteLine($"Option {option} applies only to new result and new scenario.");
                return 2;
            }
        }

        if (!RecordTypes.All.Contains(type) && !RecordTypes.PageStyles.Contains(type))
        {
            Console.Error.WriteLine($"Unknown type '{type}'.");
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
                Console.Error.WriteLine($"Type '{type}' lives as an entry in {home}; add the entry there instead of scaffolding a file.");
            }
            else
            {
                Console.Error.WriteLine($"Type '{type}' is not scaffolded; copy templates/consumer/{type}.yaml to its home.");
            }

            return 2;
        }

        var full = $"{type}/{id}";

        if (!Identifiers.TrySplit(full, out var prefix, out var homeName, out var name) || prefix != type)
        {
            Console.Error.WriteLine($"Identifier '{full}' is not a valid {type} identifier.");
            return 2;
        }

        if (RecordPages.GrammarError(type, homeName, name, full) is { } grammar)
        {
            Console.Error.WriteLine(grammar);
            return 2;
        }

        string root;

        try
        {
            root = ConsumerRoot.Resolve(parsed.Value("--root"));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root")}': {exception.Message}");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        ConsumerConfig config;
        IReadOnlyList<Record> records;

        try
        {
            config = ConsumerConfig.Load(root);
            records = RecordReader.Read(config).Records;
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($".litespec/project.json is not valid JSON: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            Console.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        var holder = records.FirstOrDefault(record => record.Id == full);

        if (holder is not null)
        {
            Console.Error.WriteLine($"Identifier '{full}' is already taken by {config.Relative(holder.File)}.");
            return 2;
        }

        var templateFile = $"{type}.yaml";

        if (ReadTemplate(templateFile) is not { } template)
        {
            return 1;
        }

        if (!RecordFile.TryParse(template, out _, out var tree, out var parseError))
        {
            Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{templateFile}: {parseError}");
            return 1;
        }

        if (JsonNode.Parse(tree.ToJson()) is not JsonObject node)
        {
            Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{templateFile}.");
            return 1;
        }

        node["id"] = full;

        var filled = RecordFile.Write(node);
        string? explanation = null;

        if (RecordFile.ArticleTypes.Contains(type))
        {
            var explanationFile = $"{type}.md";

            if (ReadTemplate(explanationFile) is not { } explanationTemplate)
            {
                return 1;
            }

            explanation = explanationTemplate;
        }

        var target = RecordPages.PagePath(config, type, homeName, name);

        if (File.Exists(target))
        {
            Console.Error.WriteLine($"'{config.Relative(target)}' already exists; pick another name.");
            return 2;
        }

        var explanationTarget = explanation is null ? null : RecordFile.ExplanationPathOf(target);

        if (explanationTarget is not null && File.Exists(explanationTarget))
        {
            Console.Error.WriteLine($"'{config.Relative(explanationTarget)}' already exists; pick another name.");
            return 2;
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
            Console.Error.WriteLine($"Failed to write '{config.Relative(target)}': {exception.Message}");
            return 1;
        }

        Console.WriteLine($"Wrote {config.Relative(target)} with {full}.");

        if (explanationTarget is not null)
        {
            Console.WriteLine($"Wrote {config.Relative(explanationTarget)}.");
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
            Console.Error.WriteLine($"'{name}' is not a valid {style} name: use kebab-case.");
            return 2;
        }

        string root;

        try
        {
            root = ConsumerRoot.Resolve(parsed.Value("--root"));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid root '{parsed.Value("--root")}': {exception.Message}");
            Console.Error.WriteLine(Usage);
            return 2;
        }

        ConsumerConfig config;

        try
        {
            config = ConsumerConfig.Load(root);
        }
        catch (JsonException exception)
        {
            Console.Error.WriteLine($".litespec/project.json is not valid JSON: {exception.Message}");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AggregateException)
        {
            Console.Error.WriteLine($"Failed to read the consumer repository: {exception.Message}");
            return 1;
        }

        var templateFile = $"{style}.md";

        if (ReadTemplate(templateFile) is not { } template)
        {
            return 1;
        }

        var filled = FillTitle(template, AppendEntry.TitleFrom(name));
        var target = RecordPages.DocumentPath(config, style, name);

        if (File.Exists(target))
        {
            Console.Error.WriteLine($"'{config.Relative(target)}' already exists; pick another name.");
            return 2;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, filled);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Failed to write '{config.Relative(target)}': {exception.Message}");
            return 1;
        }

        Console.WriteLine($"Wrote {config.Relative(target)}.");
        return 0;
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
                Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{file}.");
                return null;
            }

            return File.ReadAllText(templatePath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{file}: {exception.Message}");
            return null;
        }
    }

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
