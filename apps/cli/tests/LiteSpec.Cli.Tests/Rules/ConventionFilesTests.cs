using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Rules;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves the project convention reader: identifier order, skipped files, scope forms and scope matching.</summary>
public sealed class ConventionFilesTests
{
    /// <summary>Load returns the conventions in identifier order and skips files without an identifier or default.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void LoadReturnsConventionsInIdentifierOrder()
    {
        using var temp = WriteConsumer();

        var entries = ConventionFiles.Load(ConsumerConfig.Load(temp.Path));

        Assert.Equal(
            ["demo/conventions#first-rule", "demo/conventions#second-rule"],
            entries.Select(entry => entry.Id).ToList());
    }

    /// <summary>LoadFiles reports each convention with its file and its Applies to line.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void LoadFilesReportsFileAndAppliesToLine()
    {
        using var temp = WriteConsumer();

        var pages = ConventionFiles.LoadFiles(ConsumerConfig.Load(temp.Path));

        Assert.Equal(2, pages.Count);
        Assert.Equal("a-first.md", Path.GetFileName(pages[0].File));
        Assert.Equal(1, pages[0].AppliesToLine);
        Assert.Equal("", pages[0].Entry.AppliesTo);
        Assert.Equal("b-second.md", Path.GetFileName(pages[1].File));
        Assert.Equal(7, pages[1].AppliesToLine);
        Assert.Equal("apps/api/src/**", pages[1].Entry.AppliesTo);
    }

    /// <summary>IsScopeForm takes the scope vocabulary provisions use.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void IsScopeFormTakesScopeVocabulary()
    {
        Assert.True(ConventionFiles.IsScopeForm("stack: dotnet, concern: application, artifact: command-handler"));
        Assert.True(ConventionFiles.IsScopeForm("artifact: command-handler"));
        Assert.True(ConventionFiles.IsScopeForm("artifact: command-handler, query-handler"));
        Assert.True(ConventionFiles.IsScopeForm("artifact: aggregate; layer: implementation; trace: yes"));
        Assert.True(ConventionFiles.IsScopeForm("artifact: record:use-case"));
        Assert.True(ConventionFiles.IsScopeForm("artifact: any"));
        Assert.True(ConventionFiles.IsScopeForm("stack: dotnet; artifact: command-handler"));
    }

    /// <summary>IsScopeForm rejects globs, free text and unknown keys.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void IsScopeFormRejectsOtherText()
    {
        Assert.False(ConventionFiles.IsScopeForm(""));
        Assert.False(ConventionFiles.IsScopeForm("   "));
        Assert.False(ConventionFiles.IsScopeForm("apps/api/src/**"));
        Assert.False(ConventionFiles.IsScopeForm("*.cs"));
        Assert.False(ConventionFiles.IsScopeForm("Confirm payouts"));
        Assert.False(ConventionFiles.IsScopeForm("scope: everywhere"));
        Assert.False(ConventionFiles.IsScopeForm("when: query"));
        Assert.False(ConventionFiles.IsScopeForm("artifact:"));
        Assert.False(ConventionFiles.IsScopeForm("stack dotnet"));
        Assert.False(ConventionFiles.IsScopeForm("apps/a,apps/b"));
        Assert.False(ConventionFiles.IsScopeForm(":"));
        Assert.False(ConventionFiles.IsScopeForm("artifact: command-handler; foo: bar"));
        Assert.False(ConventionFiles.IsScopeForm("hello, artifact: command-handler"));
    }

    /// <summary>IsGlobForm takes one path token.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void IsGlobFormTakesPathTokens()
    {
        Assert.True(ConventionFiles.IsGlobForm("apps/api/src/**"));
        Assert.True(ConventionFiles.IsGlobForm("*.cs"));
        Assert.True(ConventionFiles.IsGlobForm("Dockerfile"));
        Assert.True(ConventionFiles.IsGlobForm("behavior/entry-points/claimwick-app/screens/*"));
        Assert.True(ConventionFiles.IsGlobForm("sometime"));
    }

    /// <summary>IsGlobForm rejects scope forms, lists and free text.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void IsGlobFormRejectsOtherText()
    {
        Assert.False(ConventionFiles.IsGlobForm(""));
        Assert.False(ConventionFiles.IsGlobForm("   "));
        Assert.False(ConventionFiles.IsGlobForm("Confirm payouts"));
        Assert.False(ConventionFiles.IsGlobForm("stack: dotnet"));
        Assert.False(ConventionFiles.IsGlobForm("apps/a,apps/b"));
        Assert.False(ConventionFiles.IsGlobForm("apps/a;apps/b"));
    }

    /// <summary>MatchesScope matches when every stated field holds the target.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void MatchesScopeMatchesEveryStatedField()
    {
        Assert.True(ConventionFiles.MatchesScope(
            "stack: dotnet, concern: application, artifact: command-handler",
            "dotnet", "application", "command-handler", []));
        Assert.False(ConventionFiles.MatchesScope(
            "stack: dotnet, concern: application, artifact: command-handler",
            "dotnet", "application", "query-handler", []));
        Assert.False(ConventionFiles.MatchesScope(
            "stack: dotnet, concern: application, artifact: command-handler",
            "react", "application", "command-handler", []));
        Assert.False(ConventionFiles.MatchesScope(
            "stack: dotnet, concern: application, artifact: command-handler",
            "dotnet", "domain", "command-handler", []));
        Assert.True(ConventionFiles.MatchesScope("artifact: command-handler", "dotnet", "application", "command-handler", []));
        Assert.True(ConventionFiles.MatchesScope("artifact: command-handler, query-handler", "", "", "query-handler", []));
    }

    /// <summary>MatchesScope matches record values against the marked records and any against every target.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void MatchesScopeMatchesRecordsAndAny()
    {
        Assert.True(ConventionFiles.MatchesScope(
            "artifact: record:use-case", "", "records", "use-case", ["use-case/spending.submit-expense"]));
        Assert.False(ConventionFiles.MatchesScope("artifact: record:use-case", "", "records", "use-case", []));
        Assert.True(ConventionFiles.MatchesScope("artifact: any", "dotnet", "application", "command-handler", []));
        Assert.True(ConventionFiles.MatchesScope("artifact: any; concern: application", "dotnet", "application", "command-handler", []));
        Assert.False(ConventionFiles.MatchesScope("artifact: any; concern: domain", "dotnet", "application", "command-handler", []));
        Assert.False(ConventionFiles.MatchesScope("apps/api/src/**", "dotnet", "application", "command-handler", []));
        Assert.False(ConventionFiles.MatchesScope("", "dotnet", "application", "command-handler", []));
        Assert.True(ConventionFiles.MatchesScope("layer: implementation; trace: yes", "dotnet", "application", "command-handler", []));
    }

    static TempFolder WriteConsumer()
    {
        var temp = TestCli.Temp("rules");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {"project": {"name": "Demo"}, "owners": ["lending-team"],
                 "paths": {"docs": ".litespec", "glossary": ".litespec/glossary.yaml"}}
                """);
            Write(temp.Path, ".litespec/governance/conventions/b-second.md", """
                # Confirm Large Payouts

                **ID:** `demo/conventions#second-rule`

                **Strength:** default

                **Applies to:** `apps/api/src/**`

                **Default:** A payout above 1,000 euros asks for one confirmation step before it runs.

                **Rationale:** Large payouts are hard to reverse, and one confirmation stops most slips.

                **Reviewed for:** Which payouts above 1,000 euros run without a confirmation step? Look at the payout screens.
                """);
            Write(temp.Path, ".litespec/governance/conventions/a-first.md", """
                # Page Every Collection Read

                **ID:** `demo/conventions#first-rule`

                **Strength:** default

                **Default:** Every collection read returns one page shape.

                **Rationale:** One page shape keeps every list readable.

                **Reviewed for:** Which collection reads return another page shape? Look at the query handlers.
                """);
            Write(temp.Path, ".litespec/governance/conventions/no-id.md", """
                # Rule Without An Identifier

                **Strength:** default

                **Applies to:** `apps/api/src/**`

                **Default:** Every collection read returns one page shape.
                """);
            Write(temp.Path, ".litespec/governance/conventions/no-default.md", """
                # Rule Without A Default

                **ID:** `demo/conventions#no-default`

                **Strength:** default

                **Applies to:** `apps/api/src/**`
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
