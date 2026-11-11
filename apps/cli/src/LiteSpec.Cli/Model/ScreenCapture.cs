namespace LiteSpec.Cli.Model;

/// <summary>One screenshot capture of a screen route: its file, theme, width and capture date.</summary>
/// <param name="Path">The capture file, relative to the consumer root with forward slashes.</param>
/// <param name="Theme">The capture theme the file name names, such as light or dark.</param>
/// <param name="Width">The capture width in pixels the file name names.</param>
/// <param name="CapturedOn">The capture date, the file's last-write date in UTC, as yyyy-MM-dd.</param>
sealed record ScreenCapture(string Path, string Theme, int Width, string CapturedOn);
