using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The idempotency replay check: a keyed command's declaration replays the first result exactly when its record states returns-first-result.</summary>
/// <follows>litespec/core/use-cases#state-what-a-replay-does</follows>
/// <follows>litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</follows>
internal static class IdempotencyReplayChecks
{
    /// <summary>
    ///     Reports every keyed command whose idempotency declaration replays differently than its record states:
    ///     the record states replay returns-first-result while the declaration replays nothing, or the record
    ///     states another replay while the declaration replays the first result. Definitions carry no mark; each
    ///     use case's definition is found from the message type its handler handles, in the handler's own
    ///     Application folder. A use case without a handler, without a keyed idempotency, without a stated
    ///     replay, or without its definition file keeps no finding here: the record and definition checks own
    ///     those gaps. A definition file that does not parse, or that states no replay position, keeps none either.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each handler with its message type.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.UseCaseId.Length == 0 || handler.MessageType.Length == 0)
            {
                continue;
            }

            if (!model.ById.TryGetValue(handler.UseCaseId, out var record)
                || record.Type != RecordTypes.UseCase
                || record.Derived)
            {
                continue;
            }

            var folder = Path.GetDirectoryName(handler.File);

            if (folder is null)
            {
                continue;
            }

            CheckReplay(handler, record, folder, findings);
        }
    }

    /// <summary>Reports the keyed command whose declaration replays differently than its record states.</summary>
    /// <param name="handler">The handler whose message names the definition.</param>
    /// <param name="record">The use case record holding idempotency.</param>
    /// <param name="folder">The Application folder holding the handler and its definitions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckReplay(CodeHandler handler, Record record, string folder, List<Finding> findings)
    {
        if (IdempotencyOf(record) is not { } idempotency
            || ReplayOf(idempotency) is not { } replay)
        {
            return;
        }

        var message = handler.MessageType;
        var definition = message + "IdempotencyDefinition";
        var file = Path.Combine(folder, definition + ".cs");

        if (!File.Exists(file))
        {
            return;
        }

        string text;

        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var tree = CSharpSyntaxTree.ParseText(text);

        if (HasParseError(tree))
        {
            return;
        }

        if (IdempotencyReplays.Of(tree.GetRoot()) is not { } replays)
        {
            return;
        }

        if (replay == "returns-first-result" && !replays)
        {
            findings.Add(new Finding(
                "idempotency-replay-drift",
                $"Idempotency definition '{definition}' declares no replay of the first result, but '{record.Id}' states replay 'returns-first-result'.",
                file,
                1,
                record.Id));
        }
        else if (replay != "returns-first-result" && replays)
        {
            findings.Add(new Finding(
                "idempotency-replay-drift",
                $"Idempotency definition '{definition}' declares a replay of the first result, but '{record.Id}' states replay '{replay}'.",
                file,
                1,
                record.Id));
        }
    }

    /// <summary>Whether the syntax tree holds a parse error, so its shape cannot be trusted.</summary>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <returns>True when the tree holds an error diagnostic; otherwise false.</returns>
    private static bool HasParseError(SyntaxTree tree)
    {
        foreach (var diagnostic in tree.GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The keyed idempotency the use case record states, or null when it states none.</summary>
    /// <param name="record">The use case record.</param>
    /// <returns>The idempotency object, or null when the record states no keyed idempotency.</returns>
    private static JsonElement? IdempotencyOf(Record record)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Idempotency, out var idempotency)
            || idempotency.ValueKind != JsonValueKind.Object
            || !idempotency.TryGetProperty(RecordFields.Mode, out var mode)
            || mode.ValueKind != JsonValueKind.String
            || mode.GetString() != "key")
        {
            return null;
        }

        return idempotency;
    }

    /// <summary>The replay the keyed idempotency states, or null when it states none.</summary>
    /// <param name="idempotency">The keyed idempotency object.</param>
    /// <returns>The replay string, or null.</returns>
    private static string? ReplayOf(JsonElement idempotency)
    {
        if (!idempotency.TryGetProperty(RecordFields.Replay, out var replay)
            || replay.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return replay.GetString();
    }
}
