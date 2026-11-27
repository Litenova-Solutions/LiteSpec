using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Sync;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Sync;

/// <summary>Proves each sync renderer writes the record facts in its language.</summary>
public sealed class SyncRendererTests
{
    /// <summary>Refusal codes render as a C# class with one documented constant per refusal, sorted by code.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void RefusalCodesRenderAsADocumentedConstantClass()
    {
        var entries = new List<RefusalEntry>
        {
            new("lending", "lending.loan-not-active", "The loan is already returned.", "refusals.yaml", "loans"),
            new("lending", "lending.copy-not-available", "The copy is on loan.", "refusals.yaml", "loans"),
        };

        var text = RefusalCodesRenderer.RenderDotnet("lending", entries, "Library.Lending");

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.DoesNotContain("litespec generate", text);
        Assert.Contains("namespace Library.Lending;", text);
        Assert.Contains("public static class LendingRefusalCodes", text);
        Assert.Contains("public const string CopyNotAvailable = \"lending.copy-not-available\";", text);
        Assert.Contains("/// <summary>The copy is on loan.</summary>", text);
        Assert.True(
            text.IndexOf("CopyNotAvailable", StringComparison.Ordinal) < text.IndexOf("LoanNotActive", StringComparison.Ordinal),
            "Refusals order by code.");
    }

    /// <summary>Refusal codes render as TypeScript with a const array of codes and their union type.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void RefusalCodesRenderAsATypeScriptArray()
    {
        var entries = new List<RefusalEntry>
        {
            new("lending", "lending.loan-not-active", "The loan is already returned.", "refusals.yaml", "loans"),
            new("lending", "lending.copy-not-available", "The copy is on loan.", "refusals.yaml", "loans"),
        };

        var text = RefusalCodesRenderer.RenderTypeScript(entries);

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.Contains("\"lending.copy-not-available\",", text);
        Assert.Contains("export type RefusalCode = (typeof refusals)[number];", text);
        Assert.True(
            text.IndexOf("lending.copy-not-available", StringComparison.Ordinal) < text.IndexOf("lending.loan-not-active", StringComparison.Ordinal),
            "Refusals order by code.");
    }

    /// <summary>Permissions render as a C# class with one documented constant per permission id, sorted by id.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void PermissionsRenderAsADocumentedConstantClass()
    {
        using var temp = TestCli.Temp("sync");
        var read = TestModel.Record(
            "permission", "permission/borrow-copy", Path.Combine(temp.Path, "permissions.yaml"),
            """{"meaning": "Borrow an available copy.", "grantedTo": ["role/reader"]}""");
        var manage = TestModel.Record(
            "permission", "permission/manage-copies", Path.Combine(temp.Path, "permissions.yaml"),
            """{"meaning": "Register and retire copies.", "grantedTo": ["role/librarian"]}""");

        var text = PermissionsRenderer.RenderDotnet("lending", [manage, read], "Library.Lending");

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.Contains("public static class LendingPermissions", text);
        Assert.Contains("public const string BorrowCopy = \"permission/borrow-copy\";", text);
        Assert.Contains("/// <summary>Borrow an available copy.</summary>", text);
        Assert.True(
            text.IndexOf("BorrowCopy", StringComparison.Ordinal) < text.IndexOf("ManageCopies", StringComparison.Ordinal),
            "Permissions order by id.");
    }

    /// <summary>Permissions render as TypeScript with the ids and their union type.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void PermissionsRenderAsTypeScript()
    {
        using var temp = TestCli.Temp("sync");
        var read = TestModel.Record(
            "permission", "permission/borrow-copy", Path.Combine(temp.Path, "permissions.yaml"),
            """{"meaning": "Borrow an available copy.", "grantedTo": ["role/reader"]}""");

        var text = PermissionsRenderer.RenderTypeScript([("lending", read)]);

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.Contains("LendingBorrowCopy: \"permission/borrow-copy\",", text);
        Assert.Contains("export type PermissionId = (typeof permissions)[keyof typeof permissions];", text);
    }

    /// <summary>Screen capabilities render as TypeScript with one entry per route holding its permissions.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void CapabilitiesRenderRoutesToPermissions()
    {
        var screens = new List<CapabilitiesRenderer.ScreenCapability>
        {
            new("/returns", []),
            new("/loans", ["permission/borrow-copy", "permission/read-loans"]),
        };

        var text = CapabilitiesRenderer.RenderTypeScript(screens);

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.Contains("import type { PermissionId } from \"./permissions\";", text);
        Assert.Contains("export const screenCapabilities: Record<string, readonly PermissionId[]> = {", text);
        Assert.Contains("\"/loans\": [\"permission/borrow-copy\", \"permission/read-loans\"],", text);
        Assert.Contains("\"/returns\": [],", text);
        Assert.True(
            text.IndexOf("/loans", StringComparison.Ordinal) < text.IndexOf("/returns", StringComparison.Ordinal),
            "Routes order alphabetically.");
    }

    /// <summary>Event decisions render as a C# table with one constant per event holding its outcome word, and an identifier dictionary.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void EventDecisionsRenderAsATable()
    {
        var events = new List<EventDecisionsRenderer.EventDecision>
        {
            new("domain-event/loan.loan-borrowed", "loan", "loan-borrowed", DomainEventOutcome.Ignored),
            new("domain-event/loan.loan-returned", "loan", "loan-returned", DomainEventOutcome.Consumed),
            new("domain-event/loan.loan-overdue", "loan", "loan-overdue", DomainEventOutcome.Missing),
        };

        var text = EventDecisionsRenderer.RenderDotnet("lending", events, "Library.Lending");

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.Contains("public static class LendingEventDecisions", text);
        Assert.Contains("public const string LoanLoanBorrowed = \"ignored\";", text);
        Assert.Contains("public const string LoanLoanReturned = \"consumed\";", text);
        Assert.Contains("public const string LoanLoanOverdue = \"missing\";", text);
        Assert.Contains("[\"domain-event/loan.loan-returned\"] = LoanLoanReturned,", text);
        Assert.Contains("IReadOnlyDictionary<string, string> Decisions", text);
        Assert.DoesNotContain("translated", text, StringComparison.Ordinal);
        Assert.DoesNotContain("undecided", text, StringComparison.Ordinal);
    }

    /// <summary>The shared derivation reads the ignored flag first, then consumers, and reports neither as missing.</summary>
    /// <proves>litespec/core/outbox#decide-every-domain-event-outcome</proves>
    [Fact]
    public void EventDecisionOutcomesDeriveFromTheIgnoredFlagAndConsumers()
    {
        Assert.Equal(DomainEventOutcome.Ignored, DomainEventOutcomes.FromRecord(ignored: true, consumed: false));
        Assert.Equal(DomainEventOutcome.Ignored, DomainEventOutcomes.FromRecord(ignored: true, consumed: true));
        Assert.Equal(DomainEventOutcome.Consumed, DomainEventOutcomes.FromRecord(ignored: false, consumed: true));
        Assert.Equal(DomainEventOutcome.Missing, DomainEventOutcomes.FromRecord(ignored: false, consumed: false));

        Assert.Equal("ignored", DomainEventOutcomes.Value(DomainEventOutcome.Ignored));
        Assert.Equal("consumed", DomainEventOutcomes.Value(DomainEventOutcome.Consumed));
        Assert.Equal("missing", DomainEventOutcomes.Value(DomainEventOutcome.Missing));
    }

    /// <summary>Each Claimwick domain event renders the outcome the outbox decision check derives from its record.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void RenderedClaimwickDecisionsEqualTheCheckDerivation()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var model = ModelBuilder.Build(ConsumerConfig.Load(root));
        var consumers = RecordEntries.DomainEventConsumers(model);
        var plan = SyncPlan.Build(model);
        var file = Assert.Single(
            plan.Files,
            candidate => candidate.AbsolutePath.EndsWith("SpendingEventDecisions.cs", StringComparison.Ordinal));
        var values = DecisionValues(file.Content);
        var events = model.Records
            .Where(record => record.Type == RecordTypes.DomainEvent && !record.Derived)
            .ToList();

        Assert.NotEmpty(events);
        Assert.Equal(events.Count, values.Count);

        foreach (var record in events)
        {
            var consumed = consumers.TryGetValue(record.Id, out var named) && named.Count > 0;
            var expected = DomainEventOutcomes.Value(DomainEventOutcomes.FromRecord(record.Flag(RecordFields.Ignored), consumed));

            Assert.True(values.TryGetValue(record.Id, out var rendered), $"The table holds no decision for '{record.Id}'.");
            Assert.Equal(expected, rendered);
        }

        Assert.Equal("ignored", values["domain-event/expense.approved"]);
        Assert.Equal("ignored", values["domain-event/expense.refused"]);
        Assert.Equal("ignored", values["domain-event/receipt.attached"]);
        Assert.Equal("consumed", values["domain-event/expense.submitted"]);
        Assert.Equal("consumed", values["domain-event/expense.reimbursed"]);
        Assert.Equal("consumed", values["domain-event/expense.personal-data-erased"]);
    }

    /// <summary>Closed sets render as nested C# classes with sanitized constant names.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void ClosedSetsRenderAsNestedClasses()
    {
        var sets = new List<ClosedSetsRenderer.ClosedSet>
        {
            new("reference-data/lending.loan-kind", "loan-kind", ["standard", "reference-only", "48-hour"]),
        };

        var text = ClosedSetsRenderer.RenderDotnet("lending", sets, "Library.Lending");

        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", text);
        Assert.Contains("public static class LendingClosedSets", text);
        Assert.Contains("public static class LoanKind", text);
        Assert.Contains("public const string Standard = \"standard\";", text);
        Assert.Contains("public const string ReferenceOnly = \"reference-only\";", text);
        Assert.Contains("public const string _48Hour = \"48-hour\";", text);
    }

    /// <summary>A closed-set value with no word reads as Value, and a repeated name takes a numeric suffix.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void ClosedSetNamesStayValidAndUnique()
    {
        Assert.Equal("Value", SyncNaming.SanitizeConstName("---"));

        var taken = new HashSet<string>(StringComparer.Ordinal);

        Assert.Equal("AB", SyncNaming.UniqueName("AB", taken));
        Assert.Equal("AB2", SyncNaming.UniqueName("AB", taken));
    }

    /// <summary>Module placeholders resolve both spellings, and literals escape quotes and backslashes.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void NamingResolvesPlaceholdersAndEscapesLiterals()
    {
        Assert.Equal("Library.Lending", SyncNaming.ResolveModule("Library.{Module}", "lending"));
        Assert.Equal("apps/lending", SyncNaming.ResolveModule("apps/{module}", "lending"));
        Assert.Equal("say \\\"hi\\\" \\\\ ok", SyncNaming.EscapeLiteral("say \"hi\" \\ ok"));
        Assert.Equal("a &amp; &lt;b&gt;", SyncNaming.EscapeXml("a & <b>"));
    }

    /// <summary>Reads the rendered decision word by event id from the dictionary entries of a synced table.</summary>
    /// <param name="text">The synced table text.</param>
    /// <returns>The decision word by event identifier.</returns>
    static Dictionary<string, string> DecisionValues(string text)
    {
        var constants = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("public const string ", StringComparison.Ordinal)
                && trimmed.EndsWith(';'))
            {
                var rest = trimmed["public const string ".Length..^1].Split(" = ");
                constants[rest[0]] = rest[1].Trim('"');
            }
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("[\"", StringComparison.Ordinal) && trimmed.EndsWith(','))
            {
                var id = trimmed[2..trimmed.IndexOf("\"]", StringComparison.Ordinal)];
                var constant = trimmed[(trimmed.IndexOf("] = ", StringComparison.Ordinal) + 4)..^1];

                Assert.True(constants.TryGetValue(constant, out var value), $"The table holds no constant '{constant}'.");
                values[id] = value;
            }
        }

        return values;
    }
}
