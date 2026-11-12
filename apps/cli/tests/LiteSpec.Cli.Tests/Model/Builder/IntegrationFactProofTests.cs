using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves an integration fact confirms a rule or a scenario from inside the host.</summary>
public sealed class IntegrationFactProofTests
{
    /// <summary>A fact resolving a service and sending a governed message confirms its rule claim.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RuleFactSendingAGovernedMessageConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/RuleMessageFactTests.cs", RuleMessageFile);

        using var document = Build(temp.Path);

        Assert.True(ProvesLink(document.RootElement, "rule/loan.has-one-borrower").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A fact working the governed aggregate's store confirms its rule claim.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RuleFactWorkingTheGovernedStoreConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/RuleStoreFactTests.cs", RuleStoreFile);

        using var document = Build(temp.Path);

        Assert.True(ProvesLink(document.RootElement, "rule/lending.copy-barcode-unique").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A fact calling the governed aggregate confirms its rule claim.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RuleFactCallingTheGovernedAggregateConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/RuleStaticFactTests.cs", RuleStaticFile);

        using var document = Build(temp.Path);

        Assert.True(ProvesLink(document.RootElement, "rule/lending.copy-barcode-unique").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A fact that acts without asserting confirms no rule claim.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void RuleFactWithoutAnAssertionDoesNotConfirm()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/RuleSilentFactTests.cs", RuleSilentFile);

        using var document = Build(temp.Path);

        Assert.False(ProvesLink(document.RootElement, "rule/loan.has-one-borrower").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A fact pinning the message type with typeof confirms its scenario claim.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void ScenarioFactPinningTheMessageTypeConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/success-scenario.yaml", SuccessScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/TypeofFactTests.cs", TypeofFile);

        using var document = Build(temp.Path);

        Assert.True(ProvesLink(document.RootElement, "scenario/borrow-copy.borrowing-an-available-copy-succeeds").GetProperty("confirmed").GetBoolean());
    }

    /// <summary>A nested value sharing an aggregate's name does not read as constructing it.</summary>
    /// <proves>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</proves>
    [Fact]
    public void ScenarioFactBuildingANestedValueConfirms()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/success-scenario.yaml", SuccessScenarioJson);
        Write(temp.Path, "apps/api/tests/Library.Integration.Tests/Strength/NestedValueFactTests.cs", NestedFile);

        using var document = Build(temp.Path);

        Assert.True(ProvesLink(document.RootElement, "scenario/borrow-copy.borrowing-an-available-copy-succeeds").GetProperty("confirmed").GetBoolean());
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

    const string RuleMessageFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class RuleMessageFactTests(TestApi api)
        {
            /// <proves>rule/loan.has-one-borrower</proves>
            [Fact]
            public async Task ALoanKeepsItsSingleBorrower()
            {
                await using var scope = api.Services.CreateAsyncScope();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
                var outcome = await commands.SendAsync(new BorrowCopyCommand { CopyId = "copy-1" });
                Assert.NotNull(outcome);
            }
        }
        """;

    const string RuleStoreFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class RuleStoreFactTests(TestApi api)
        {
            /// <proves>rule/lending.copy-barcode-unique</proves>
            [Fact]
            public async Task BarcodesStayUnique()
            {
                await using var scope = api.Services.CreateAsyncScope();
                var copies = scope.ServiceProvider.GetRequiredService<ICopyRepository>();
                var found = await copies.FindByBarcodeAsync("barcode-1");
                Assert.NotNull(found);
            }
        }
        """;

    const string RuleStaticFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class RuleStaticFactTests(TestApi api)
        {
            /// <proves>rule/lending.copy-barcode-unique</proves>
            [Fact]
            public async Task RegisteringACopyKeepsItsBarcode()
            {
                await using var scope = api.Services.CreateAsyncScope();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
                var copy = Copy.Register("barcode-1");
                Assert.NotNull(copy);
            }
        }
        """;

    const string RuleSilentFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class RuleSilentFactTests(TestApi api)
        {
            /// <proves>rule/loan.has-one-borrower</proves>
            [Fact]
            public async Task ActingWithoutAsserting()
            {
                await using var scope = api.Services.CreateAsyncScope();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
                await commands.SendAsync(new BorrowCopyCommand { CopyId = "copy-1" });
            }
        }
        """;

    const string TypeofFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class TypeofFactTests(TestApi api)
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            [Fact]
            public async Task BorrowingThroughTheHost()
            {
                _ = typeof(BorrowCopyCommand);
                await using var scope = api.Services.CreateAsyncScope();
                var session = scope.ServiceProvider.GetRequiredService<ISession>();
                var outcome = await session.ApplyAsync();
                Assert.NotNull(outcome);
            }
        }
        """;

    const string NestedFile = """
        namespace Library.Integration.Tests.Strength;

        public sealed class NestedValueFactTests
        {
            /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
            [Fact]
            public async Task BorrowingBesideANestedValue()
            {
                var command = new BorrowCopyCommand { CopyId = "copy-1" };
                var lookup = new LoanLookup.Loan("loan-1");
                var outcome = await gateway.SendAsync(command);
                Assert.NotNull(outcome);
            }
        }
        """;
}
