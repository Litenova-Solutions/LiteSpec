using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the topic, reaction, job and exception record types and the link verbs are registered.</summary>
public sealed class RecordTypesTests
{
    /// <summary>The topic is an Intent record type, and the requirement page style is gone.</summary>
    /// <proves>litespec/core/intent#shape-each-topic-page</proves>
    [Fact]
    public void Topic_Is_An_Intent_Record_Type()
    {
        Assert.Contains("topic", RecordTypes.All);
        Assert.Equal("intent", RecordTypes.LayerByType["topic"]);
        Assert.DoesNotContain("requirement-page", RecordTypes.PageStyles);
    }

    /// <summary>Cites links the six reason types and the requirements a journey shows.</summary>
    /// <proves>litespec/core/intent#cite-the-reasons-of-every-requirement</proves>
    [Fact]
    public void Cites_Links_Reasons_And_Requirements()
    {
        Assert.True(RecordTypes.LinkTargets["cites"].SetEquals(
            new[] { "goal", "problem", "feature", "constraint", "research", "decision", "requirement" }));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("features"));
    }

    /// <summary>Addresses links the actors a page speaks to and the permission an event needs.</summary>
    /// <proves>litespec/core/language#name-each-field-by-its-word-class</proves>
    [Fact]
    public void Addresses_Links_Actors_And_Permissions()
    {
        Assert.True(RecordTypes.LinkTargets["addresses"].SetEquals(
            new[] { "actor", "permission" }));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("audience"));
    }

    /// <summary>The reaction and the job are Behavior record types with a home, and automation is gone.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Reactions_And_Jobs_Are_Behavior_Record_Types()
    {
        Assert.Contains("reaction", RecordTypes.All);
        Assert.Equal("behavior", RecordTypes.LayerByType["reaction"]);
        Assert.Contains("reaction", RecordTypes.WithHome);
        Assert.Contains("job", RecordTypes.All);
        Assert.Equal("behavior", RecordTypes.LayerByType["job"]);
        Assert.Contains("job", RecordTypes.WithHome);
        Assert.DoesNotContain("automation", RecordTypes.All);
    }

    /// <summary>The exception is a Governance record type, and deviation and known gap are gone.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Exception_Is_A_Governance_Record_Type()
    {
        Assert.Contains("exception", RecordTypes.All);
        Assert.Equal("governance", RecordTypes.LayerByType["exception"]);
        Assert.DoesNotContain("deviation", RecordTypes.All);
        Assert.DoesNotContain("known-gap", RecordTypes.All);
    }

    /// <summary>The effect verbs link the records each effect reaches, with no retired verb left.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Effect_Verbs_Link_Their_Targets()
    {
        Assert.True(RecordTypes.LinkTargets["runs"].SetEquals(new[] { "use-case" }));
        Assert.True(RecordTypes.LinkTargets["sends"].SetEquals(new[] { "notification" }));
        Assert.True(RecordTypes.LinkTargets["emits"].SetEquals(new[] { "integration-event" }));
        Assert.True(RecordTypes.LinkTargets["calls"].SetEquals(new[] { "external-system" }));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("issues"));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("publishes"));
    }

    /// <summary>The reaction trigger links the events that set it off, and the old trigger is gone.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void TriggeredBy_Links_Events()
    {
        Assert.True(RecordTypes.LinkTargets["triggeredBy"].SetEquals(
            new[] { "event", "integration-event" }));
        Assert.False(RecordTypes.LinkTargets.ContainsKey("on"));
    }

    /// <summary>The structured objects link through their containers to the records they name.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Object_Links_Resolve_Through_Their_Containers()
    {
        Assert.True(RecordTypes.LinkTargets["compensation"].SetEquals(new[] { "use-case" }));
        Assert.Empty(RecordTypes.LinkTargets["freshness"]);
        Assert.True(RecordTypes.LinkTargets["erasure"].SetEquals(
            new[] { "external-system", "integration-event" }));
        Assert.True(RecordTypes.LinkTargets["enforcedBy"].SetEquals(new[] { "reaction", "job" }));
        Assert.True(RecordTypes.LinkTargets["dataClasses"].SetEquals(
            new[] { "reaction", "job", "decision", "requirement" }));
    }

    /// <summary>The link sets match the Written on columns of the Links table.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void Link_Sets_Match_The_Links_Table()
    {
        var rows = LinksRows(File.ReadAllText(Path.Combine(FindRepository(), "docs", "core", "model.md")));

        var fulfills = rows.Where(row => row.Field == "fulfills").ToList();
        Assert.Single(fulfills);
        Assert.Equal(
            WritersOf(fulfills[0]),
            RecordTypes.FulfillingTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());

        var steps = rows
            .Where(row => row.Field == "serves"
                && row.PointsAt.Contains("journey step", StringComparison.OrdinalIgnoreCase)
                && !row.WrittenOn.Contains("screen", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(steps);
        Assert.Equal(
            WritersOf(steps[0]),
            RecordTypes.StepServingTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());

        var linked = RecordTypes.FulfillingTypes
            .Concat(RecordTypes.StepServingTypes)
            .Distinct(StringComparer.Ordinal)
            .Where(type => type != "rule")
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(linked, RecordTypes.UpLinkedTypes.OrderBy(type => type, StringComparer.Ordinal).ToList());
    }

    /// <summary>One Links table row: the field, who writes it, and what it points at.</summary>
    sealed record LinksRow(string Field, string WrittenOn, string PointsAt);

    /// <summary>Every row of the Links table on the model page.</summary>
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

            if (line.StartsWith("|:"))
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
    static IReadOnlyList<string> WritersOf(LinksRow row) =>
        row.WrittenOn.Split(',')
            .Select(part => part.Trim())
            .Select(part => part.StartsWith("and ", StringComparison.OrdinalIgnoreCase) ? part[4..].TrimStart() : part)
            .Where(part => part.Length > 0)
            .Select(part => part.Replace(' ', '-').ToLowerInvariant())
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToList();

    /// <summary>The LiteSpec repository root, found above the test binary.</summary>
    static string FindRepository()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "litespec.manifest.json")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("The test binary sits outside a LiteSpec checkout.");
    }
}
