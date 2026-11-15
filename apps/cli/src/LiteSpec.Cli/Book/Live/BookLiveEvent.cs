namespace LiteSpec.Cli.Book.Live;

/// <summary>One change notification for the open page: the event name and its one-line data.</summary>
/// <param name="Name">The event name: building, built or failed.</param>
/// <param name="Data">The data line: the build time for built, the error for failed, empty for building.</param>
sealed record BookLiveEvent(string Name, string Data);
