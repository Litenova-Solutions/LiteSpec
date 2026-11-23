using LiteSpec.Cli.Model.Code;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Cli.Model.Readers.Dotnet.Backend;

/// <summary>Reads the outbox decision each domain event type carries into the model: one row per decision call.</summary>
/// <follows>litespec/ext/outbox#decide-every-domain-event-outcome</follows>
static class OutboxDecisionReader
{
    /// <summary>Reads the outbox decisions of one file into the model.</summary>
    /// <param name="file">The file.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="model">The code model the decisions add to.</param>
    public static void ReadOutboxDecisions(string file, SyntaxTree tree, CodeFacts model)
    {
        foreach (var decision in OutboxDecisions.Of(tree))
        {
            model.OutboxDecisions.Add(new CodeOutboxDecision(decision.EventType, file, decision.Line, decision.Ignored));
        }
    }
}
