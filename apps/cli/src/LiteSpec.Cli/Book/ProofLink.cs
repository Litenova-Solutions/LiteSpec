using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>One proves link enriched with its test, if the scenario names one.</summary>
/// <param name="Scenario">The proving scenario name.</param>
/// <param name="Confirmed">Whether the proof claim holds.</param>
/// <param name="Indirect">Whether the proof is indirect.</param>
/// <param name="Passing">The scenario's last outcome, or null without a test run.</param>
/// <param name="Test">The scenario's test, or null when no test has that name.</param>
sealed record ProofLink(JsonNode? Scenario, bool Confirmed, bool Indirect, bool? Passing, JsonObject? Test);
