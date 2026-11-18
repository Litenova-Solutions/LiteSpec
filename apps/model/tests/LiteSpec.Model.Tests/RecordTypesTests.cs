using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the reaction, job and waiver record types and the link verbs are registered.</summary>
public sealed class RecordTypesTests
{
    /// <summary>The topic record type and the review report page style are gone.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void TopicAndReviewReportAreGone()
    {
        Assert.DoesNotContain("topic", RecordTypes.All);
        Assert.DoesNotContain("topic", RecordTypes.LayerByType.Keys);
        Assert.DoesNotContain("review-report", RecordTypes.PageStyles);
        Assert.DoesNotContain("requirement-page", RecordTypes.PageStyles);
    }

    /// <summary>Cites links the seven reason types and the requirements a flow shows.</summary>
    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void CitesLinksReasonsAndRequirements()
    {
        Assert.True(RecordTypes.LinkTargets["cites"].SetEquals(
            ["goal", "direction", "problem", "feature", "constraint", "research", "decision", "requirement"]));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("features"));
    }

    /// <summary>Audience links the actors a page speaks to, and requires links actors and permissions.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact]
    public void AudienceAndRequiresLinkActorsAndPermissions()
    {
        Assert.True(RecordTypes.LinkTargets["audience"].SetEquals(
            ["actor"]));
        Assert.True(RecordTypes.LinkTargets["requires"].SetEquals(
            ["actor", "permission"]));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("addresses"));
    }

    /// <summary>The reaction and the job are Behavior record types with a home, and automation is gone.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void ReactionsAndJobsAreBehaviorRecordTypes()
    {
        Assert.Contains("reaction", RecordTypes.All);
        Assert.Equal("behavior", RecordTypes.LayerByType["reaction"]);
        Assert.Contains("reaction", RecordTypes.WithHome);
        Assert.Contains("job", RecordTypes.All);
        Assert.Equal("behavior", RecordTypes.LayerByType["job"]);
        Assert.Contains("job", RecordTypes.WithHome);
        Assert.DoesNotContain("automation", RecordTypes.All);
    }

    /// <summary>The waiver is a Governance record type, and deviation and known gap are gone.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void WaiverIsAGovernanceRecordType()
    {
        Assert.Contains("waiver", RecordTypes.All);
        Assert.Equal("governance", RecordTypes.LayerByType["waiver"]);
        Assert.DoesNotContain("deviation", RecordTypes.All);
        Assert.DoesNotContain("known-gap", RecordTypes.All);
    }

    /// <summary>The effect verbs link the records each effect reaches, and an event publish is no verb.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    /// <proves>litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates</proves>
    [Fact]
    public void EffectVerbsLinkTheirTargets()
    {
        Assert.True(RecordTypes.LinkTargets["runs"].SetEquals(["use-case"]));
        Assert.True(RecordTypes.LinkTargets["sends"].SetEquals(["notification"]));
        Assert.True(RecordTypes.LinkTargets["calls"].SetEquals(["external-system"]));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("emits"));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("issues"));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("publishes"));
    }

    /// <summary>A read model links the aggregate it is keyed by, the integration events it takes fields from and removes rows on, and the use cases that read it.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void ReadModelLinksItsSourcesAndReaders()
    {
        Assert.Contains("read-model", RecordTypes.All);
        Assert.Contains("read-model", RecordTypes.WithHome);
        Assert.Equal("behavior", RecordTypes.LayerByType["read-model"]);
        Assert.True(RecordTypes.LinkTargets["keyedBy"].SetEquals(["aggregate"]));
        Assert.True(RecordTypes.LinkTargets["from"].SetEquals(["module-event", "integration-event"]));
        Assert.True(RecordTypes.LinkTargets["erasedBy"].SetEquals(["module-event", "integration-event"]));
        Assert.True(RecordTypes.LinkTargets["removedBy"].SetEquals(["module-event", "integration-event"]));
        Assert.True(RecordTypes.LinkTargets["reads"].SetEquals(["aggregate", "setting", "read-model", "reference-data"]));
    }

    /// <summary>A read model is a technical type that code implements, and its record home is its module's read-models folder.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-read-model-in-its-module-read-models-folder</proves>
    [Fact]
    public void ReadModelHomeIsTheModuleReadModelsFolder()
    {
        Assert.Contains("read-models", RecordFolders.BehaviorModuleFolders);
        Assert.Equal("behavior/modules/reporting/read-models/reimbursed-expense/reimbursed-expense.yaml", RecordFolders.ScaffoldPath("read-model", "reporting", "reimbursed-expense"));
        Assert.True(RecordFolders.Places("read-model", ["behavior", "modules", "reporting", "read-models", "reimbursed-expense"], "reimbursed-expense.yaml"));
    }

    /// <summary>The reaction trigger links the events that set it off, and the old trigger is gone.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void TriggeredByLinksEvents()
    {
        Assert.True(RecordTypes.LinkTargets["triggeredBy"].SetEquals(
            ["domain-event", "module-event", "integration-event"]));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("on"));
    }

    /// <summary>The structured objects link through their containers to the records they name.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void ObjectLinksResolveThroughTheirContainers()
    {
        Assert.Empty(RecordTypes.LinkTargets["freshness"]);
        Assert.True(RecordTypes.LinkTargets["erasure"].SetEquals(
            ["external-system", "module-event", "integration-event"]));
        Assert.True(RecordTypes.LinkTargets["enforcedBy"].SetEquals(["job", "use-case"]));
        Assert.True(RecordTypes.LinkTargets["dataClasses"].SetEquals(
            ["job", "use-case", "decision", "requirement"]));
    }

    /// <summary>BasedOn links the records a limit or a choice rests on, and the renamed fields are gone.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void BasedOnLinksLimitsAndChoices()
    {
        Assert.True(RecordTypes.LinkTargets["basedOn"].SetEquals(
            ["constraint", "research", "direction", "decision", "requirement"]));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("acceptedBy"));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("entryPoint"));
    }

    /// <summary>The link sets match the Written on columns of the Links table.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void LinkSetsMatchTheLinksTable()
    {
        var rows = LinksRows(File.ReadAllText(Path.Combine(FindRepository(), "docs", "core", "model.md")));

        var fulfills = rows.Where(row => row.Field == "fulfills").ToList();
        Assert.Single(fulfills);
        Assert.Equal(
            WritersOf(fulfills[0]),
            RecordTypes.FulfillingTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());

        var steps = rows
            .Where(row => row.Field == "serves"
                && row.PointsAt.Contains("flow step", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(steps);
        Assert.Contains("screen", WritersOf(steps[0]));
        Assert.Equal(
            WritersOf(steps[0]).Where(type => type != "screen").OrderBy(type => type, StringComparer.Ordinal).ToList(),
            RecordTypes.StepServingTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());

        var linked = RecordTypes.FulfillingTypes
            .Concat(RecordTypes.StepServingTypes)
            .Distinct(StringComparer.Ordinal)
            .Where(type => type != "rule" && type != "entry-point")
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(linked, RecordTypes.UpLinkedTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());
    }

    /// <summary>One Links table row: the field, who writes it, and what it points at.</summary>
    sealed record LinksRow(string Field, string WrittenOn, string PointsAt);

    /// <summary>Every row of the Links table on the model page.</summary>
    /// <param name="page">The markdown text of the model page.</param>
    /// <returns>Every row of the Links table on the page.</returns>
    static IReadOnlyList<LinksRow> LinksRows(string page)
    {
        var rows = new List<LinksRow>();
        var inTable = false;

        foreach (var line in page.Replace("\r\n", "\n").Split('\n'))
        {
            if (!inTable)
            {
                if (line.StartsWith("| Field | Written on |", StringComparison.Ordinal))
                {
                    inTable = true;
                }

                continue;
            }

            if (!line.StartsWith('|'))
            {
                break;
            }

            if (line.StartsWith("|:", StringComparison.Ordinal))
            {
                continue;
            }

            var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

            if (cells.Count >= 5)
            {
                rows.Add(new LinksRow(cells[0].Trim('`'), cells[1], cells[2]));
            }
        }

        return rows;
    }

    /// <summary>One Written on cell as record types: commas split, spaces become hyphens.</summary>
    /// <param name="row">The Links table row to read.</param>
    /// <returns>The record types that write the field, in ordinal order.</returns>
    static IReadOnlyList<string> WritersOf(LinksRow row) =>
        row.WrittenOn.Split(',')
            .Select(part => part.Trim())
            .Select(part => part.StartsWith("and ", StringComparison.OrdinalIgnoreCase) ? part[4..].TrimStart() : part)
            .Where(part => part.Length > 0)
            .Select(part => part.Replace(' ', '-').ToLowerInvariant())
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToList();

    /// <summary>The LiteSpec repository root, found above the test binary.</summary>
    /// <returns>The path of the repository root.</returns>
    static string FindRepository()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "litespec.manifest.yaml")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("The test binary sits outside a LiteSpec checkout.");
    }
}
