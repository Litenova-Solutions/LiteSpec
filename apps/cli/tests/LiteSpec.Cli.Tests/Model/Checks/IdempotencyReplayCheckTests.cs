using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the idempotency replay check: a keyed command's declaration replays the first result exactly when its record states returns-first-result.</summary>
public sealed class IdempotencyReplayCheckTests
{
    /// <summary>A declaration replaying the first result for a record stating returns-first-result reports nothing.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void MatchingReplayReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: true)");

        Assert.Empty(Run(model));
    }

    /// <summary>A declaration replaying nothing for a record stating returns-first-result reports idempotency-replay-drift.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void MissingReplayInCodeReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: false)");

        var finding = Assert.Single(Run(model));

        Assert.Equal("idempotency-replay-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares no replay of the first result, but 'use-case/lending.borrow-copy' states replay 'returns-first-result'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A declaration replaying nothing for a record stating refuses reports nothing.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void MatchingRefusalReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "refuses"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: false)");

        Assert.Empty(Run(model));
    }

    /// <summary>A declaration replaying the first result for a record stating refuses reports idempotency-replay-drift.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void UnexpectedReplayInCodeReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "refuses"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: true)");

        var finding = Assert.Single(Run(model));

        Assert.Equal("idempotency-replay-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares a replay of the first result, but 'use-case/lending.borrow-copy' states replay 'refuses'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A declaration replaying nothing for a record stating acts-again reports nothing.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void MatchingActAgainReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "acts-again"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: false)");

        Assert.Empty(Run(model));
    }

    /// <summary>A declaration replaying the first result for a record stating acts-again reports idempotency-replay-drift.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void UnexpectedReplayForActAgainReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "acts-again"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: true)");

        var finding = Assert.Single(Run(model));

        Assert.Equal("idempotency-replay-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares a replay of the first result, but 'use-case/lending.borrow-copy' states replay 'acts-again'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A keyed declaration without the replay flag replays nothing, so a record stating returns-first-result reports drift.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void OmittedFlagReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\")");

        Assert.Contains(Run(model), finding => finding.Code == "idempotency-replay-drift");
    }

    /// <summary>A declaration setting ReplayResult in an initializer for a record stating returns-first-result reports nothing.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void PropertyInitializerMatchReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "new IdempotencyDeclaration(static message => \"copy-id\") { ReplayResult = true }");

        Assert.Empty(Run(model));
    }

    /// <summary>A declaration clearing ReplayResult in an initializer for a record stating returns-first-result reports idempotency-replay-drift.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void PropertyInitializerMismatchReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "new IdempotencyDeclaration(static message => \"copy-id\") { ReplayResult = false }");

        var finding = Assert.Single(Run(model));

        Assert.Equal("idempotency-replay-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares no replay of the first result, but 'use-case/lending.borrow-copy' states replay 'returns-first-result'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A state command with an exempting declaration reports nothing: the aggregate answers the repeat, not the pipeline.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void StateModeWithExemptionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "state", "recognizedBy": "the copy already lent to the member", "replay": "refuses"}}""");
        File.WriteAllText(
            Path.Combine(folder, "BorrowCopyCommandIdempotencyDefinition.cs"),
            "using LiteBus.Messaging.Abstractions;\n\ninternal sealed class BorrowCopyCommandIdempotencyDefinition : IMessageDefinition<BorrowCopyCommand>\n{\n    public void Describe(IMessageDeclarations declarations) => declarations.Exempt<IdempotencyDeclaration>(\"recognized by state\");\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>A keyed command without a stated replay reports nothing here: the record check owns the missing replay.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void MissingReplayInRecordReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant"}}""");
        WriteDefinition(folder, "IdempotencyDeclaration.Idempotent(static message => \"copy-id\", scope: \"tenant\", replayResult: true)");

        Assert.Empty(Run(model));
    }

    /// <summary>A keyed command without its definition file reports nothing here: the definition check owns the missing file.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void MissingDefinitionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "returns-first-result"}}""");

        Assert.Empty(Run(model));
    }

    /// <summary>A definition file that does not parse reports nothing here: its shape cannot be trusted.</summary>
    /// <proves>litespec/core/use-cases#state-what-a-replay-does</proves>
    [Fact]
    public void UnparseableDefinitionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = ReplayModel(temp.Path, folder, """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant", "replay": "refuses"}}""");
        File.WriteAllText(Path.Combine(folder, "BorrowCopyCommandIdempotencyDefinition.cs"), "internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>\n{\n    public IdempotencyDeclaration Idempotency { get; } = IdempotencyDeclaration.Idempotent(static message =>\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the replay check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The replay findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        IdempotencyReplayChecks.Run(model, findings);

        return findings.Where(finding => finding.Code is "idempotency-replay-drift").ToList();
    }

    /// <summary>A model with the use case record and its handler in the Application folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="folder">The Application folder holding the handler.</param>
    /// <param name="json">The use case record fields as a JSON object.</param>
    /// <returns>The project model holding the record and its handler.</returns>
    static ProjectModel ReplayModel(string root, string folder, string json)
    {
        var model = TestModel.Model(
            TestModel.Config(root),
            TestModel.Record(RecordTypes.UseCase, "use-case/lending.borrow-copy", Path.Combine(root, ".litespec", "borrow-copy.yaml"), json));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            "BorrowCopyCommandHandler",
            MessageType: "BorrowCopyCommand"));

        return model;
    }

    /// <summary>The use case Application folder holding the handler and its definitions.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The Application folder.</returns>
    static string ApplicationFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "BorrowCopy");
        Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>Writes the idempotency definition file declaring the expression.</summary>
    /// <param name="folder">The Application folder holding the definition.</param>
    /// <param name="declaration">The declaration expression the Idempotency property holds.</param>
    static void WriteDefinition(string folder, string declaration)
    {
        File.WriteAllText(
            Path.Combine(folder, "BorrowCopyCommandIdempotencyDefinition.cs"),
            "using LiteBus.Messaging.Abstractions;\n\ninternal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>\n{\n    public IdempotencyDeclaration Idempotency { get; } = " + declaration + ";\n}\n");
    }
}
