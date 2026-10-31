using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec new</c> command: scaffolds one record page from the
///     template for its type, with the identifier filled in and reserved.
/// </summary>
/// <follows>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</follows>
static partial class NewCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec new <type> <id> [--root <path>] [--help]
               litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
               litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
               litespec new entry <page-id> <type> <name> [--root <path>]

        Creates one record page from the template for its type, with the opening
        specification metadata block already filled. It reserves the identifier
        before any prose exists, so two authors working in parallel never mint
        the same identifier twice. <type> is the record type, such as topic,
        use-case, or decision. <id> is the home.name part of the identifier, such
        as lending.notify-reader; the full identifier is <type>/<id>. The page is
        written at the home its type owns: a topic under docs/intent/, a reaction
        under modules/<module>/reactions/, a job under modules/<module>/jobs/,
        a notification under modules/<module>/notifications/, a decision under
        the decisions folder. A use-case page starts under reads/ beside its
        module; move the file beside its aggregate once the use case names one.
        A topic page starts directly under docs/intent/; move it into its area
        folder. The command refuses an identifier a record already carries.

        new result and new scenario append one entry to a use-case page's
        frontmatter records instead of scaffolding a page. The entry lands in
        house order, successes first, then refusals in precedence order, then
        scenarios, so a new refusal follows the existing refusals and a new
        scenario lands last; every other byte of the frontmatter stays
        untouched. The entry id is result/<use-case>.<name> or
        scenario/<use-case>.<name> from the use case's own name. --code is
        required for a refusal and carries <module>.<reason>; on a success it
        becomes the first of codes. The command refuses a duplicate id and an
        unknown use case.

        new entry appends one requirement, rule, permission, or setting entry
        to its owning list page's frontmatter records instead of scaffolding a
        page. <page-id> is the page's consumer-relative path, because list
        pages carry no record identifier. The type must belong on the page: a
        requirement on a topic page, a rule on a rule-set page, a
        permission on the permission list, a setting on a setting set. The
        entry id is <type>/<home>.<name> from the page's home, or
        permission/<name>; the home is the page's existing entries' home, else
        the file stem for a requirement or rule and the parent modules folder
        for a setting. The entry lands last with every required field filled
        with its template placeholder; every other byte of the page stays
        untouched. The command refuses a duplicate id and an unknown page.

          --root    The consumer repository to write into. Defaults to the working directory.
          --code    The refusal code, for new result.
          --outcome success or refusal, for new result.
          --result  The scenario's result, for new scenario.
          --help    Print this text and exit.

        Exit codes: 0 written, 1 the release holds no readable template for the type, 2 the identifier is taken, the use case or page is unknown, the type is unknown or does not belong on the page, or an option is invalid.
        """;

    /// <summary>Runs the command and returns its exit code.</summary>
    /// <param name="args">The command arguments after the command name, holding the type and identifier or the append form and its options.</param>
    /// <returns>The process exit code: 0 when the page or entry is written, 1 when no template or repository can be read, and 2 on invalid input or a taken identifier.</returns>
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
            Console.Error.WriteLine("New needs a type and an id.");
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

        if (!RecordPages.PageTypes.Contains(type))
        {
            if (RecordPages.EntryHomes.TryGetValue(type, out var home))
            {
                Console.Error.WriteLine($"Type '{type}' lives as an entry in {home}; add the entry there instead of scaffolding a page.");
            }
            else
            {
                Console.Error.WriteLine($"Type '{type}' is a page style without its own identifier; copy templates/consumer/{type}.md to its home.");
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
            root = Path.GetFullPath(parsed.Value("--root") ?? Directory.GetCurrentDirectory());
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
            Console.Error.WriteLine($"litespec.project.json is not valid JSON: {exception.Message}");
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

        // The product template carries the page style name; every other record
        // template carries the record type name.
        var templateFile = type == "product" ? "product-brief.md" : $"{type}.md";
        string template;

        try
        {
            var templatePath = LiteSpecHome.PathTo($"templates/consumer/{templateFile}");

            if (!File.Exists(templatePath))
            {
                Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{templateFile}.");
                return 1;
            }

            template = File.ReadAllText(templatePath);
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"The LiteSpec release holds no readable templates/consumer/{templateFile}: {exception.Message}");
            return 1;
        }

        var filled = IdPattern().Replace(template, _ => $"\"id\": \"{full}\"", 1);
        var target = RecordPages.PagePath(config, type, homeName, name);

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

        Console.WriteLine($"Wrote {config.Relative(target)} with {full}.");
        return 0;
    }

    /// <summary>The page's identifier line, the first <c>"id"</c> of the template.</summary>
    [GeneratedRegex(@"""id""\s*:\s*""[^""]*""")]
    private static partial Regex IdPattern();
}
