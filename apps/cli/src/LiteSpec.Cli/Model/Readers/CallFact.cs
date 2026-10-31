namespace LiteSpec.Model.Readers;

/// <summary>One generated-client call a route reaches: the operation name as the client exports it.</summary>
/// <param name="Operation">The operation name, as the client exports it.</param>
/// <param name="File">The file holding the call, relative to the frontend root with forward slashes.</param>
/// <param name="Line">The 1-based line of the call.</param>
/// <follows>litespec/frontend/structure#mark-each-route-with-its-screen</follows>
sealed record CallFact(string Operation, string File, int Line);
