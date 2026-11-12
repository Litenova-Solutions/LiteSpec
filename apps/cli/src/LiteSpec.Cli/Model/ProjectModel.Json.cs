using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Proof;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Tests;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The JSON form of the project model: every record with its computed links, completion and
///     proof, the read code and the test run, as the book reads it.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
sealed partial class ProjectModel
{
    /// <summary>Writes the model as JSON.</summary>
    /// <returns>The model serialized as indented JSON.</returns>
    public string ToJson()
    {
        var options = CliJson.Options;

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = options.Encoder }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", 1);
            writer.WriteString("root", ".");

            writer.WriteStartArray("records");

            var bodies = new Dictionary<(string File, string? PageId), string>(Records.Count);

            foreach (var page in Bodies)
            {
                // An explanation body joins its page record through the record file beside it.
                bodies[(RecordFile.RecordPathOf(page.File), page.PageId)] = PageText(page.Body);
            }

            foreach (var record in Records.OrderBy(left => left.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("type", record.Type);
                writer.WriteString("id", record.Id);
                writer.WriteString("folder", record.Folder);

                if (record.Module is { } module)
                {
                    writer.WriteString("module", module);
                }
                else
                {
                    writer.WriteNull("module");
                }

                writer.WriteString("title", record.Title);

                if (record.Derived)
                {
                    writer.WriteBoolean("derived", true);
                }

                if (record.Type == RecordTypes.UseCase)
                {
                    WriteNullable(writer, "aggregate", ModelBuilder.AggregateOf(this, record)?.Id);
                }

                if (record.Type == RecordTypes.Problem)
                {
                    writer.WriteString("evidenceStrength", Checks.Intent.IntentChecks.EvidenceStrengthOf(record));
                }

                writer.WriteString("file", PathText.Relative(Config.Root, record.File));
                writer.WriteNumber("line", record.Line);

                if (bodies.TryGetValue((record.File, record.Id), out var text))
                {
                    writer.WriteString("explanation", text);
                }
                writer.WritePropertyName("fields");
                record.Raw.WriteTo(writer);
                writer.WritePropertyName("links");
                CliJson.StringLists(ModelBuilder.ResolvedLinks(this, record)).WriteTo(writer, options);

                if (Completion.TryGetValue(record.Id, out var completion))
                {
                    writer.WriteStartObject("completion");
                    writer.WriteString("stage", completion.Stage);
                    writer.WriteString("proof", completion.Proof);
                    writer.WriteStartArray("heldBy");

                    foreach (var held in completion.HeldBy)
                    {
                        writer.WriteStringValue(held);
                    }

                    writer.WriteEndArray();
                    writer.WriteStartObject("counts");

                    foreach (var count in completion.Counts.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                    {
                        writer.WriteStartObject(count.Key);
                        writer.WriteNumber("done", count.Value.Done);
                        writer.WriteNumber("of", count.Value.Of);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                if (record.Type == RecordTypes.Screen)
                {
                    writer.WriteStartArray("captures");

                    foreach (var capture in Captures.GetValueOrDefault(record.Id) ?? [])
                    {
                        writer.WriteStartObject();
                        writer.WriteString("path", capture.Path);
                        writer.WriteString("theme", capture.Theme);
                        writer.WriteNumber("width", capture.Width);
                        writer.WriteString("capturedOn", capture.CapturedOn);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteString("captureState", CaptureState.GetValueOrDefault(record.Id) ?? "no-route");
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartObject("code");

            writer.WriteStartArray("handlers");

            foreach (var handler in Code.Handlers)
            {
                writer.WriteStartObject();
                writer.WriteString("useCase", handler.UseCaseId);
                writer.WriteString("operation", handler.Operation);
                writer.WriteString("kind", handler.Kind);
                writer.WriteString("path", PathText.Relative(Config.Root, handler.File));
                writer.WriteString("class", handler.Class);
                writer.WriteNumber("line", handler.Line);
                writer.WriteString("messageType", handler.MessageType);
                writer.WriteNumber("dependencyCount", handler.DependencyCount);
                writer.WriteStartArray("aggregateCalls");

                foreach (var call in handler.AggregateCalls ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("receiver", call.Receiver);
                    writer.WriteString("method", call.Method);
                    writer.WriteString("path", PathText.Relative(Config.Root, call.File));
                    writer.WriteNumber("line", call.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("collaborators");

                foreach (var collaborator in handler.Collaborators ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", collaborator.Type);
                    writer.WriteString("path", PathText.Relative(Config.Root, collaborator.File));
                    writer.WriteNumber("line", collaborator.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("loopReads");

                foreach (var read in handler.LoopReads ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("repository", read.Repository);
                    writer.WriteString("method", read.Method);
                    writer.WriteString("path", PathText.Relative(Config.Root, read.File));
                    writer.WriteNumber("line", read.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("memoryFilters");

                foreach (var filter in handler.MemoryFilters ?? [])
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", PathText.Relative(Config.Root, filter.File));
                    writer.WriteNumber("line", filter.Line);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("reactions");

            foreach (var reaction in Code.Reactions)
            {
                writer.WriteStartObject();
                writer.WriteString("class", reaction.Class);
                writer.WriteString("path", PathText.Relative(Config.Root, reaction.File));
                WriteNullable(writer, "useCase", reaction.UseCaseId);
                writer.WriteNumber("line", reaction.Line);
                writer.WriteNumber("dependencyCount", reaction.DependencyCount);
                WriteCollaborators(writer, reaction.Collaborators);
                WriteLoopReads(writer, reaction.LoopReads);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("jobs");

            foreach (var job in Code.Jobs)
            {
                writer.WriteStartObject();
                writer.WriteString("class", job.Class);
                writer.WriteString("path", PathText.Relative(Config.Root, job.File));
                WriteNullable(writer, "useCase", job.UseCaseId);
                writer.WriteNumber("line", job.Line);
                writer.WriteNumber("dependencyCount", job.DependencyCount);
                WriteCollaborators(writer, job.Collaborators);
                WriteLoopReads(writer, job.LoopReads);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("codes");

            foreach (var literal in Code.Literals.OrderBy(left => left.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("code", literal.Value);
                writer.WriteString("path", PathText.Relative(Config.Root, literal.File));
                WriteNullable(writer, "class", literal.Class);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("aggregateMethods");

            foreach (var method in Code.AggregateMethods)
            {
                writer.WriteStartObject();
                writer.WriteString("aggregate", method.Aggregate);
                writer.WriteString("method", method.Method);
                writer.WriteStartArray("events");

                foreach (var raised in method.Events)
                {
                    writer.WriteStringValue(raised);
                }

                writer.WriteEndArray();
                writer.WriteString("path", PathText.Relative(Config.Root, method.File));
                writer.WriteNumber("line", method.Line);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("auditActions");

            foreach (var action in Code.AuditActions)
            {
                writer.WriteStartObject();
                writer.WriteString("operation", action.Operation);
                writer.WriteString("action", action.Action);
                writer.WriteString("path", PathText.Relative(Config.Root, action.File));
                writer.WriteNumber("line", action.Line);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("endpoints");

            foreach (var endpoint in Code.Endpoints)
            {
                writer.WriteStartObject();
                writer.WriteString("operation", endpoint.Operation);
                writer.WriteString("useCase", endpoint.UseCaseId);
                writer.WriteString("method", endpoint.Method);
                writer.WriteString("route", endpoint.Route);
                writer.WriteString("path", PathText.Relative(Config.Root, endpoint.File));
                writer.WriteNumber("line", endpoint.Line);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("events");

            foreach (var domainEvent in Code.Events.OrderBy(left => left.Class, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("class", domainEvent.Class);
                writer.WriteString("path", PathText.Relative(Config.Root, domainEvent.File));
                WriteNullable(writer, "event", domainEvent.EventId);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteStartArray("screens");

            foreach (var screen in Screens)
            {
                writer.WriteStartObject();
                writer.WriteString("frontend", screen.Frontend);
                writer.WriteString("route", screen.Route);
                writer.WriteString("file", screen.File);
                WriteNullable(writer, "screen", screen.ScreenId);
                writer.WriteString("reader", screen.Reader);
                writer.WriteStartArray("operations");

                foreach (var operation in screen.Operations)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", operation.Name);
                    writer.WriteString("method", operation.Method);
                    writer.WriteString("path", operation.Path);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("useCases");

                foreach (var useCase in screen.UseCases)
                {
                    writer.WriteStringValue(useCase);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("calls");

                foreach (var call in screen.PairedCalls())
                {
                    writer.WriteStartObject();
                    writer.WriteString("operation", call.Operation);
                    writer.WriteString("useCase", call.UseCase);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                var screenCaptures = ScreenshotCaptures.ForRoute(this, screen.Route);

                writer.WriteStartArray("captures");

                foreach (var capture in screenCaptures)
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", capture.Path);
                    writer.WriteString("theme", capture.Theme);
                    writer.WriteNumber("width", capture.Width);
                    writer.WriteString("capturedOn", capture.CapturedOn);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteString(
                    "captureState",
                    screenCaptures.Count > 0 ? "captured" : "route-not-captured");
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("browserTests");

            foreach (var test in BrowserTests)
            {
                writer.WriteStartObject();
                writer.WriteString("frontend", test.Frontend);
                writer.WriteString("title", test.Title);
                writer.WriteString("file", PathText.Relative(Config.Root, test.File));
                writer.WriteNumber("line", test.Line);
                writer.WriteStartArray("proves");

                foreach (var claim in test.Proves)
                {
                    writer.WriteStringValue(claim);
                }

                writer.WriteEndArray();
                WriteNullable(writer, "journey", test.Journey);
                writer.WriteStartArray("gotos");

                foreach (var target in test.Gotos)
                {
                    writer.WriteStringValue(target);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("codes");

                foreach (var code in test.Codes)
                {
                    writer.WriteStringValue(code);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("exercisedSteps");

            foreach (var step in ExercisedSteps)
            {
                writer.WriteStringValue(step);
            }

            writer.WriteEndArray();

            writer.WriteStartArray("tests");

            foreach (var test in Tests)
            {
                writer.WriteStartObject();
                writer.WriteString("scenario", test.Name);
                writer.WriteString("class", test.Class);
                writer.WriteString("file", PathText.Relative(Config.Root, test.File));
                writer.WriteNumber("line", test.Line);
                writer.WriteStartArray("claims");

                foreach (var claim in test.Claims)
                {
                    writer.WriteStringValue(claim);
                }

                writer.WriteEndArray();
                writer.WriteStartArray("steps");

                foreach (var step in test.Steps)
                {
                    writer.WriteStartObject();
                    writer.WriteString("phase", step.Phase);
                    writer.WriteString("receiver", step.Receiver);
                    WriteNullable(writer, "receiverType", step.ReceiverType);
                    writer.WriteString("method", step.Method);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("codes");

                foreach (var code in test.Codes)
                {
                    writer.WriteStringValue(code);
                }

                writer.WriteEndArray();

                if (TestRun is not null && TestRun.OutcomeByMethod.TryGetValue(test.Name, out var outcome))
                {
                    writer.WriteString("outcome", outcome);
                }
                else
                {
                    writer.WriteNull("outcome");
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            if (TestRun is null)
            {
                writer.WriteNull("testRun");
            }
            else
            {
                writer.WriteStartObject("testRun");
                writer.WriteStartArray("files");

                foreach (var file in TestRun.Files)
                {
                    writer.WriteStringValue(file);
                }

                writer.WriteEndArray();
                writer.WriteNumber("total", TestRun.Total);
                writer.WriteNumber("passed", TestRun.Passed);
                writer.WriteNumber("failed", TestRun.Failed);
                writer.WriteString("date", TestRun.Date);
                writer.WriteEndObject();
            }

            writer.WriteStartObject("links");

            writer.WriteStartArray("implements");

            foreach (var link in Implements)
            {
                writer.WriteStartObject();
                writer.WriteString("path", link.Path);
                writer.WriteString("to", link.To);
                writer.WriteString("kind", link.Kind);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("follows");

            foreach (var link in Follows)
            {
                writer.WriteStartObject();
                writer.WriteString("path", link.Path);
                writer.WriteString("to", link.To);
                writer.WriteString("kind", link.Kind);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("calls");

            foreach (var link in Calls)
            {
                writer.WriteStartObject();
                writer.WriteString("screen", link.Screen);
                writer.WriteString("to", link.To);
                writer.WriteString("operation", link.Operation);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("proves");

            foreach (var link in Proves)
            {
                writer.WriteStartObject();
                writer.WriteString("scenario", link.Scenario);
                writer.WriteString("claim", link.Claim);
                writer.WriteBoolean("confirmed", link.Confirmed);

                if (link.Passing is null)
                {
                    writer.WriteNull("passing");
                }
                else
                {
                    writer.WriteBoolean("passing", link.Passing.Value);
                }

                writer.WriteBoolean("indirect", link.Indirect);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteStartObject("reachableStates");

            foreach (var entry in ReachableStates.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray(entry.Key);

                foreach (var state in entry.Value.OrderBy(left => left, StringComparer.Ordinal))
                {
                    writer.WriteStringValue(state);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();

            writer.WriteStartObject("constituents");

            foreach (var entry in Constituents.OrderBy(left => left.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray(entry.Key);

                foreach (var constituent in entry.Value.OrderBy(left => left, StringComparer.Ordinal))
                {
                    writer.WriteStringValue(constituent);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();

            writer.WriteStartArray("measures");

            foreach (var measure in Measures)
            {
                writer.WriteStartObject();
                writer.WriteString("tool", measure.Tool);
                writer.WriteString("check", measure.Check);
                writer.WriteBoolean("passed", measure.Passed);
                WriteNullable(writer, "value", measure.Value);
                writer.WriteString("source", PathText.Relative(Config.Root, measure.Source));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("unmatchedCalls");

            foreach (var call in UnmatchedCalls)
            {
                writer.WriteStartObject();
                writer.WriteString("screen", call.Screen);
                writer.WriteString("operation", call.Operation);
                writer.WriteString("file", call.File);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WritePropertyName("findings");

            if (FindingsJson is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                FindingsJson.WriteTo(writer, options);
            }

            writer.WritePropertyName("covered");

            if (CoveredJson is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                CoveredJson.WriteTo(writer, options);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>The page text the book shows: the body without its H1 title, trimmed.</summary>
    /// <param name="body">The raw page body, with any line-ending form.</param>
    /// <returns>The body without its H1 title, trimmed, with LF line endings.</returns>
    private static string PageText(string body)
    {
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var first = 0;

        while (first < lines.Length && lines[first].Trim().Length == 0)
        {
            first++;
        }

        if (first < lines.Length && IsTitle(lines[first]))
        {
            first++;
        }

        return string.Join('\n', lines[first..]).Trim();
    }

    /// <summary>Tests whether a body line is the H1 title.</summary>
    /// <param name="line">The body line to test.</param>
    /// <returns>True when the trimmed line is an H1 heading.</returns>
    private static bool IsTitle(string line)
    {
        var text = line.TrimStart();

        return text.StartsWith("# ", StringComparison.Ordinal) || text == "#";
    }

    /// <summary>Writes the collaborators a reaction or job builds with <c>new</c>.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="collaborators">The collaborators to write, or null for an empty array.</param>
    private void WriteCollaborators(Utf8JsonWriter writer, IReadOnlyList<CodeCollaborator>? collaborators)
    {
        writer.WriteStartArray("collaborators");

        foreach (var collaborator in collaborators ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("type", collaborator.Type);
            writer.WriteString("path", PathText.Relative(Config.Root, collaborator.File));
            writer.WriteNumber("line", collaborator.Line);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Writes the repository reads a reaction or job makes inside a loop.</summary>
    /// <param name="writer">The JSON writer to write the array into.</param>
    /// <param name="reads">The loop reads to write, or null for an empty array.</param>
    private void WriteLoopReads(Utf8JsonWriter writer, IReadOnlyList<CodeLoopRead>? reads)
    {
        writer.WriteStartArray("loopReads");

        foreach (var read in reads ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("repository", read.Repository);
            writer.WriteString("method", read.Method);
            writer.WriteString("path", PathText.Relative(Config.Root, read.File));
            writer.WriteNumber("line", read.Line);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Writes one string property, or a JSON null when the value is null.</summary>
    /// <param name="writer">The JSON writer to write the property into.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value, or null.</param>
    private static void WriteNullable(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }
}
