using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Writes a book in-process from a parsed model, through the same writer the command uses.</summary>
static class BookTestBooks
{
    /// <summary>Loads a frozen model fixture as a parsed model object.</summary>
    /// <param name="relative">The fixture path with forward-slash segments.</param>
    /// <returns>The parsed model object.</returns>
    public static JsonObject LoadFixture(string relative) =>
        (JsonObject)JsonNode.Parse(File.ReadAllText(Cli.Fixture(relative)))!;

    /// <summary>Writes the book for a parsed model to the folder, with the carried release's own words.</summary>
    /// <param name="model">The parsed model object that holds the records array.</param>
    /// <param name="root">The consumer root for source links and captures.</param>
    /// <param name="outDir">The folder to write the static site to.</param>
    /// <param name="editor">The editor link template, or null for the default template.</param>
    /// <param name="level">The conformance level the project claims.</param>
    /// <returns>The console line naming the output folder, the record count, the shard count and the unplaced ids.</returns>
    public static string Write(JsonObject model, string root, string outDir, string? editor = null, ConformanceLevel level = ConformanceLevel.Full) =>
        BookCommand.WriteBook(
            model,
            root,
            BookLexicon.Load(Cli.Repository),
            Cli.Repository,
            editor ?? BookCommand.DefaultEditor,
            outDir,
            level);
}
