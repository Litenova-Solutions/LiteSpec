using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Proves <c>litespec rename</c> rewrites marks and leaves other strings alone.</summary>
public sealed class RenameMarksTests
{
    const string Old = "use-case/lending.borrow-copy";
    const string New = "use-case/lending.borrow-item";

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
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

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void Rename_Leaves_An_Unrelated_String_Alone()
    {
        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", Old, New, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"public const string BorrowCopy = \"{Old}\";", File.ReadAllText(Unrelated(temp.Path)));
    }

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
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

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void Rename_Keeps_A_Technical_Decision_Under_Implementation()
    {
        const string oldDecision = "decision/0002-store-loans-as-documents";
        const string newDecision = "decision/0002-store-loans-as-files";

        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);
        var intentPage = Path.Combine(temp.Path, "docs", "intent", "decisions", "0002-store-loans-as-documents.md");
        var technicalFolder = Path.Combine(temp.Path, "docs", "implementation", "decisions");
        Directory.CreateDirectory(technicalFolder);
        File.Move(intentPage, Path.Combine(technicalFolder, "0002-store-loans-as-documents.md"));

        var result = Cli.Run("rename", oldDecision, newDecision, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Output);
        Assert.True(File.Exists(Path.Combine(technicalFolder, "0002-store-loans-as-files.md")), result.Output);
        Assert.False(File.Exists(Path.Combine(temp.Path, "docs", "intent", "decisions", "0002-store-loans-as-files.md")));
    }

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void Rename_Rewrites_A_Requirement_Id_Inside_Proves_Marks_In_Both_Styles()
    {
        const string oldRequirement = "requirement/performance.borrow-latency";
        const string newRequirement = "requirement/performance.borrow-speed";

        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", oldRequirement, newRequirement, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"<proves>{newRequirement}</proves>", File.ReadAllText(ProvesTag(temp.Path)));
        Assert.DoesNotContain(oldRequirement, File.ReadAllText(ProvesTag(temp.Path)));
        Assert.Contains($"[Proves(\"{newRequirement}\")]", File.ReadAllText(ProvesAttribute(temp.Path)));
        Assert.DoesNotContain(oldRequirement, File.ReadAllText(ProvesAttribute(temp.Path)));
    }

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void Rename_Rewrites_An_External_System_Id_Inside_Proves_Marks_In_Both_Styles()
    {
        const string oldSystem = "external-system/text-gateway";
        const string newSystem = "external-system/sms-gateway";

        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", oldSystem, newSystem, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"<proves>{newSystem}</proves>", File.ReadAllText(ProvesTag(temp.Path)));
        Assert.DoesNotContain(oldSystem, File.ReadAllText(ProvesTag(temp.Path)));
        Assert.Contains($"[Proves(\"{newSystem}\")]", File.ReadAllText(ProvesAttribute(temp.Path)));
        Assert.DoesNotContain(oldSystem, File.ReadAllText(ProvesAttribute(temp.Path)));
    }

    /// <proves>litespec/core/authoring#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void Rename_Rewrites_An_Integration_Event_Id_Inside_Proves_Marks_In_Both_Styles()
    {
        const string oldEvent = "integration-event/loan-borrowed";
        const string newEvent = "integration-event/loan-extended";

        using var temp = Cli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = Cli.Run("rename", oldEvent, newEvent, "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains($"<proves>{newEvent}</proves>", File.ReadAllText(ProvesTag(temp.Path)));
        Assert.DoesNotContain(oldEvent, File.ReadAllText(ProvesTag(temp.Path)));
        Assert.Contains($"[Proves(\"{newEvent}\")]", File.ReadAllText(ProvesAttribute(temp.Path)));
        Assert.DoesNotContain(oldEvent, File.ReadAllText(ProvesAttribute(temp.Path)));
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

    static string ProvesTag(string root) => Path.Combine(
        root, "apps", "api", "tests", "Library.Acceptance.Tests", "Proof", "ProvesTag.cs");

    static string ProvesAttribute(string root) => Path.Combine(
        root, "apps", "api", "tests", "Library.Acceptance.Tests", "Proof", "ProvesAttribute.cs");

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
              "approvedOn": "2026-10-01",
              "choice": "Documents",
              "options": [
                {
                  "name": "Documents",
                  "pros": "Loans read as one document",
                  "cons": "Cross-loan queries need an index"
                }
              ],
              "reopenedWhen": "Cross-loan queries pass one hundred a day"
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
        Write(Path.Combine(root, "docs", "intent", "borrowing", "performance.md"), """
            ---
            {
              "type": "topic",
              "id": "topic/performance",
              "title": "Borrowing performance",
              "owner": "lending-team",
              "status": "approved",
              "reviewBy": "2027-04-01",
              "records": [
                {
                  "type": "requirement",
                  "id": "requirement/performance.borrow-latency",
                  "title": "Borrow latency",
                  "kind": "quality",
                  "statement": "Borrowing completes within two hundred milliseconds at p95.",
                  "rationale": "Readers leave when borrowing is slow."
                }
              ]
            }
            ---

            # Borrowing performance

            Borrowing stays fast.
            """);
        Write(Path.Combine(root, "docs", "behavior", "external-systems", "text-gateway.md"), """
            ---
            {
              "type": "external-system",
              "id": "external-system/text-gateway",
              "title": "Text gateway",
              "owner": "lending-team",
              "status": "reviewed"
            }
            ---

            # Text gateway

            The text gateway sends overdue notices to readers.
            """);
        Write(Path.Combine(root, "docs", "behavior", "integration-events", "loan-borrowed.md"), """
            ---
            {
              "type": "integration-event",
              "id": "integration-event/loan-borrowed",
              "title": "Loan borrowed",
              "owner": "lending-team",
              "status": "reviewed"
            }
            ---

            # Loan borrowed

            A loan opened on the shelf.
            """);
        Write(ProvesTag(root), """
            namespace Library.Acceptance.Tests;

            public sealed class LatencyTests
            {
                /// <proves>requirement/performance.borrow-latency</proves>
                /// <proves>external-system/text-gateway</proves>
                /// <proves>integration-event/loan-borrowed</proves>
                [Fact]
                public void Borrowing_stays_fast()
                {
                }
            }
            """);
        Write(ProvesAttribute(root), """
            namespace Library.Acceptance.Tests;

            public sealed class LatencyAttributeTests
            {
                [Proves("requirement/performance.borrow-latency")]
                [Proves("external-system/text-gateway")]
                [Proves("integration-event/loan-borrowed")]
                [Fact]
                public void Borrowing_stays_fast()
                {
                }
            }
            """);
    }

    static void Write(string file, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text.Replace("\r\n", "\n") + "\n");
    }
}
