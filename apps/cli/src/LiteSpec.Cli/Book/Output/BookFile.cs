namespace LiteSpec.Cli.Book.Output;

/// <summary>One file of the book: its path inside the book with forward slashes, its bytes and its media type.</summary>
/// <param name="Path">The path inside the book, such as <c>data/manifest.js</c>.</param>
/// <param name="Bytes">The file's bytes, held in memory.</param>
/// <param name="ContentType">The media type the live server sends with the file.</param>
sealed record BookFile(string Path, byte[] Bytes, string ContentType)
{
    /// <summary>A book file whose media type follows from its extension.</summary>
    /// <param name="path">The path inside the book.</param>
    /// <param name="bytes">The file's bytes.</param>
    /// <returns>The book file.</returns>
    public static BookFile Of(string path, byte[] bytes) => new(path, bytes, MediaType(path));

    /// <summary>The media type of a book path, from its extension.</summary>
    /// <param name="path">The path inside the book.</param>
    /// <returns>The media type; an unknown extension is sent as an opaque byte stream.</returns>
    static string MediaType(string path) =>
        System.IO.Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
}
