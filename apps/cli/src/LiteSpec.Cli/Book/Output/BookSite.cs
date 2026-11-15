using System.Diagnostics.CodeAnalysis;

namespace LiteSpec.Cli.Book.Output;

/// <summary>
///     The whole book held in memory: the shell, the app assets, the screen captures and the
///     data shards, each under its path inside the book. The live server answers from it and
///     the static export writes it to a folder, so nothing reads the disk after the build.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
sealed class BookSite
{
    /// <summary>The files by path, in path order.</summary>
    readonly SortedDictionary<string, BookFile> files = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="BookSite" /> class that holds the given files; a later file at the same path replaces an earlier one.</summary>
    /// <param name="items">The book files.</param>
    public BookSite(IEnumerable<BookFile> items)
    {
        foreach (var file in items)
        {
            files[file.Path] = file;
        }
    }

    /// <summary>Gets the number of files in the book.</summary>
    public int Count => files.Count;

    /// <summary>Gets the files in path order.</summary>
    public IEnumerable<BookFile> Files => files.Values;

    /// <summary>Finds the file at a path inside the book; the path must match a file exactly.</summary>
    /// <param name="path">The path inside the book, with forward slashes and no leading slash.</param>
    /// <param name="file">The file, when the book holds one at that path.</param>
    /// <returns>Whether the book holds a file at the path.</returns>
    public bool TryGet(string path, [NotNullWhen(true)] out BookFile? file)
    {
        if (files.TryGetValue(path, out var found))
        {
            file = found;
            return true;
        }

        file = null;
        return false;
    }

    /// <summary>Writes every file under a folder, creating the folders each path needs.</summary>
    /// <param name="outDir">The folder the book is written to.</param>
    public void WriteTo(string outDir)
    {
        foreach (var file in files.Values)
        {
            var target = Path.Combine(outDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
            var folder = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllBytes(target, file.Bytes);
        }
    }
}
