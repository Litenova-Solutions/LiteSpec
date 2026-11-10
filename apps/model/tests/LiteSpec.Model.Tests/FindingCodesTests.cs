using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the finding code table holds every code the checkers emit and nothing else, so an unknown or dead code fails the build.</summary>
public sealed class FindingCodesTests
{
    /// <summary>Every mark finding code has a tier.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Mark_Codes_Have_Tiers()
    {
        var missing = MarkRules.FindingCodes
            .Where(code => !FindingCodes.IsKnown(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }

    /// <summary>Every code the checkers emit in source has a tier.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Emitted_Codes_Have_Tiers()
    {
        var emitted = EmittedCodes(FindRepository());
        var missing = emitted
            .Where(code => !FindingCodes.IsKnown(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }

    /// <summary>Every code in the table is emitted by a check, so the table holds no dead code.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Table_Codes_Are_Emitted()
    {
        var emitted = QuotedLiterals(FindRepository());
        emitted.UnionWith(MarkRules.FindingCodes);
        var dead = FindingCodes.All
            .Select(row => row.Code)
            .Where(code => !emitted.Contains(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(dead);
    }

    /// <summary>Every row of the table names its level, meaning and fix, and no code appears twice.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Table_Rows_Are_Complete_And_Unique()
    {
        Assert.All(FindingCodes.All, row =>
        {
            Assert.Contains(row.Level, new[] { "G", "S", "A" });
            Assert.False(string.IsNullOrWhiteSpace(row.Meaning), row.Code);
            Assert.False(string.IsNullOrWhiteSpace(row.Fix), row.Code);
        });
        Assert.Equal(FindingCodes.All.Count, FindingCodes.All.Select(row => row.Code).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Each new Intent finding code carries its contracted tier.</summary>
    /// <param name="code">The finding code under test.</param>
    /// <param name="tier">The tier the code must carry.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData("requirement-cites-missing", "A")]
    [InlineData("constraint-unreached", "G")]
    public void New_Intent_Codes_Have_Tiers(string code, string tier)
    {
        Assert.Equal(tier, FindingCodes.Of(code).Level);
    }

    /// <summary>Each new YAML finding code carries its contracted tier.</summary>
    /// <param name="code">The finding code under test.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData("explanation-orphaned")]
    [InlineData("explanation-missing")]
    [InlineData("format-drift")]
    [InlineData("source-link-bare")]
    [InlineData("source-key-unknown")]
    [InlineData("source-unused")]
    public void New_Yaml_Codes_Are_Advisory(string code)
    {
        Assert.Equal("A", FindingCodes.Of(code).Level);
    }

    /// <summary>Each new Behavior finding code carries its contracted tier.</summary>
    /// <param name="code">The finding code under test.</param>
    /// <param name="tier">The tier the code must carry.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData("job-name-invalid", "G")]
    [InlineData("reaction-trigger-unknown", "G")]
    [InlineData("job-guard-missing", "G")]
    [InlineData("reaction-unimplemented", "S")]
    [InlineData("job-unimplemented", "S")]
    [InlineData("atomic-group-drift", "G")]
    [InlineData("atomic-group-invalid", "A")]
    public void New_Behavior_Codes_Have_Tiers(string code, string tier)
    {
        Assert.Equal(tier, FindingCodes.Of(code).Level);
    }

    /// <summary>Each new Proof and Governance finding code carries its contracted tier.</summary>
    /// <param name="code">The finding code under test.</param>
    /// <param name="tier">The tier the code must carry.</param>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData("exception-expired", "G")]
    [InlineData("exception-finding-unknown", "G")]
    [InlineData("use-case-unverified", "A")]
    [InlineData("proof-invalid", "A")]
    public void New_Governance_Codes_Have_Tiers(string code, string tier)
    {
        Assert.Equal(tier, FindingCodes.Of(code).Level);
    }

    /// <summary>The backend folder drift code blocks where its reader is qualified.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Mark_Folder_Drift_Code_Is_Stack()
    {
        Assert.Equal("S", FindingCodes.Of("mark-folder-drift").Level);
    }

    /// <summary>Every finding code reads subject-problem with one of the problem words.</summary>
    /// <proves>litespec/core/language#name-each-finding-code-by-subject-and-problem</proves>
    [Fact]
    public void Codes_Follow_Subject_Problem_Grammar()
    {
        var bad = FindingCodes.All.Select(row => row.Code)
            .Where(code => !FollowsGrammar(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(bad);
    }

    /// <summary>Every literal a checker passes as a finding code, across the CLI and model sources.</summary>
    /// <param name="repository">The path of the repository root.</param>
    /// <returns>The emitted finding codes in ordinal order.</returns>
    static IReadOnlyList<string> EmittedCodes(string repository)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var root in new[] { Path.Combine(repository, "apps", "cli", "src"), Path.Combine(repository, "apps", "model", "src") })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Match match in EmittedCode().Matches(File.ReadAllText(file)))
                {
                    found.Add(match.Groups["code"].Value);
                }
            }
        }

        return found.ToList();
    }

    /// <summary>Whether the code ends with one of the problem words of the finding-code grammar.</summary>
    /// <param name="code">The finding code under test.</param>
    /// <returns>True when the code follows the subject-problem grammar; otherwise false.</returns>
    static bool FollowsGrammar(string code)
    {
        var dash = code.LastIndexOf('-');

        return dash > 0 && dash < code.Length - 1 && ProblemWords.Contains(code[(dash + 1)..]);
    }

    /// <summary>Gets the problem words of the finding-code grammar, as the problem-word table on the record language page lists them.</summary>
    static HashSet<string> ProblemWords { get; } = new(StringComparer.Ordinal)
    {
        "missing", "unknown", "undeclared", "unrecorded", "unimplemented", "unreached",
        "drift", "invalid", "duplicate", "expired", "unverified", "break",
        "orphaned", "bare", "unused",
    };

    /// <summary>The LiteSpec repository root, found above the test binary.</summary>
    /// <returns>The path of the repository root.</returns>
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

    /// <summary>Every quoted kebab-case literal in the CLI, model and analyzer sources, outside the code table itself.</summary>
    /// <param name="repository">The path of the repository root.</param>
    /// <returns>The literals found.</returns>
    static HashSet<string> QuotedLiterals(string repository)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in new[] { "cli", "model", "analyzers" }.Select(app => Path.Combine(repository, "apps", app, "src")))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);

                if (name is "FindingCodes.cs" or "FindingCode.cs"
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Match match in QuotedCode().Matches(File.ReadAllText(file)))
                {
                    found.Add(match.Groups["code"].Value);
                }
            }
        }

        return found;
    }

    static Regex QuotedCode() =>
        new("\"(?<code>[a-z0-9]+(?:-[a-z0-9]+)+)\"", RegexOptions.Compiled);

    static Regex EmittedCode() =>
        new("(?:new Finding\\(|Tiered\\()\"(?<code>[a-z0-9]+(?:-[a-z0-9]+)+)\"", RegexOptions.Compiled);

}
