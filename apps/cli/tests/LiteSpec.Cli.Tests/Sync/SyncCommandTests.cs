using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Sync;

/// <summary>Proves <c>litespec sync</c> writes the pure data the records determine.</summary>
public sealed class SyncCommandTests
{
    /// <summary>Sync writes each configured .NET file with the sync header and the record values, under plain names.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncWritesConfiguredDotnetFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            - kind: permissions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var refusals = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingRefusalCodes.cs");
        var permissions = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingPermissions.cs");
        Assert.True(File.Exists(refusals), "refusal codes file");
        Assert.True(File.Exists(permissions), "permissions file");
        Assert.False(File.Exists(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingRefusalCodes.generated.cs")), "no generated suffix file");

        var refusalText = File.ReadAllText(refusals);
        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", refusalText);
        Assert.DoesNotContain("litespec generate", refusalText);
        Assert.Contains("public static class LendingRefusalCodes", refusalText);
        Assert.Contains("public const string CopyNotAvailable = \"lending.copy-not-available\";", refusalText);
        Assert.Contains("The copy is on loan or on hold for another reader.", refusalText);

        var permissionText = File.ReadAllText(permissions);
        Assert.Contains("public static class LendingPermissions", permissionText);
        Assert.Contains("public const string BorrowCopy = \"permission/borrow-copy\";", permissionText);
        Assert.Contains("LendingRefusalCodes.cs", result.Stdout);
    }

    /// <summary>Sync deletes the matching old generated file on its first run and names it in the output.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncDeletesReplacedGeneratedFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        var folder = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending");
        Directory.CreateDirectory(folder);
        var old = Path.Combine(folder, "LendingRefusalCodes.generated.cs");
        File.WriteAllText(old, "// stale generated file\n");

        var drifted = TestCli.Run("sync", "--root", temp.Path, "--check");
        Assert.Equal(1, drifted.Status);
        Assert.Contains("LendingRefusalCodes.generated.cs", drifted.Stdout);
        Assert.Contains("LendingRefusalCodes.cs", drifted.Stdout);
        Assert.True(File.Exists(old));

        Assert.Equal(0, TestCli.Run("sync", "--root", temp.Path).Status);

        var current = Path.Combine(folder, "LendingRefusalCodes.cs");
        Assert.True(File.Exists(current), "synced file");
        Assert.False(File.Exists(old), "replaced file is deleted");
        Assert.Contains("public const string CopyNotAvailable", File.ReadAllText(current));
        Assert.Equal(0, TestCli.Run("sync", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Sync --check exits 1 listing the drifted file, and a resync clears it.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncCheckReportsDriftAndResyncClearsIt()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        Assert.Equal(0, TestCli.Run("sync", "--root", temp.Path).Status);

        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingRefusalCodes.cs");
        File.Delete(file);

        var drifted = TestCli.Run("sync", "--root", temp.Path, "--check");
        Assert.Equal(1, drifted.Status);
        Assert.Contains("LendingRefusalCodes.cs", drifted.Stdout);
        Assert.Contains("1 files would change.", drifted.Stdout);
        Assert.False(File.Exists(file));

        Assert.Equal(0, TestCli.Run("sync", "--root", temp.Path).Status);
        Assert.True(File.Exists(file));
        Assert.Equal(0, TestCli.Run("sync", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Sync writes nothing for a module with no entries, and exits 0 with no files.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncWritesNothingWhenNoEntriesExist()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: closed-sets
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("0 files rewritten.", result.Stdout);
    }

    /// <summary>Sync writes nothing and names each invalid file when a record file does not parse.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncRefusesWhenARecordFileDoesNotParse()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "broken.yaml"),
            "type: rule\nid: rule/lending.broken\nmeaning: The statuses, each with the day it applies from: registered or exempt\n");

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("broken.yaml", result.Output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingRefusalCodes.cs")));
    }

    /// <summary>Sync writes the TypeScript tables beside the client of each frontend that names one.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncWritesFrontendClientFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        ReplaceFrontends(temp.Path);
        WriteScreen(temp.Path);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var folder = Path.Combine(temp.Path, "packages", "api", "src", "litespec");
        var refusals = Path.Combine(folder, "refusals.ts");
        var permissions = Path.Combine(folder, "permissions.ts");
        var capabilities = Path.Combine(folder, "capabilities.ts");
        Assert.True(File.Exists(refusals), "refusals file");
        Assert.True(File.Exists(permissions), "permissions file");
        Assert.True(File.Exists(capabilities), "capabilities file");

        var refusalText = File.ReadAllText(refusals);
        Assert.Contains("// Written by litespec sync from the records. Edit the records, then run litespec sync.", refusalText);
        Assert.Contains("\"lending.copy-not-available\",", refusalText);
        Assert.Contains("export type RefusalCode = (typeof refusals)[number];", refusalText);

        var permissionText = File.ReadAllText(permissions);
        Assert.Contains("LendingBorrowCopy: \"permission/borrow-copy\",", permissionText);
        Assert.Contains("export type PermissionId = (typeof permissions)[keyof typeof permissions];", permissionText);

        var capabilityText = File.ReadAllText(capabilities);
        Assert.Contains("import type { PermissionId } from \"./permissions\";", capabilityText);
        Assert.Contains("\"/loans\": [\"permission/read-loans\"],", capabilityText);
    }

    /// <summary>
    ///     The capabilities table holds permission ids only: a self() grant reads through to its
    ///     permission, and a signed-in grant names none.
    /// </summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncCapabilitiesHoldPermissionIdsOnly()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        ReplaceFrontends(temp.Path);
        var useCases = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates");
        ReplaceGrant(Path.Combine(useCases, "holds", "use-cases", "cancel-hold", "cancel-hold.yaml"), "permission/place-hold", "self(permission/place-hold)");
        ReplaceGrant(Path.Combine(useCases, "copies", "use-cases", "register-copy", "register-copy.yaml"), "permission/manage-copies", "signed-in");
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "entry-points", "library-app", "screens", "hold-list");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "hold-list.yaml"),
            """
            type: screen
            id: screen/library-app.hold-list
            title: Hold List
            summary: A reader sees their holds and cancels one.
            route: /holds
            offers:
            - use-case/lending.cancel-hold
            - use-case/lending.register-copy
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var capabilityText = File.ReadAllText(Path.Combine(temp.Path, "packages", "api", "src", "litespec", "capabilities.ts"));
        Assert.Contains("\"/holds\": [\"permission/place-hold\"],", capabilityText);
        Assert.DoesNotContain("self(", capabilityText);
        Assert.DoesNotContain("signed-in", capabilityText);

        static void ReplaceGrant(string file, string from, string to)
        {
            var text = File.ReadAllText(file);
            Assert.Contains($"grant: {from}", text);
            File.WriteAllText(file, text.Replace($"grant: {from}", $"grant: {to}", StringComparison.Ordinal));
        }
    }

    /// <summary>Sync writes one event decision file per module with the decisions the records state.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncWritesEventDecisions()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: event-decisions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingEventDecisions.cs");
        Assert.True(File.Exists(file), "event decisions file");

        var text = File.ReadAllText(file);
        Assert.Contains("public static class LendingEventDecisions", text);
        Assert.Contains("public const string LoanLoanBorrowed = \"ignored\";", text);
        Assert.Contains("[\"domain-event/loan.loan-borrowed\"] = LoanLoanBorrowed,", text);
    }

    /// <summary>Sync writes one closed set file per home with a nested class per set.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncWritesClosedSets()
    {
        using var temp = ModelFixture.Prepare("small");
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "reference-data", "loan-kind");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "loan-kind.yaml"),
            """
            type: reference-data
            id: reference-data/lending.loan-kind
            title: Loan Kind
            summary: The kinds of loan the library lends.
            choices:
            - standard
            - reference-only
            rows:
            - value: standard
              key: standard-2026
              effectiveFrom: 2026-01-01
              source: decision/0001-loan-period
            """);
        AppendGenerate(temp.Path, """
            sync:
            - kind: closed-sets
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingClosedSets.cs");
        Assert.True(File.Exists(file), "closed sets file");

        var text = File.ReadAllText(file);
        Assert.Contains("public static class LendingClosedSets", text);
        Assert.Contains("public static class LoanKind", text);
        Assert.Contains("public const string Standard = \"standard\";", text);
        Assert.Contains("public const string ReferenceOnly = \"reference-only\";", text);
    }

    /// <summary>Sync writes nothing and exits 1 naming each invalid target.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncFailsOnAnInvalidTarget()
    {
        using var temp = ModelFixture.Prepare("small");
        var modules = Path.Combine(temp.Path, "apps", "api", "src", "Modules");

        if (Directory.Exists(modules))
        {
            Directory.Delete(modules, recursive: true);
        }

        AppendGenerate(temp.Path, """
            sync:
            - kind: unknown-kind
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("Sync target kind 'unknown-kind' is unknown", result.Stderr, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "apps", "api", "src", "Modules")));
    }

    /// <summary>Sync skips a svelte target: the frontend clients own the TypeScript tables now.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncSkipsSvelteTargets()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            sync:
            - kind: refusal-codes
              stack: svelte
              path: apps/web/src/generated
            """);

        var result = TestCli.Run("sync", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("0 files rewritten.", result.Stdout);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "apps", "web")));
    }

    /// <summary>Sync --help prints the usage text, and an unknown option is a usage error.</summary>
    /// <proves>litespec/core/model#sync-record-determined-code-from-the-records</proves>
    [Fact]
    public void SyncHelpAndUsageErrors()
    {
        using var temp = ModelFixture.Prepare("small");

        var help = TestCli.Run("sync", "--help");
        Assert.Equal(0, help.Status);
        Assert.Contains("litespec sync", help.Stdout, StringComparison.Ordinal);

        var unknown = TestCli.Run("sync", "--root", temp.Path, "--bogus");
        Assert.Equal(2, unknown.Status);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");

    /// <summary>Replaces the fixture frontends line with the web frontend the client tables need.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void ReplaceFrontends(string root)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        File.WriteAllText(
            project,
            File.ReadAllText(project).Replace(
                "frontends: []",
                """
                frontends:
                - name: web
                  path: apps/web
                  platform: svelte
                  client:
                    contract: packages/api/openapi.json
                    generated: packages/api/src/generated
                """,
                StringComparison.Ordinal));
    }

    /// <summary>Writes a screen offering the list-loans use case, so the capabilities table has one route.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteScreen(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "entry-points", "library-app", "screens", "loan-list");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "loan-list.yaml"),
            """
            type: screen
            id: screen/library-app.loan-list
            title: Loan List
            summary: A reader sees their loans with the latest state of each.
            route: /loans
            offers:
            - use-case/lending.list-loans
            """);
    }
}
