using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using LiteSpec.Cli.Book.Lexicon;
using LiteSpec.Cli.Book.Live;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.CommandLine;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Book;

/// <summary>
///     The <c>litespec book</c> command: serves the project book live from memory on localhost,
///     or writes it as a static site to a folder with <c>--out</c>.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookCommand
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec book [--root <folder>] [--out <folder>] [--port <port>] [--links <template>]

        Options:
          --root <folder>       The consumer root to read. Defaults to the consumer found from the working directory.
          --out <folder>        Write the static book to this folder and exit. Without it the book is served live on localhost.
          --port <port>         The localhost port the live book listens on. Defaults to 7420. Live only.
          --links <template>    The source link template with {root}, {path}, {line} and {commit}, or "none". Live defaults to vscode://file/{root}/{path}:{line}; --out defaults to none.
          --help                Print this text and exit.

        Exit codes: 0 written, or the live book stopped; 1 unreadable input, unwritable output or a port that cannot be opened; 2 usage error, a missing carried release, or a missing or broken lexicon page.
        """;

    /// <summary>The localhost port the live book listens on when <c>--port</c> is not given.</summary>
    public const int DefaultPort = 7420;

    /// <summary>Runs the command with the arguments after <c>book</c>; returns the exit code.</summary>
    /// <param name="args">The command arguments after the command name.</param>
    /// <returns>The process exit code.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The book command is a last-resort command handler that reports any release lookup failure as an error message and an exit code instead of crashing.")]
    public static int Run(string[] args)
    {
        var parsed = ParseArgs(args);
        if (parsed.Help)
        {
            CliHost.Out.WriteLine(Usage);
            return 0;
        }

        if (parsed.Error is not null)
        {
            return CommandFailure.UsageError(parsed.Error, Usage, json: false);
        }

        var config = ConsumerRoot.Open(parsed.Root, json: false, out var openExit);
        if (config is null)
        {
            return openExit;
        }

        string liteSpecRoot;
        try
        {
            liteSpecRoot = LiteSpecHome.RequireRoot();
        }
        catch (Exception exception)
        {
            return CommandFailure.Fail($"Cannot find the carried LiteSpec release: {exception.Message}", json: false, code: 2);
        }

        BookLexicon lexicon;
        try
        {
            lexicon = BookLexicon.Load(liteSpecRoot);
        }
        catch (BookLexiconException exception)
        {
            return CommandFailure.Fail($"Cannot load the book words from '{exception.File}': {exception.Message}", json: false, code: 2);
        }

        var live = parsed.Out is null;
        var links = parsed.Links switch
        {
            null => live ? BookBuild.LiveLinks : null,
            "none" => null,
            var template => template,
        };

        BookSiteBuild build;
        try
        {
            build = BookBuild.Run(config, lexicon, links);
        }
        catch (BookBuildException exception)
        {
            return CommandFailure.Fail(exception.Message, json: false, code: exception.Code);
        }

        if (!live)
        {
            return WriteStatic(build, parsed.Out!);
        }

        return Serve(config, lexicon, links, build, parsed.Port ?? DefaultPort);
    }

    /// <summary>Writes the built book to a folder and reports the summary line.</summary>
    /// <param name="build">The built book.</param>
    /// <param name="outDir">The folder to write the static book to.</param>
    /// <returns>The exit code: 0 when written, 1 when the folder cannot be written.</returns>
    static int WriteStatic(BookSiteBuild build, string outDir)
    {
        try
        {
            build.Site.WriteTo(outDir);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return CommandFailure.Fail($"Cannot write to '{outDir}': {exception.Message}", json: false, code: 1);
        }

        CliHost.Out.Write(BookSiteBuilder.Summary(build, outDir) + "\n");
        return 0;
    }

    /// <summary>Serves the live book until Ctrl+C, rebuilding it in the background as the project changes.</summary>
    /// <param name="config">The consumer configuration the first build used.</param>
    /// <param name="lexicon">The book lexicon read from the carried release.</param>
    /// <param name="links">The source link template, or null for no source links.</param>
    /// <param name="first">The first good build.</param>
    /// <param name="port">The localhost port to listen on.</param>
    /// <returns>The exit code: 0 when the book is stopped, 1 when the port cannot be listened on.</returns>
    static int Serve(ConsumerConfig config, BookLexicon lexicon, string? links, BookSiteBuild first, int port)
    {
        using var live = new BookLive(config.Root, lexicon, links);
        live.Start(first, config);

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stop.Cancel();
        };

        Console.CancelKeyPress += onCancel;
        try
        {
            BookLiveServer.ServeAsync(port, live, CliHost.Out, stop.Token).GetAwaiter().GetResult();
            return 0;
        }
        catch (HttpListenerException exception)
        {
            return CommandFailure.Fail($"Cannot serve the book on port {port}: {exception.Message}", json: false, code: 1);
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    /// <summary>The parsed book command arguments: the help flag, the usage error, and the values, each null when absent.</summary>
    /// <param name="Help">Whether the help flag was given.</param>
    /// <param name="Error">The usage error, or null.</param>
    /// <param name="Out">The static book folder, or null for the live book.</param>
    /// <param name="Root">The consumer root, or null for the working directory.</param>
    /// <param name="Links">The source link template, "none", or null for the mode's default.</param>
    /// <param name="Port">The live port, or null for the default.</param>
    sealed record Parsed(bool Help, string? Error, string? Out, string? Root, string? Links, int? Port);

    /// <summary>Parses the book command arguments into their named values and any usage error.</summary>
    /// <param name="argv">The command arguments after the command name.</param>
    /// <returns>The parsed arguments, with the help flag or the usage error set when the input calls for it.</returns>
    static Parsed ParseArgs(string[] argv)
    {
        var parsed = CliArgs.Parse(argv, [], ["--out", "--root", "--links", "--port"]);
        if (parsed.Help)
        {
            return new Parsed(true, null, null, null, null, null);
        }

        if (parsed.Error is not null)
        {
            return new Parsed(false, parsed.Error, null, null, null, null);
        }

        var outDir = parsed.Value("--out");
        var links = parsed.Value("--links");
        var portText = parsed.Value("--port");

        if (links is not null && links != "none" && !links.Contains("{path}", StringComparison.Ordinal))
        {
            return new Parsed(false, "Option --links needs a template with {path} and {line}, or none.", null, null, null, null);
        }

        int? port = null;
        if (portText is not null)
        {
            if (outDir is not null)
            {
                return new Parsed(false, "Option --port applies to the live book only; drop it with --out.", null, null, null, null);
            }

            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number is < 1 or > 65535)
            {
                return new Parsed(false, "Option --port needs a port number from 1 to 65535.", null, null, null, null);
            }

            port = number;
        }

        return new Parsed(false, null, outDir, parsed.Value("--root"), links, port);
    }
}
