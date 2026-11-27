using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the token-limits measure: a requirement naming token-limits is measured by the token limit checks of the same run.</summary>
public sealed class TokenLimitsMeasureTests
{
    /// <summary>Token limits met report no missing measure.</summary>
    /// <proves>litespec/core/intent#state-the-measure-on-a-counted-requirement</proves>
    [Fact]
    public void TokenLimitsMetReportsNoMissingMeasure()
    {
        using var temp = TestCli.Temp("token-measure");
        WriteDesignContract(temp.Path, PassingTokens());
        var model = TestModel.Model(TestModel.Config(temp.Path), TokenRequirement(temp.Path));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Token limits missed report the token failure with no missing measure.</summary>
    /// <proves>litespec/core/intent#state-the-measure-on-a-counted-requirement</proves>
    [Fact]
    public void TokenLimitsMissedReportsTokenFailureWithNoMissingMeasure()
    {
        using var temp = TestCli.Temp("token-measure");
        WriteDesignContract(temp.Path, FailingTokens());
        var model = TestModel.Model(TestModel.Config(temp.Path), TokenRequirement(temp.Path));
        var codes = TestModel.Codes(model);

        Assert.Contains("design-token-limit-failed", codes);
        Assert.DoesNotContain("requirement-measure-missing", codes);
    }

    /// <summary>No configured tokens report no missing measure.</summary>
    /// <proves>litespec/core/intent#state-the-measure-on-a-counted-requirement</proves>
    [Fact]
    public void TokenLimitsWithoutConfiguredTokensReportsNoMissingMeasure()
    {
        using var temp = TestCli.Temp("token-measure");
        var model = TestModel.Model(TestModel.Config(temp.Path), TokenRequirement(temp.Path));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>A quality requirement naming token-limits as its check.</summary>
    /// <param name="root">The consumer root the record file sits under.</param>
    /// <returns>The requirement record.</returns>
    static Record TokenRequirement(string root) =>
        TestModel.Record(
            "requirement",
            "requirement/ui-text-contrast",
            Path.Combine(root, "ui-text-contrast.md"),
            """{"kind": "quality", "statement": "Body text reaches 4.5:1 against its surface in each declared color scheme.", "stimulus": "A reader opens any screen in any declared color scheme.", "environment": "Normal operation on a phone or desktop display.", "response": "Every text role meets its contrast limit against its surface.", "measure": "Lowest contrast ratio of body text against its surface, per declared scheme", "by": "2027-01-01", "check": "token-limits"}""");

    /// <summary>Writes a project whose UI package names a design contract with a token file.</summary>
    /// <param name="root">The consumer root.</param>
    /// <param name="tokens">The token file text, written beside the design contract.</param>
    static void WriteDesignContract(string root, string tokens)
    {
        var package = Path.Combine(root, "packages", "ui");
        Directory.CreateDirectory(Path.Combine(package, "tokens"));
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(Path.Combine(package, "tokens", "fixture.tokens.json"), tokens);
        File.WriteAllText(
            Path.Combine(package, "DESIGN.md"),
            """
            ---
            schemaVersion: 2
            package: "@fixture/ui"
            tokens: tokens/fixture.tokens.json
            pageFrames:
              list-page: ListPage
            ---

            # Fixture design contract
            """);
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ],
              "ui": { "package": { "designContract": "packages/ui/DESIGN.md" } },
              "paths": {}
            }
            """);
    }

    /// <summary>A token file meeting every limit in the light scheme.</summary>
    /// <returns>The token file text.</returns>
    static string PassingTokens() =>
        """
        {
          "light": {
            "page": {"$value": "#ffffff"},
            "layer-1": {"$value": "#f0f0f0"},
            "layer-2": {"$value": "#e0e0e0"},
            "layer-accent": {"$value": "#b9d6f2"},
            "separator": {"$value": "#d8d8d8"},
            "layer-edge": {"$value": "#c9c9c9"},
            "control-boundary": {"$value": "#767676"},
            "text": {"$value": "#111111"},
            "muted-text": {"$value": "#595959"},
            "primary": {"$value": "#005fcc"},
            "primary-text": {"$value": "#ffffff"},
            "focus-ring": {"$value": "#005fcc"},
            "tone": {
              "neutral": {"text": {"$value": "#111111"}, "fill": {"$value": "#f0f0f0"}, "edge": {"$value": "#999999"}},
              "positive": {"text": {"$value": "#0a4d1f"}, "fill": {"$value": "#dcf5e3"}, "edge": {"$value": "#999999"}},
              "caution": {"text": {"$value": "#6b4a00"}, "fill": {"$value": "#ffefc2"}, "edge": {"$value": "#999999"}},
              "destructive": {"text": {"$value": "#7a0000"}, "fill": {"$value": "#ffe0e0"}, "edge": {"$value": "#999999"}},
              "info": {"text": {"$value": "#003a6b"}, "fill": {"$value": "#d8ecff"}, "edge": {"$value": "#999999"}}
            },
            "chart": {
              "1": {"$value": "#b00000"},
              "2": {"$value": "#0047b0"},
              "3": {"$value": "#00691f"}
            },
            "magnitude": ["#1a1a1a", "#7a7a7a", "#e8e8e8"]
          }
        }
        """;

    /// <summary>A token file missing the text contrast limit in the light scheme.</summary>
    /// <returns>The token file text.</returns>
    static string FailingTokens() =>
        """
        {
          "light": {
            "page": {"$value": "#ffffff"},
            "text": {"$value": "#c8c8c8"}
          }
        }
        """;
}
