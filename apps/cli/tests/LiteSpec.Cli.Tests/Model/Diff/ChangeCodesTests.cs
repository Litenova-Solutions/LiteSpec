using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteSpec.Tests.Model.Diff;

/// <summary>Proves every diff change code keeps the past-participle form.</summary>
public sealed class ChangeCodesTests
{
    /// <summary>Every change code the differ emits or maps ends with a past participle.</summary>
    /// <proves>litespec/core/language#name-each-change-code-with-a-past-participle</proves>
    [Fact]
    public void Change_Codes_Use_Past_Participles()
    {
        var codes = DifferCodes(FindRepository());
        var bad = codes
            .Where(code => !EndsWithParticiple(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(codes);
        Assert.Empty(bad);
    }

    /// <summary>The documented, emitted, and mapped change codes agree.</summary>
    /// <proves>litespec/core/language#name-each-change-code-with-a-past-participle</proves>
    [Fact]
    public void Documented_Emitted_And_Mapped_Codes_Agree()
    {
        var repository = FindRepository();
        var source = DifferSource(repository);
        var documented = DocumentedCodes(repository).OrderBy(code => code, StringComparer.Ordinal).ToList();
        var emitted = EmittedCodes(source).OrderBy(code => code, StringComparer.Ordinal).ToList();
        var mapped = MappedCodes(source).OrderBy(code => code, StringComparer.Ordinal).ToList();

        Assert.NotEmpty(documented);
        Assert.Equal(emitted, documented);
        Assert.Equal(mapped, emitted);
    }

    /// <summary>Every change code the differ emits or maps to a message.</summary>
    /// <param name="repository">The LiteSpec repository root folder.</param>
    /// <returns>Every change code the differ knows.</returns>
    static IReadOnlyList<string> DifferCodes(string repository)
    {
        var source = DifferSource(repository);
        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var code in EmittedCodes(source))
        {
            found.Add(code);
        }

        foreach (var code in MappedCodes(source))
        {
            found.Add(code);
        }

        return found.ToList();
    }

    /// <summary>Every change code the differ emits: reason calls and removal reasons.</summary>
    /// <param name="differSource">The differ source text to scan.</param>
    /// <returns>The change codes the differ emits.</returns>
    static IReadOnlyList<string> EmittedCodes(string differSource)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (Match match in EmittedReason().Matches(differSource))
        {
            found.Add(match.Groups["code"].Value);
        }

        foreach (Match match in SwitchValue().Matches(differSource))
        {
            found.Add(match.Groups["code"].Value);
        }

        return found.ToList();
    }

    /// <summary>The codes in the differ's reason table, read from its source.</summary>
    /// <param name="differSource">The differ source text to scan.</param>
    /// <returns>The change codes in the reason table.</returns>
    static IReadOnlyList<string> MappedCodes(string differSource)
    {
        var start = differSource.IndexOf("static BreakingReason Reason(string code)", StringComparison.Ordinal);

        Assert.True(start >= 0, "Differ.cs carries no breaking-reason table.");

        var open = differSource.IndexOf('{', start);
        var close = differSource.IndexOf("});", open, StringComparison.Ordinal);

        Assert.True(open >= 0 && close > open, "Differ.cs carries no breaking-reason table.");

        var block = differSource.Substring(open, close - open);
        var found = new List<string>();

        foreach (Match match in SwitchKey().Matches(block))
        {
            found.Add(match.Groups["code"].Value);
        }

        return found;
    }

    /// <summary>The codes in the breaking-reason table on the model page.</summary>
    /// <param name="repository">The LiteSpec repository root folder.</param>
    /// <returns>The documented breaking change codes.</returns>
    static IReadOnlyList<string> DocumentedCodes(string repository)
    {
        var lines = File.ReadAllLines(Path.Combine(repository, "docs", "tools", "model.md"));
        var start = Array.FindIndex(
            lines, line => line.Contains("Each breaking entry names its reason code:", StringComparison.Ordinal));

        Assert.True(start >= 0, "model.md names no breaking-reason table.");

        var found = new List<string>();

        foreach (var line in lines.Skip(start + 1))
        {
            var match = TableCode().Match(line);

            if (match.Success)
            {
                found.Add(match.Groups["code"].Value);
            }
            else if (found.Count > 0)
            {
                break;
            }
        }

        return found;
    }

    /// <summary>Whether the code ends with a past participle.</summary>
    /// <param name="code">The change code to classify.</param>
    /// <returns>True when the last word of the code reads as a past participle.</returns>
    static bool EndsWithParticiple(string code)
    {
        var dash = code.LastIndexOf('-');

        if (dash < 0 || dash == code.Length - 1)
        {
            return false;
        }

        var last = code[(dash + 1)..];

        return last.EndsWith("ed", StringComparison.Ordinal)
            || last.EndsWith("en", StringComparison.Ordinal);
    }

    /// <summary>The differ source, read from the repository.</summary>
    /// <param name="repository">The LiteSpec repository root folder.</param>
    /// <returns>The differ source text.</returns>
    static string DifferSource(string repository) =>
        File.ReadAllText(Path.Combine(repository, "apps", "cli", "src", "LiteSpec.Cli", "Model", "Diff", "Differ.cs"));

    /// <summary>The LiteSpec repository root, found above the test binary.</summary>
    /// <returns>The repository root folder path.</returns>
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

    static Regex EmittedReason() =>
        new("Reason\\(\"(?<code>[a-z]+(?:-[a-z]+)+)\"\\)", RegexOptions.Compiled);

    static Regex SwitchValue() =>
        new("=> \"(?<code>[a-z]+(?:-[a-z]+)+)\"", RegexOptions.Compiled);

    static Regex SwitchKey() =>
        new("\"(?<code>[a-z]+(?:-[a-z]+)+)\" =>", RegexOptions.Compiled);

    static Regex TableCode() =>
        new("^\\| `(?<code>[a-z]+(?:-[a-z]+)+)` \\|", RegexOptions.Compiled);
}
