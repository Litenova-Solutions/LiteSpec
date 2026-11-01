namespace LiteSpec.Model;

/// <summary>One call a handler makes on an aggregate root it holds.</summary>
sealed record CodeCall(string Receiver, string Method, string File, int Line);
