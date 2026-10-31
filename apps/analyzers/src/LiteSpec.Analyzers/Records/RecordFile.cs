namespace LiteSpec.Analyzers.Records;

/// <summary>
///     One Markdown file handed to the record index: its path and its text.
/// </summary>
public sealed class RecordFile
{
    /// <summary>Initializes a new instance of the <see cref="RecordFile"/> class.Creates a record file.</summary>
    /// <param name="path">The file path, as the host reports it.</param>
    /// <param name="text">The file text.</param>
    public RecordFile(string path, string text)
    {
        Path = path;
        Text = text;
    }

    /// <summary>Gets the file path, as the host reports it.</summary>
    public string Path { get; }

    /// <summary>Gets the file text.</summary>
    public string Text { get; }
}
