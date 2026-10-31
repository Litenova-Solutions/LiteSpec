using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves every finding code the checkers emit has a tier, so an unknown code fails the build.</summary>
public sealed class FindingCodesTests
{
    /// <summary>Every mark finding code has a tier.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Mark_Codes_Have_Tiers()
    {
        var missing = MarkRules.FindingCodes
            .Where(code => !Finding.Tiers.ContainsKey(code))
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
            .Where(code => !Finding.Tiers.ContainsKey(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }

    /// <summary>Every code a known gap may name has a tier.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Checker_Codes_Have_Tiers()
    {
        var repository = FindRepository();
        var checker = Path.Combine(repository, "apps", "cli", "src", "LiteSpec.Cli", "Model", "Checker.cs");
        var codes = CheckerCodes(File.ReadAllText(checker));
        var missing = codes
            .Where(code => !Finding.Tiers.ContainsKey(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(codes);
        Assert.Empty(missing);
    }

    /// <summary>Each new Intent finding code carries its contracted tier.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Theory]
    [InlineData("requirement-cites-missing", "A")]
    [InlineData("constraint-unreached", "G")]
    [InlineData("parent-reopened", "A")]
    public void New_Intent_Codes_Have_Tiers(string code, string tier)
    {
        Assert.Equal(tier, Finding.Tiers[code]);
    }

    /// <summary>The value-style code reports at level A.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Value_Invalid_Code_Is_Advisory()
    {
        Assert.Equal("A", Finding.Tiers["value-invalid"]);
    }

    /// <summary>The table-drift code reports at level A.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Table_Drift_Code_Is_Advisory()
    {
        Assert.Equal("A", Finding.Tiers["table-drift"]);
    }

    /// <summary>Every finding code reads subject-problem with one of the twelve problem words.</summary>
    /// <proves>litespec/core/language#name-each-finding-code-by-subject-and-problem</proves>
    [Fact]
    public void Codes_Follow_Subject_Problem_Grammar()
    {
        var bad = Finding.Tiers.Keys
            .Where(code => !FollowsGrammar(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(bad);
    }

    /// <summary>Every literal a checker passes as a finding code, across the CLI and model sources.</summary>
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

    /// <summary>The codes in the Checker's known-gap set, read from its source.</summary>
    static IReadOnlyList<string> CheckerCodes(string checkerSource)
    {
        var start = checkerSource.IndexOf("Codes = new", StringComparison.Ordinal);

        Assert.True(start >= 0, "Checker.cs carries no known-gap code set.");

        var open = checkerSource.IndexOf('{', start);
        var close = checkerSource.IndexOf("};", open, StringComparison.Ordinal);

        Assert.True(open >= 0 && close > open, "Checker.cs carries no known-gap code set.");

        var block = checkerSource.Substring(open, close - open);
        var found = new List<string>();

        foreach (Match match in QuotedCode().Matches(block))
        {
            found.Add(match.Groups["code"].Value);
        }

        return found;
    }

    /// <summary>Whether the code ends with one of the twelve problem words.</summary>
    static bool FollowsGrammar(string code)
    {
        var dash = code.LastIndexOf('-');

        return dash > 0 && dash < code.Length - 1 && ProblemWords.Contains(code[(dash + 1)..]);
    }

    /// <summary>The twelve problem words of the finding-code grammar.</summary>
    static HashSet<string> ProblemWords { get; } = new(StringComparer.Ordinal)
    {
        "missing", "unknown", "undeclared", "unrecorded", "unimplemented", "unreached",
        "drift", "invalid", "duplicate", "clash", "expired", "reopened",
    };

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

    static Regex EmittedCode() =>
        new("(?:new Finding\\(|Tiered\\()\"(?<code>[a-z0-9]+(?:-[a-z0-9]+)+)\"", RegexOptions.Compiled);

    static Regex QuotedCode() =>
        new("\"(?<code>[a-z0-9]+(?:-[a-z0-9]+)+)\"", RegexOptions.Compiled);
}
