using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves <c>litespec generate --check</c> and the <c>generated-drift</c> finding detect every generated file whose content no longer matches the records.</summary>
public sealed class GenerateDriftCheckTests
{
    /// <summary>A route change lists the endpoints file as one that would change, and the model check reports it as drifted.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateCheckListsEndpointsFileAfterARouteChange()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);

        var borrow = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(borrow, File.ReadAllText(borrow).Replace(
            "  route: /api/loans\n",
            "  route: /api/lending/loans\n",
            StringComparison.Ordinal));

        var drifted = TestCli.Run("generate", "--root", temp.Path, "--check");
        Assert.Equal(1, drifted.Status);
        Assert.Contains("apps/api/src/Modules/Library.Lending/LendingEndpoints.generated.cs", drifted.Stdout, StringComparison.Ordinal);
        Assert.Contains("1 files would change.", drifted.Stdout, StringComparison.Ordinal);

        var checked_ = TestCli.Run("model", "check", "--root", temp.Path);
        AssertDriftReported(checked_.Stdout, "apps/api/src/Modules/Library.Lending/LendingEndpoints.generated.cs");
    }

    /// <summary>An input rename lists the endpoints file as one that would change, and the model check reports it as drifted.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateCheckListsEndpointsFileAfterAnInputRename()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);

        var borrow = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(borrow, File.ReadAllText(borrow).Replace(
            "- name: copyId\n",
            "- name: copy\n",
            StringComparison.Ordinal));

        var drifted = TestCli.Run("generate", "--root", temp.Path, "--check");
        Assert.Equal(1, drifted.Status);
        Assert.Contains("apps/api/src/Modules/Library.Lending/LendingEndpoints.generated.cs", drifted.Stdout, StringComparison.Ordinal);
        Assert.Contains("1 files would change.", drifted.Stdout, StringComparison.Ordinal);

        var checked_ = TestCli.Run("model", "check", "--root", temp.Path);
        AssertDriftReported(checked_.Stdout, "apps/api/src/Modules/Library.Lending/LendingEndpoints.generated.cs");
    }

    /// <summary>An input rename lists both the message file and the declarations file that reads its members, and the model check reports both as drifted.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateCheckListsDeclarationsFileAfterAMemberRename()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);

        RenameBorrowCopyInput(temp.Path);

        var drifted = TestCli.Run("generate", "--root", temp.Path, "--check");
        Assert.Equal(1, drifted.Status);
        Assert.Contains("apps/api/src/Modules/Library.Lending/Application/Loans/BorrowCopy/BorrowCopyCommand.generated.cs", drifted.Stdout, StringComparison.Ordinal);
        Assert.Contains("apps/api/src/Modules/Library.Lending/LendingDeclarations.generated.cs", drifted.Stdout, StringComparison.Ordinal);
        Assert.Contains("2 files would change.", drifted.Stdout, StringComparison.Ordinal);

        var checked_ = TestCli.Run("model", "check", "--root", temp.Path);
        AssertDriftReported(checked_.Stdout, "apps/api/src/Modules/Library.Lending/Application/Loans/BorrowCopy/BorrowCopyCommand.generated.cs");
        AssertDriftReported(checked_.Stdout, "apps/api/src/Modules/Library.Lending/LendingDeclarations.generated.cs");
    }

    /// <summary>One generate run after a member rename leaves nothing to change: the declarations follow the message in the same run.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateRewritesDeclarationsInTheSameRunAsTheMessage()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);

        RenameBorrowCopyInput(temp.Path);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);

        var drifted = TestCli.Run("generate", "--root", temp.Path, "--check");
        Assert.Equal(0, drifted.Status);
        Assert.Contains("0 files would change.", drifted.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A version-only header difference stays current: a candidate suffix on the stamped release is no drift.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateCheckIgnoresAVersionOnlyHeaderDifference()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path).Status);

        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingEndpoints.generated.cs");
        var text = File.ReadAllText(file);
        var endOfFirstLine = text.IndexOf('\n', StringComparison.Ordinal);
        var head = System.Text.RegularExpressions.Regex.Replace(
            text[..endOfFirstLine], @"LiteSpec \S+\.", "LiteSpec 7.0.0-rc.9.");
        Assert.Contains("LiteSpec 7.0.0-rc.9.", head, StringComparison.Ordinal);
        File.WriteAllText(file, head + text[endOfFirstLine..]);

        var drifted = TestCli.Run("generate", "--root", temp.Path, "--check");
        Assert.Equal(0, drifted.Status);
        Assert.Contains("0 files would change.", drifted.Stdout, StringComparison.Ordinal);

        var checked_ = TestCli.Run("model", "check", "--root", temp.Path);
        AssertDriftAbsent(checked_.Stdout, "apps/api/src/Modules/Library.Lending/LendingEndpoints.generated.cs");
    }

    /// <summary>Renames the borrow-copy input with its idempotency key placeholder and audit target, as one record change.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void RenameBorrowCopyInput(string root)
    {
        var borrow = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        var yaml = File.ReadAllText(borrow)
            .Replace("- name: copyId\n", "- name: copy\n", StringComparison.Ordinal)
            .Replace("key: \"{copyId}-for-{readerId}\"\n", "key: \"{copy}-for-{readerId}\"\n", StringComparison.Ordinal)
            .Replace("  target: copyId\n", "  target: copy\n", StringComparison.Ordinal);
        File.WriteAllText(borrow, yaml);
    }

    /// <summary>Asserts one model check line reports the file as generated-drift.</summary>
    /// <param name="stdout">The model check standard output.</param>
    /// <param name="relative">The generated file path with forward slashes.</param>
    static void AssertDriftReported(string stdout, string relative) =>
        Assert.Contains(
            stdout.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'),
            line => line.Contains("generated-drift", StringComparison.Ordinal) && line.Contains(relative, StringComparison.Ordinal));

    /// <summary>Asserts no model check line reports the file as generated-drift.</summary>
    /// <param name="stdout">The model check standard output.</param>
    /// <param name="relative">The generated file path with forward slashes.</param>
    static void AssertDriftAbsent(string stdout, string relative) =>
        Assert.DoesNotContain(
            stdout.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'),
            line => line.Contains("generated-drift", StringComparison.Ordinal) && line.Contains(relative, StringComparison.Ordinal));

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
