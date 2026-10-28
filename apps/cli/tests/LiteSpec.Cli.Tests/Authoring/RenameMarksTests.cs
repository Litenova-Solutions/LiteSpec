using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Proves <c>litespec rename</c> rewrites marks and leaves other strings alone.</summary>
public sealed class RenameMarksTests
{
    const string Old = "use-case/lending.borrow-copy";
    const string New = "use-case/lending.borrow-item";

    [Fact]
    public void Rename_Rewrites_A_Doc_Tag_An_Attribute_And_A_Frontend_Constant()
    {
        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", Old, New, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"<implements>{New}</implements>", File.ReadAllText(DocTag(temp.Path)));
        Assert.DoesNotContain(Old, File.ReadAllText(DocTag(temp.Path)));
        Assert.Contains($"[Implements(\"{New}\")]", File.ReadAllText(Attribute(temp.Path)));
        Assert.DoesNotContain(Old, File.ReadAllText(Attribute(temp.Path)));
        Assert.Contains($"implements: \"{New}\"", File.ReadAllText(Frontend(temp.Path)));
        Assert.DoesNotContain(Old, File.ReadAllText(Frontend(temp.Path)));
    }

    [Fact]
    public void Rename_Leaves_An_Unrelated_String_Alone()
    {
        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", Old, New, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"public const string BorrowCopy = \"{Old}\";", File.ReadAllText(Unrelated(temp.Path)));
    }

    static string DocTag(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.Application", "BorrowCopyCommandHandler.cs");

    static string Attribute(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.WebApi", "BorrowCopyEndpoint.cs");

    static string Frontend(string root) => Path.Combine(
        root, "apps", "catalog", "src", "lib", "borrow.ts");

    static string Unrelated(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.Application", "Labels.cs");

    static void WriteConsumer(string root)
    {
        Write(Path.Combine(root, "litespec.project.json"), """
            {
              "project": {
                "name": "Rename Fixture"
              },
              "size": "small",
              "owners": [
                "lending-team"
              ],
              "paths": {
                "docs": "docs",
                "behaviorDocs": "docs/behavior",
                "glossary": "docs/glossary.json",
                "testRoots": [
                  "apps/api/tests"
                ]
              },
              "stackConfig": {
                "dotnet": {
                  "application": "apps/api/src/Library.Application",
                  "domain": "apps/api/src/Library.Domain",
                  "webapi": "apps/api/src/Library.WebApi"
                }
              }
            }
            """);
        Write(Path.Combine(root, "docs", "glossary.json"), """
            {
              "schemaVersion": 2,
              "terms": [],
              "mannered": []
            }
            """);
        Write(Path.Combine(root, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md"), """
            ---
            {
              "type": "use-case",
              "id": "use-case/lending.borrow-copy",
              "title": "Borrow copy",
              "owner": "lending-team",
              "status": "reviewed",
              "kind": "command"
            }
            ---

            # Borrow copy

            A reader borrows an available copy.
            """);
        Write(DocTag(root), """
            namespace Library.Application;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler;
            """);
        Write(Attribute(root), """
            namespace Library.WebApi;

            [Implements("use-case/lending.borrow-copy")]
            public static class BorrowCopyEndpoint;
            """);
        Write(Frontend(root), """
            export const litespec = { implements: "use-case/lending.borrow-copy" } as const;
            """);
        Write(Unrelated(root), """
            namespace Library.Application;

            public static class Labels
            {
                public const string BorrowCopy = "use-case/lending.borrow-copy";
            }
            """);
    }

    static void Write(string file, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text.Replace("\r\n", "\n") + "\n");
    }
}
