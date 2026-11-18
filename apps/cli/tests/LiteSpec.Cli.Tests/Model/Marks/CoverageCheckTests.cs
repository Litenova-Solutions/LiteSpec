using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves the record-side traceability findings against the marks fixtures.</summary>
public sealed class CoverageCheckTests
{
    /// <summary>Aggregate Without A Marked Root Reports Aggregate Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void AggregateWithoutAMarkedRootReportsAggregateUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <summary>Entity Aggregate Without A Root Still Reports.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void EntityAggregateWithoutARootStillReports()
    {
        using var temp = WriteKindConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "aggregate-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    /// <summary>Reaction Without A Marked Handler Reports Reaction Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void ReactionWithoutAMarkedHandlerReportsReactionUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "reaction-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("reaction/lending.send-due-notice-on-loan-borrowed", finding.Subject);
    }

    /// <summary>Job Without A Marked Host Reports Job Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void JobWithoutAMarkedHostReportsJobUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "job-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("job/lending.nightly-due-scan", finding.Subject);
    }

    /// <summary>System Without A Marked Port Reports External System Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void SystemWithoutAMarkedPortReportsExternalSystemUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "external-system-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("external-system/text-gateway", finding.Subject);
    }

    /// <summary>Browser Use Case Without An Endpoint Reports Use Case Unexposed.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void BrowserUseCaseWithoutAnEndpointReportsUseCaseUnexposed()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-endpoint-missing");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <summary>Browser Use Case Without A Screen Reports Use Case Unreached.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void BrowserUseCaseWithoutAScreenReportsUseCaseUnreached()
    {
        using var fixture = MarksFixture.Prepare("exposed");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-unreached");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("entry-point/library-app", finding.Message);
    }

    /// <summary>Declared Edge No Code Takes Reports Effect Unimplemented.</summary>
    /// <proves>litespec/core/system#declare-every-edge-between-records</proves>
    [Fact]
    public void DeclaredEdgeNoCodeTakesReportsEffectUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("effects");
        var findings = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "effect-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("sends notification/lending.loan-due", finding.Message);
    }

    /// <summary>A job host registering its command and a job sending through a resolved mediator report no unimplemented edge.</summary>
    /// <proves>litespec/core/system#declare-every-edge-between-records</proves>
    [Fact]
    public void JobHostsTakingTheirRunsEdgeReportNothing()
    {
        using var temp = WriteJobConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "effect-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("job/lending.abandon-sweep", finding.Subject);
    }

    /// <summary>A job running a command whose handler marks the use case reports no unimplemented edge for it.</summary>
    /// <proves>litespec/core/system#declare-every-edge-between-records</proves>
    [Fact]
    public void JobRunningAliasedCommandReportsNothing()
    {
        using var temp = WriteJobConsumer();
        var findings = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "effect-unimplemented");

        var finding = Assert.Single(findings);
        Assert.Equal("job/lending.abandon-sweep", finding.Subject);
    }

    /// <summary>Writes a consumer in the module layout where one job registers its command, one sends it through a resolved mediator, one runs a command named apart from its use case, and one has no host.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteJobConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "7.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Modules/*",
                      "kernel": "apps/api/src/Library.Kernel",
                      "platform": "apps/api/src/Library.Platform"
                    }
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow",
                      "renew",
                      "abandon",
                      "lend"
                    ]
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            foreach (var (useCase, title) in new[] { ("borrow-copy", "Borrow copy"), ("renew-loan", "Renew loan"), ("abandon-loan", "Abandon loan"), ("lend-copy", "Lend copy") })
            {
                TestRecords.Write(temp.Path, $".litespec/behavior/modules/lending/aggregates/loans/{useCase}.yaml", """
                    {
                      "type": "use-case",
                      "id": "use-case/lending.USECASE",
                      "title": "TITLE",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "kind": "command",
                      "records": [
                        {
                          "type": "result",
                          "id": "result/USECASE.done",
                          "title": "Done",
                          "owner": "lending-team",
                          "outcome": "success",
                          "meaning": "The copy is handled."
                        }
                      ]
                    }
                    """.Replace("USECASE", useCase).Replace("TITLE", title));
                TestRecords.Explain(temp.Path, $".litespec/behavior/modules/lending/aggregates/loans/{useCase}.md", $"""
                    # {title}

                    A reader handles a copy.
                    """);
            }

            foreach (var (job, runs) in new[] { ("borrow-sweep", "borrow-copy"), ("renew-sweep", "renew-loan"), ("abandon-sweep", "abandon-loan"), ("lend-sweep", "lend-copy") })
            {
                TestRecords.Write(temp.Path, $".litespec/behavior/modules/lending/jobs/{job}/{job}.yaml", """
                    {
                      "type": "job",
                      "id": "job/lending.JOB",
                      "title": "TITLE",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "kind": "scheduled",
                      "schedule": {"every": "P1D", "timezone": "UTC"},
                      "selection": "Copies past their due date.",
                      "effects": [{"runs": "use-case/lending.RUNS", "timing": "inline"}],
                      "outcomes": [{"name": "completed", "meaning": "The sweep handled the due copies."}],
                      "onFailure": {"mode": "skip-and-log"}
                    }
                    """.Replace("JOB", job).Replace("TITLE", job).Replace("RUNS", runs));
                TestRecords.Explain(temp.Path, $".litespec/behavior/modules/lending/jobs/{job}/{job}.md", $"""
                    # {job}

                    A sweep handles the due copies.
                    """);
            }

            Write(temp.Path, "apps/api/src/Modules/Library.Lending/UseCases/BorrowCopy/Application/BorrowCopySchedule.cs", """
                namespace Library.Lending.UseCases.BorrowCopy.Application;

                /// <implements>job/lending.borrow-sweep</implements>
                internal static class BorrowCopySchedule
                {
                    public static void Register(CommandModuleBuilder commands)
                    {
                        commands.AddRecurring(() => new BorrowCopyCommand(Guid.NewGuid()));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/UseCases/RenewLoan/Application/RenewLoanSweepJob.cs", """
                namespace Library.Lending.UseCases.RenewLoan.Application;

                /// <implements>job/lending.renew-sweep</implements>
                internal sealed class RenewLoanSweepJob : BackgroundService
                {
                    private readonly IServiceScopeFactory scopes;

                    public RenewLoanSweepJob(IServiceScopeFactory scopes)
                    {
                        this.scopes = scopes;
                    }

                    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
                    {
                        using var scope = scopes.CreateScope();
                        var commands = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
                        var renew = new RenewLoanCommand(Guid.NewGuid());
                        await commands.SendAsync(renew, stoppingToken);
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/UseCases/LendCopy/Application/LendCopySchedule.cs", """
                namespace Library.Lending.UseCases.LendCopy.Application;

                /// <implements>job/lending.lend-sweep</implements>
                internal static class LendCopySchedule
                {
                    public static void Register(CommandModuleBuilder commands)
                    {
                        commands.AddRecurring(() => new LendOverdueCopiesCommand(Guid.NewGuid()));
                    }
                }
                """);
            Write(temp.Path, "apps/api/src/Modules/Library.Lending/UseCases/LendCopy/Application/LendOverdueCopiesCommandHandler.cs", """
                namespace Library.Lending.UseCases.LendCopy.Application;

                /// <implements>use-case/lending.lend-copy</implements>
                internal sealed class LendOverdueCopiesCommandHandler : ICommandHandler<LendOverdueCopiesCommand, LendOverdueCopiesResult>
                {
                    public Task<LendOverdueCopiesResult> HandleAsync(LendOverdueCopiesCommand command, CancellationToken cancellationToken)
                    {
                        throw new NotImplementedException();
                    }
                }
                """);
            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    /// <summary>Writes a consumer where an aggregate has no root in code.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteKindConsumer()
    {
        var temp = TestCli.Temp("marks");

        try
        {
            Write(temp.Path, ".litespec/project.yaml", """
                {
                  "project": {
                    "name": "Marks Fixture"
                  },
                  "size": "small",
                  "shape": "server",
                  "stacks": [
                    "dotnet"
                  ],
                  "layers": {"intent": "litespec", "behavior": "litespec", "implementation": "litespec", "proof": "litespec"},
                  "reviewedLiteSpecVersion": "7.0.0",
                  "owners": [
                    "lending-team"
                  ],
                  "paths": {
                    "docs": ".litespec",
                    "behaviorDocs": ".litespec/behavior",
                    "glossary": ".litespec/glossary.yaml",
                    "testRoots": [
                      "apps/api/tests"
                    ],
                    "frontends": []
                  },
                  "stackConfig": {
                    "dotnet": {
                      "modules": "apps/api/src/Library.Application",
                      "domain": "apps/api/src/Library.Domain",
                      "webapi": "apps/api/src/Library.WebApi",
                      "infrastructure": "apps/api/src/Library.Infrastructure"
                    }
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.md", """
                # Loan

                A loan lends one copy to one reader.
                """);

            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
