using System.Text.Json.Nodes;
using LiteSpec.Book;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Book;

/// <summary>Writes a book in-process from a parsed model, through the same writer the command uses.</summary>
static class BookTestBooks
{
    /// <summary>Loads a frozen model fixture as a parsed model object.</summary>
    /// <param name="relative">The fixture path with forward-slash segments.</param>
    /// <returns>The parsed model object.</returns>
    public static JsonObject LoadFixture(string relative) =>
        (JsonObject)DataFile.Read(TestCli.Fixture(relative))!;

    /// <summary>Writes the book for a parsed model to the folder, with the carried release's own words.</summary>
    /// <param name="model">The parsed model object that holds the records array.</param>
    /// <param name="root">The consumer root for source links and captures.</param>
    /// <param name="outDir">The folder to write the static site to.</param>
    /// <param name="editor">The editor link template, or null for the default template.</param>
    /// <param name="claims">The record layers the project claims, every layer with LiteSpec's practice by default.</param>
    /// <returns>The console line naming the output folder, the record count, the shard count and the unplaced ids.</returns>
    public static string Write(JsonObject model, string root, string outDir, string? editor = null, LayerClaims? claims = null) =>
        BookCommand.WriteBook(
            model,
            root,
            BookLexicon.Load(TestCli.Repository),
            TestCli.Repository,
            editor ?? BookCommand.DefaultEditor,
            outDir,
            claims ?? LayerClaims.All);
}
