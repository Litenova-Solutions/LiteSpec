namespace LiteSpec.Model;

/// <summary>One endpoint operation mapped to its use case.</summary>
sealed record EndpointOperation(string Operation, string UseCaseId, string Method, string Route, string File, int Line = 1);
