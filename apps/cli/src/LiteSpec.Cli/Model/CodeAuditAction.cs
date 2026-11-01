namespace LiteSpec.Model;

/// <summary>One audit action literal a command message declares in its audit attribute.</summary>
sealed record CodeAuditAction(string Operation, string Action, string File, int Line);
