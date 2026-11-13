using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Proves <c>litespec rename</c> rewrites marks and leaves other strings alone.</summary>
public sealed class RenameMarksTests
{
    const string Old = "use-case/lending.borrow-copy";
    const string New = "use-case/lending.borrow-item";

    /// <summary>Rename Rewrites A Doc Tag An Attribute And A Frontend Constant.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameRewritesADocTagAnAttributeAndAFrontendConstant()
    {
        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = TestCli.Run("rename", Old, New, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Stderr);
        Assert.Contains($"<implements>{New}</implements>", File.ReadAllText(DocTag(temp.Path)));
        Assert.DoesNotContain(Old, File.ReadAllText(DocTag(temp.Path)));
        Assert.Contains($"[Implements(\"{New}\")]", File.ReadAllText(Attribute(temp.Path)));
        Assert.DoesNotContain(Old, File.ReadAllText(Attribute(temp.Path)));
        Assert.Contains($"implements: \"{New}\"", File.ReadAllText(Frontend(temp.Path)));
        Assert.DoesNotContain(Old, File.ReadAllText(Frontend(temp.Path)));
    }

    /// <summary>Rename Leaves An Unrelated String Alone.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameLeavesAnUnrelatedStringAlone()
    {
        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = TestCli.Run("rename", Old, New, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Stderr);
        Assert.Contains($"public const string BorrowCopy = \"{Old}\";", File.ReadAllText(Unrelated(temp.Path)));
    }

    /// <summary>Rename Rewrites A Decision Id Inside Follows Marks In Both Styles.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameRewritesADecisionIdInsideFollowsMarksInBothStyles()
    {
        const string oldDecision = "decision/0002-store-loans-as-documents";
        const string newDecision = "decision/0002-store-loans-as-files";

        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = TestCli.Run("rename", oldDecision, newDecision, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Stderr);
        Assert.Contains($"<follows>{newDecision}</follows>", File.ReadAllText(FollowsTag(temp.Path)));
        Assert.DoesNotContain(oldDecision, File.ReadAllText(FollowsTag(temp.Path)));
        Assert.Contains($"[Follows(\"{newDecision}\")]", File.ReadAllText(FollowsAttribute(temp.Path)));
        Assert.DoesNotContain(oldDecision, File.ReadAllText(FollowsAttribute(temp.Path)));
    }

    /// <summary>Rename Keeps A Technical Decision Under Implementation.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameKeepsATechnicalDecisionUnderImplementation()
    {
        const string oldDecision = "decision/0002-store-loans-as-documents";
        const string newDecision = "decision/0002-store-loans-as-files";

        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);
        var intentPage = Path.Combine(temp.Path, ".litespec", "intent", "decisions", "0002-store-loans-as-documents", "0002-store-loans-as-documents.yaml");
        var technicalFolder = Path.Combine(temp.Path, ".litespec", "implementation", "decisions");
        Directory.CreateDirectory(technicalFolder);
        Directory.Move(Path.GetDirectoryName(intentPage)!, Path.Combine(technicalFolder, "0002-store-loans-as-documents"));

        var result = TestCli.Run("rename", oldDecision, newDecision, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Output);
        Assert.True(File.Exists(Path.Combine(technicalFolder, "0002-store-loans-as-files", "0002-store-loans-as-files.yaml")), result.Output);
        Assert.False(File.Exists(Path.Combine(temp.Path, ".litespec", "intent", "decisions", "0002-store-loans-as-files", "0002-store-loans-as-files.yaml")));
    }

    /// <summary>Rename Rewrites A Requirement Id Inside Proves Marks In Both Styles.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameRewritesARequirementIdInsideProvesMarksInBothStyles()
    {
        const string oldRequirement = "requirement/performance.borrow-latency";
        const string newRequirement = "requirement/performance.borrow-speed";

        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = TestCli.Run("rename", oldRequirement, newRequirement, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Stderr);
        Assert.Contains($"<proves>{newRequirement}</proves>", File.ReadAllText(ProvesTag(temp.Path)));
        Assert.DoesNotContain(oldRequirement, File.ReadAllText(ProvesTag(temp.Path)));
        Assert.Contains($"[Proves(\"{newRequirement}\")]", File.ReadAllText(ProvesAttribute(temp.Path)));
        Assert.DoesNotContain(oldRequirement, File.ReadAllText(ProvesAttribute(temp.Path)));
    }

    /// <summary>Rename Rewrites An External System Id Inside Proves Marks In Both Styles.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameRewritesAnExternalSystemIdInsideProvesMarksInBothStyles()
    {
        const string oldSystem = "external-system/text-gateway";
        const string newSystem = "external-system/sms-gateway";

        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = TestCli.Run("rename", oldSystem, newSystem, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Stderr);
        Assert.Contains($"<proves>{newSystem}</proves>", File.ReadAllText(ProvesTag(temp.Path)));
        Assert.DoesNotContain(oldSystem, File.ReadAllText(ProvesTag(temp.Path)));
        Assert.Contains($"[Proves(\"{newSystem}\")]", File.ReadAllText(ProvesAttribute(temp.Path)));
        Assert.DoesNotContain(oldSystem, File.ReadAllText(ProvesAttribute(temp.Path)));
    }

    /// <summary>Rename Rewrites An Integration Event Id Inside Proves Marks In Both Styles.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact]
    public void RenameRewritesAnIntegrationEventIdInsideProvesMarksInBothStyles()
    {
        const string oldEvent = "integration-event/loan-borrowed";
        const string newEvent = "integration-event/loan-extended";

        using var temp = TestCli.Temp("rename-marks");
        WriteConsumer(temp.Path);

        var result = TestCli.Run("rename", oldEvent, newEvent, "--root", temp.Path);

        Assert.True(result.Status == 0, result.Stderr);
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
        Write(Path.Combine(root, ".litespec/project.yaml"), """
            {
              "project": {
                "name": "Rename Fixture"
              },
              "size": "small",
              "paths": {
                "docs": ".litespec",
                "behaviorDocs": ".litespec/behavior",
                "glossary": ".litespec/glossary.yaml",
                "testRoots": [
                  "apps/api/tests"
                ]
              },
              "stackConfig": {
                "dotnet": {
                  "modules": "apps/api/src/Library.Application",
                  "domain": "apps/api/src/Library.Domain",
                  "webapi": "apps/api/src/Library.WebApi"
                }
              }
            }
            """);
        TestRecords.Write(root, ".litespec/glossary.yaml", """
            {
              "schemaVersion": 2,
              "terms": [],
              "mannered": []
            }
            """);
        TestRecords.Write(root, ".litespec/behavior/modules/lending/loans/borrow-copy/borrow-copy.yaml", """
            {
              "type": "use-case",
              "id": "use-case/lending.borrow-copy",
              "title": "Borrow copy",
              "kind": "command"
            }
            """);
        TestRecords.Write(root, ".litespec/intent/decisions/0002-store-loans-as-documents/0002-store-loans-as-documents.yaml", """
            {
              "type": "decision",
              "id": "decision/0002-store-loans-as-documents",
              "title": "Store loans as documents",
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
        TestRecords.Write(root, ".litespec/intent/features/performance.yaml", """
            {
              "type": "feature",
              "id": "feature/performance",
              "title": "Borrowing performance",
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
            """);
        TestRecords.Write(root, ".litespec/behavior/external-systems/text-gateway.yaml", """
            {
              "type": "external-system",
              "id": "external-system/text-gateway",
              "title": "Text gateway"
            }
            """);
        TestRecords.Write(root, ".litespec/behavior/integration-events/loan-borrowed.yaml", """
            {
              "type": "integration-event",
              "id": "integration-event/loan-borrowed",
              "title": "Loan borrowed"
            }
            """);
        Write(ProvesTag(root), """
            namespace Library.Acceptance.Tests;

            public sealed class LatencyTests
            {
                /// <proves>requirement/performance.borrow-latency</proves>
                /// <proves>external-system/text-gateway</proves>
                /// <proves>integration-event/loan-borrowed</proves>
                [Fact]
                public void BorrowingStaysFast()
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
                public void BorrowingStaysFast()
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
