using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>
///     Proves the shared refusal rule in the CLI: a bare refusal code is the
///     expected code, and a refusal pointing at a success result names no code.
/// </summary>
public sealed class RefusalCodeCheckTests
{
    /// <summary>Bare Refusal With A Wrong Code Reports Runtime Value Drift.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact]
    public void BareRefusalWithAWrongCodeReportsRuntimeValueDrift()
    {
        using var temp = WriteConsumer();
        var drifts = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "runtime-value-drift");

        var finding = Assert.Single(drifts, found => found.Subject == "rule/loan.receipt-required");
        Assert.Contains("WrongCodeException", finding.Message);
        Assert.Contains("lending.wrong-code", finding.Message);
        Assert.Contains("lending.receipt-missing", finding.Message);
    }

    /// <summary>Bare Refusal With The Equal Code Reports No Drift.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact]
    public void BareRefusalWithTheEqualCodeReportsNoDrift()
    {
        using var temp = WriteConsumer();
        var drifts = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "runtime-value-drift");

        Assert.DoesNotContain(drifts, finding => finding.Message.Contains("EqualCodeException"));
    }

    /// <summary>Refusal To A Success Result Reports No Drift.</summary>
    /// <proves>litespec/backend/dotnet/stack#generate-runtime-values-from-the-records</proves>
    [Fact]
    public void RefusalToASuccessResultReportsNoDrift()
    {
        using var temp = WriteConsumer();
        var drifts = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "runtime-value-drift");

        Assert.DoesNotContain(drifts, finding => finding.Subject == "rule/loan.hold-allowed");
    }

    /// <summary>Writes a consumer with a bare refusal, an equal code, and a success refusal.</summary>
    /// <returns>The temporary folder holding the consumer.</returns>
    static TempFolder WriteConsumer()
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
                  "reviewedLiteSpecVersion": "5.1.0",
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
                  },
                  "vocabulary": {
                    "verbs": [
                      "borrow"
                    ]
                  }
                }
                """);
            Write(temp.Path, ".litespec/glossary.yaml", """
                schemaVersion: 2
                terms: []
                mannered: []
                """);
            TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/aggregate.yaml", """
                {
                  "type": "aggregate",
                  "id": "aggregate/lending.loan",
                  "title": "Loan",
                  "owner": "lending-team",
                  "status": "reviewed",
                  "states": [
                    {
                      "name": "Active",
                      "meaning": "The loan is open."
                    }
                  ],
                  "records": [
                    {
                      "type": "rule",
                      "id": "rule/loan.receipt-required",
                      "title": "Receipt required",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "kind": "precondition",
                      "statement": "A borrowed copy carries a receipt.",
                      "refusal": "lending.receipt-missing"
                    },
                    {
                      "type": "rule",
                      "id": "rule/loan.hold-allowed",
                      "title": "Hold allowed",
                      "owner": "lending-team",
                      "status": "reviewed",
                      "kind": "precondition",
                      "statement": "A held copy may wait.",
                      "refusal": "result/place-hold.hold-waiting"
                    },
                    {
                      "type": "result",
                      "id": "result/place-hold.hold-waiting",
                      "title": "Hold waiting",
                      "owner": "lending-team",
                      "outcome": "success",
                      "code": "lending.hold-waiting",
                      "meaning": "The hold waits."
                    }
                  ]
                }
                """);
            TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/loans/aggregate.md", """
                # Loan

                A loan lends one copy to one reader.
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/DomainException.cs", """
                namespace Library.Domain;

                /// <summary>The base of every business rejection in the fixture library.</summary>
                public abstract class DomainException(string message) : Exception(message)
                {
                    /// <summary>The stable failure code.</summary>
                    public abstract string Code { get; }
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/Exceptions/WrongCodeException.cs", """
                namespace Library.Domain.Lending.Loans.Exceptions;

                /// <implements>rule/loan.receipt-required</implements>
                public sealed class WrongCodeException()
                    : DomainException("A receipt is required.")
                {
                    /// <summary>The stable failure code.</summary>
                    public override string Code => "lending.wrong-code";
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/Exceptions/EqualCodeException.cs", """
                namespace Library.Domain.Lending.Loans.Exceptions;

                /// <implements>rule/loan.receipt-required</implements>
                public sealed class EqualCodeException()
                    : DomainException("A receipt is required.")
                {
                    /// <summary>The stable failure code.</summary>
                    public override string Code => "lending.receipt-missing";
                }
                """);
            Write(temp.Path, "apps/api/src/Library.Domain/Lending/Loans/Exceptions/SuccessRefusalException.cs", """
                namespace Library.Domain.Lending.Loans.Exceptions;

                /// <implements>rule/loan.hold-allowed</implements>
                public sealed class SuccessRefusalException()
                    : DomainException("A hold may wait.")
                {
                    /// <summary>The stable failure code.</summary>
                    public override string Code => "lending.other-code";
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

    static void Write(string root, string relative, string text)
    {
        var destination = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text);
    }
}
