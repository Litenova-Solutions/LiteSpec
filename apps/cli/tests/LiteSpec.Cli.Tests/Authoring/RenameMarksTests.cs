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

    [Fact]
    public void Rename_Rewrites_A_Decision_Id_Inside_Follows_Marks_In_Both_Styles()
    {
        const string oldDecision = "decision/0002-store-loans-as-documents";
        const string newDecision = "decision/0002-store-loans-as-files";

        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", oldDecision, newDecision, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"<follows>{newDecision}</follows>", File.ReadAllText(FollowsTag(temp.Path)));
        Assert.DoesNotContain(oldDecision, File.ReadAllText(FollowsTag(temp.Path)));
        Assert.Contains($"[Follows(\"{newDecision}\")]", File.ReadAllText(FollowsAttribute(temp.Path)));
        Assert.DoesNotContain(oldDecision, File.ReadAllText(FollowsAttribute(temp.Path)));
    }

    static string DocTag(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.Application", "BorrowCopyCommandHandler.cs");

    static string Attribute(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.WebApi", "BorrowCopyEndpoint.cs");

    static string Frontend(string root) => Path.Combine(
        root, "apps", "catalog", "src", "lib", "borrow.ts");

    static string Unrelated(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.Application", "Labels.cs");

    static string FollowsTag(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.Application", "CommitStep.cs");

    static string FollowsAttribute(string root) => Path.Combine(
        root, "apps", "api", "src", "Library.Infrastructure", "LoanDocumentStore.cs");

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
        Write(Path.Combine(root, "docs", "intent", "decisions", "0002-store-loans-as-documents.md"), """
            ---
            {
              "type": "decision",
              "id": "decision/0002-store-loans-as-documents",
              "title": "Store loans as documents",
              "owner": "lending-team",
              "status": "approved",
              "decided": "2026-10-01",
              "chosen": "Documents",
              "options": [
                {
                  "name": "Documents",
                  "for": "Loans read as one document",
                  "against": "Cross-loan queries need an index"
                }
              ],
              "changeWhen": "Cross-loan queries pass one hundred a day"
            }
            ---

            # Store loans as documents

            Store each loan as one document.
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
        Write(FollowsTag(root), """
            namespace Library.Application;

            /// <follows>decision/0002-store-loans-as-documents</follows>
            internal sealed class CommitStep;
            """);
        Write(FollowsAttribute(root), """
            namespace Library.Infrastructure;

            [Follows("decision/0002-store-loans-as-documents")]
            internal sealed class LoanDocumentStore;
            """);
    }

    static void Write(string file, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text.Replace("\r\n", "\n") + "\n");
    }
}
