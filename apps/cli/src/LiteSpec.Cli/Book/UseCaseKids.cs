using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>The results and scenarios that belong to one use case.</summary>
/// <param name="Results">The use case's results.</param>
/// <param name="Scenarios">The use case's scenarios.</param>
sealed record UseCaseKids(IReadOnlyList<JsonObject> Results, IReadOnlyList<JsonObject> Scenarios);
