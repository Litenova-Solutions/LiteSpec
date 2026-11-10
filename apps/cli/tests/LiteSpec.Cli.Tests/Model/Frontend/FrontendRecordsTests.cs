using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves <c>litespec model render</c> writes the records file each web frontend imports from its frame.</summary>
public sealed class FrontendRecordsTests
{
    /// <summary>Render Writes The Records File Into Each Frame.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void RenderWritesTheRecordsFileIntoEachFrame()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var result = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, result.Status);

        var web = Path.Combine(fixture.Path, "apps", "web", "src", "lib", "app", "litespec-records.ts");
        var console = Path.Combine(fixture.Path, "apps", "console", "src", "app", "litespec-records.ts");

        // The fixture holds no proof records, so the proof union is LiteSpec's frontend provisions.
        foreach (var file in new[] { web, console })
        {
            var text = File.ReadAllText(file);

            Assert.Contains("  | \"litespec/frontend/ui#check-every-route\"", text);
            Assert.Equal(
                // The tool writes the file with LF; the raw literal takes the test file's line ending.
                ExpectedUnion.ReplaceLineEndings("\n") + "\n",
                System.Text.RegularExpressions.Regex.Replace(
                    text, "export type ProofTargetId =\n(  \\| \"[^\"]+\"\n)*?  \\| \"[^\"]+\";\n", "export type ProofTargetId = never;\n"));
        }
        Assert.Contains("apps/web/src/lib/app/litespec-records.ts", result.Stdout);
        Assert.Contains("apps/console/src/app/litespec-records.ts", result.Stdout);
    }

    /// <summary>Render Leaves Out Use Cases With No Browser Entry Point.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void RenderLeavesOutUseCasesWithNoBrowserEntryPoint()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var result = TestCli.Run("model", "render", "--root", fixture.Path);
        Assert.Equal(0, result.Status);

        var web = Path.Combine(fixture.Path, "apps", "web", "src", "lib", "app", "litespec-records.ts");

        Assert.DoesNotContain("use-case/lending.register-copy", File.ReadAllText(web));
    }

    /// <summary>Check Writes No Records File.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void CheckWritesNoRecordsFile()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        TestCli.Run("model", "check", "--root", fixture.Path);

        Assert.False(File.Exists(Path.Combine(fixture.Path, "apps", "web", "src", "lib", "app", "litespec-records.ts")));
        Assert.False(File.Exists(Path.Combine(fixture.Path, "apps", "console", "src", "app", "litespec-records.ts")));
    }

    /// <summary>Render Writes Never For No Ids.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void RenderWritesNeverForNoIds()
    {
        var text = FrontendRecords.Render([], [], []);

        Assert.Contains("export type UseCaseId = never;", text);
        Assert.Contains("export type ScreenId = never;", text);
        Assert.Contains("export type ProofTargetId = never;", text);
        Assert.Contains("export interface LiteSpecMark { readonly implements: UseCaseId | ScreenId }", text);
        Assert.Contains("export function proves(id: ProofTargetId)", text);
        Assert.EndsWith("\n", text);
    }

    /// <summary>Render Breaks A Long Union One Id Per Line.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void RenderBreaksALongUnionOneIdPerLine()
    {
        var ids = Enumerable.Range(1, 30).Select(number => $"use-case/lending.use-case-{number:00}").ToList();
        var text = FrontendRecords.Render(ids, [], []);

        Assert.Contains("\nexport type UseCaseId =\n  | \"use-case/lending.use-case-01\"\n", text);
        Assert.Contains("  | \"use-case/lending.use-case-30\";\n", text);
        Assert.Equal(text, FrontendRecords.Render(ids, [], []));
    }

    /// <summary>Render Writes The Proves Helper Typed To Proof Targets.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void RenderWritesTheProvesHelperTypedToProofTargets()
    {
        var text = FrontendRecords.Render([], [], ["scenario/list-loans.listing-succeeds"]);

        Assert.Contains("export type ProofTargetId = \"scenario/list-loans.listing-succeeds\";", text);
        Assert.Contains("return { annotation: [{ type: \"proves\", description: id }] };", text);
    }

    const string ExpectedUnion = """
        // This file is generated by litespec model render. Do not edit it by hand.
        export type UseCaseId = "use-case/lending.borrow-copy" | "use-case/lending.list-loans";
        export type ScreenId = never;
        export type ProofTargetId = never;
        export interface LiteSpecMark { readonly implements: UseCaseId | ScreenId }
        export function proves(id: ProofTargetId): { annotation: [{ type: "proves"; description: ProofTargetId }] } {
          return { annotation: [{ type: "proves", description: id }] };
        }
        """;
}
