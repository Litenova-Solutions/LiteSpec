namespace LiteSpec.Model;

/// <summary>One mark as read from source.</summary>
sealed record CodeMark(string RecordId, MarkStyle Style, MarkVerb Verb, string File, int Line);
