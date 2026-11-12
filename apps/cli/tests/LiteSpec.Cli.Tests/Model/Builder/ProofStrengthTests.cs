using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves a scenario test asserts its result through the public boundary.</summary>
public sealed class ProofStrengthTests
{
    /// <summary>A steps test without a then step proves nothing.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void StepsTestWithoutThenStepDoesNotConfirm()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/success-scenario.yaml", SuccessScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Acceptance.Tests/Strength/NoThenScenarios.cs", NoThenFile);

        using var document = Build(temp.Path);

        Assert.False(ProvesLink(document.RootElement, "scenario/borrow-copy.borrowing-an-available-copy-succeeds").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A refusal code in setup, outside any assertion, proves no refusal.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void RefusalCodeOutsideAnAssertionDoesNotConfirm()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/refusal-scenario.yaml", RefusalScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Acceptance.Tests/Strength/SetupCodeScenarios.cs", SetupCodeFile);

        using var document = Build(temp.Path);

        Assert.False(ProvesLink(document.RootElement, "scenario/borrow-copy.borrowing-an-unavailable-copy-refuses").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A refusal code inside an assertion, sent through the message, confirms.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void RefusalCodeInsideAnAssertionConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/refusal-scenario.yaml", RefusalScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/RefusalAssertionTests.cs", RefusalAssertionFile);

        using var document = Build(temp.Path);

        Assert.True(ProvesLink(document.RootElement, "scenario/borrow-copy.borrowing-an-unavailable-copy-refuses").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A test constructing the handler directly proves nothing.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void TestConstructingAHandlerDoesNotConfirm()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/success-scenario.yaml", SuccessScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Acceptance.Tests/Strength/DirectScenarios.cs", DirectFile);

        using var document = Build(temp.Path);

        Assert.False(ProvesLink(document.RootElement, "scenario/borrow-copy.borrowing-an-available-copy-succeeds").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A rule claim without a scenario claim never confirms.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void RuleClaimWithoutAScenarioDoesNotConfirm()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/RuleOnlyTests.cs", RuleOnlyFile);

        using var document = Build(temp.Path);

        Assert.False(ProvesLink(document.RootElement, "rule/loan.has-one-borrower").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A rule claim confirms when the test also proves a governed scenario.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void RuleClaimWithAProvingScenarioConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/success-scenario.yaml", SuccessScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Acceptance.Tests/Strength/RuleAndScenarioScenarios.cs", RuleAndScenarioFile);

        using var document = Build(temp.Path);
        var root = document.RootElement;

        Assert.True(ProvesLink(root, "scenario/borrow-copy.borrowing-an-available-copy-succeeds").GetProperty("confirmed").GetBoolean());
        Assert.True(ProvesLink(root, "rule/loan.has-one-borrower").GetProperty("confirmed").GetBoolean());
    }

    static JsonElement ProvesLink(JsonElement root, string claim)
    {
        foreach (var link in root.GetProperty("links").GetProperty("proves").EnumerateArray())
        {
            if (link.GetProperty("claim").GetString() == claim)
            {
                return link;
            }
        }

        throw new InvalidOperationException($"No proves link claims '{claim}'.");
    }

    static JsonDocument Build(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string SuccessScenarioJson = """
        {
          "type": "scenario",
          "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
          "title": "Borrowing an available copy succeeds",
          "given": "A copy sits available on its shelf.",
          "when": "The reader borrows the copy.",
          "result": "result/borrow-copy.borrowed",
          "then": "The loan is active."
        }
        """;

    const string RefusalScenarioJson = """
        {
          "type": "scenario",
          "id": "scenario/borrow-copy.borrowing-an-unavailable-copy-refuses",
          "title": "Borrowing an unavailable copy refuses",
          "given": "A copy is lent out.",
          "when": "The reader borrows the copy.",
          "result": "result/borrow-copy.copy-unavailable",
          "then": "The copy stays shelved."
        }
        """;

    const string NoThenFile = """
        namespace Library.Acceptance.Tests.Strength;

        public sealed class NoThenScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            public Task Borrowing_without_a_then_step() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"));
        }
        """;

    const string SetupCodeFile = """
        namespace Library.Acceptance.Tests.Strength;

        public sealed class SetupCodeScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-unavailable-copy-refuses</proves>
            public Task Borrowing_an_unavailable_copy() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("lending.copy-not-available"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                then => new BorrowCopySteps().TheLoanIsActive());
        }
        """;

    const string RefusalAssertionFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class RefusalAssertionTests
        {
            /// <proves>scenario/borrow-copy.borrowing-an-unavailable-copy-refuses</proves>
            [Fact]
            public async Task BorrowingAnUnavailableCopyRefuses()
            {
                var command = new BorrowCopyCommand { CopyId = "copy-1" };
                var outcome = await gateway.SendAsync(command);
                Assert.Contains("lending.copy-not-available", outcome.Errors);
            }
        }
        """;

    const string DirectFile = """
        namespace Library.Acceptance.Tests.Strength;

        public sealed class DirectScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            public Task Borrowing_through_a_handler()
            {
                var handler = new BorrowCopyCommandHandler();
                return Runner.RunScenarioAsync(
                    given => new RegisterCopySteps().RegisterACopy("copy-1"),
                    when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                    then => new BorrowCopySteps().TheLoanIsActive());
            }
        }
        """;

    const string RuleOnlyFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class RuleOnlyTests
        {
            /// <proves>rule/loan.has-one-borrower</proves>
            [Fact]
            public async Task ALoanHasOneBorrower()
            {
                var command = new BorrowCopyCommand { CopyId = "copy-1" };
                var outcome = await gateway.SendAsync(command);
                Assert.NotNull(outcome);
            }
        }
        """;

    const string RuleAndScenarioFile = """
        namespace Library.Acceptance.Tests.Strength;

        public sealed class RuleAndScenarioScenarios
        {
            /// <proves>rule/loan.has-one-borrower</proves>
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            public Task Borrowing_with_its_rule() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                then => new BorrowCopySteps().TheLoanIsActive());
        }
        """;
}
