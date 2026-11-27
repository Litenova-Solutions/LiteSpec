using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Lexicon;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.CompletionStages;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Consumer;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Book;

/// <summary>
///     Builds the whole book from one consumer: the model, the findings the model check takes
///     under the claimed layers, the Intent coverage, and the site. The live server and the
///     static export both call it, so they show the same book.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookBuild
{
    /// <summary>The source link template a live book uses when no <c>--links</c> is given.</summary>
    public const string LiveLinks = "vscode://file/{root}/{path}:{line}";

    /// <summary>Builds the book for a loaded consumer configuration.</summary>
    /// <param name="config">The consumer configuration, with the layer claims the book follows.</param>
    /// <param name="lexicon">The book lexicon read from the carried release.</param>
    /// <param name="linksTemplate">The source link template with {root}, {commit}, {path} and {line}, or null for no source links.</param>
    /// <returns>The built book.</returns>
    /// <exception cref="BookBuildException">The model cannot be built, or its checks stop the build.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A book build reports any model failure as one message: a live rebuild shows it and keeps the last good book, and a one-shot run returns it as an exit code.")]
    public static BookSiteBuild Run(ConsumerConfig config, BookLexicon lexicon, string? linksTemplate)
    {
        var root = config.Root;
        ProjectModel model;
        try
        {
            model = ModelBuilder.Build(config);
        }
        catch (Exception exception)
        {
            throw new BookBuildException($"Cannot build the model from '{root}': {exception.Message}", 1);
        }

        // The book takes the findings that model check takes, under the layer claims, so the two never disagree.
        List<Finding> findings;
        IReadOnlyList<Finding> covered;
        try
        {
            (findings, covered) = ModelCommand.Collect(model, root);
        }
        catch (FailFast fail)
        {
            throw new BookBuildException(fail.Message, fail.Code);
        }

        model.FindingsJson = new JsonArray([.. findings.Select(finding => (JsonNode?)ModelCommand.AsJson(config, finding, ModelCommand.Decide(model, config, finding, release: false)))]);
        model.CoveredJson = new JsonArray([.. covered.Select(finding => (JsonNode?)ModelCommand.AsCoveredJson(config, finding))]);

        if (JsonNode.Parse(model.ToJson()) is not JsonObject store || store["records"] is not JsonArray)
        {
            throw new BookBuildException("The built model holds no records array.", 1);
        }

        store["intentCoverage"] = BookIntentCoverage.ToJson(IntentCoverage.Compute(model));

        // The book groups decisions by the concerns the one classification gives
        // their followers, computed once here so the client reads one field.
        BookDecisionDetail.AddDecisionConcerns(store, config);

        var generatedOn = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        return BookSiteBuilder.Build(store, root, lexicon, FillLinks(linksTemplate, root, CommitOf(root)), config.Layers, generatedOn);
    }

    /// <summary>
    ///     Fills the {root} and {commit} placeholders of a source link template. The reader fills
    ///     {path} and {line} for each source, so both stay in the template.
    /// </summary>
    /// <param name="template">The template, or null for no source links.</param>
    /// <param name="root">The absolute consumer root.</param>
    /// <param name="commit">The checked-out commit.</param>
    /// <returns>The filled template, or null when the template is null.</returns>
    public static string? FillLinks(string? template, string root, string commit) =>
        template?.Replace("{root}", root, StringComparison.Ordinal).Replace("{commit}", commit, StringComparison.Ordinal);

    /// <summary>The checked-out commit of the consumer, or HEAD when the folder is not a git checkout.</summary>
    /// <param name="root">The consumer root.</param>
    /// <returns>The full commit identifier, or HEAD.</returns>
    static string CommitOf(string root)
    {
        var result = ProcessRunner.Git(root, "rev-parse", "HEAD");
        var commit = result.Status == 0 ? result.Stdout.Trim() : "";
        return commit.Length > 0 ? commit : "HEAD";
    }
}
