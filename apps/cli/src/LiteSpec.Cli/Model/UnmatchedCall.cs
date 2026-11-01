namespace LiteSpec.Model;

/// <summary>One screen operation that resolves to no backend endpoint.</summary>
sealed record UnmatchedCall(string Screen, string Operation, string File);
