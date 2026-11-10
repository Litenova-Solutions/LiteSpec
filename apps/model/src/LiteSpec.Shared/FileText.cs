using System.Text;

namespace LiteSpec;

/// <summary>
///     Reads and writes the text of files the way every LiteSpec tool does: UTF-8 text,
///     a byte order mark removed where a reader needs it gone, and forward slashes in
///     every path a message or a record shows.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class FileText
{
    /// <summary>Reads a file as UTF-8 text. A byte order mark stays in the text; <see cref="TrimBom"/> removes it.</summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The file text.</returns>
    public static string ReadText(string file) => Encoding.UTF8.GetString(File.ReadAllBytes(file));

    /// <summary>Removes a leading byte order mark from text, when it has one.</summary>
    /// <param name="text">The text to trim.</param>
    /// <returns>The text without its leading byte order mark.</returns>
    public static string TrimBom(string text) => text.StartsWith('﻿') ? text[1..] : text;

    /// <summary>Writes a path with forward slashes, the form every message and record uses.</summary>
    /// <param name="value">The path to write.</param>
    /// <returns>The path with each backslash replaced by a forward slash.</returns>
    public static string Slash(string value) => value.Replace('\\', '/');
}
