namespace LiteSpec.Book;

/// <summary>A carried LiteSpec page the lexicon needs is missing or lost its shape.</summary>
sealed class BookLexiconException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="BookLexiconException"/> class.Builds the failure for the page at <paramref name="file"/>.</summary>
    /// <param name="file">The carried page that is missing or lost its shape.</param>
    /// <param name="message">What the page lacks.</param>
    public BookLexiconException(string file, string message)
        : base(message)
    {
        File = file;
    }

    /// <summary>Gets the carried page that is missing or lost its shape.</summary>
    public string File { get; }
}
