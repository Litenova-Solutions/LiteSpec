using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> usage, exit codes, output shape, conventions and departures.</summary>
public sealed class RulesCommandTests
{
    /// <summary>Help prints the usage text and exits zero.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void RulesHelpPrintsUsage()
    {
        var result = TestCli.Run("rules", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage:", result.Output);
    }

    /// <summary>No target prints the usage text and exits 2.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void RulesNoTargetPrintsUsage()
    {
        var result = TestCli.Run("rules", "--root", TestCli.Fixture("rules-basic"));

        Assert.Equal(2, result.Status);
        Assert.Contains("Usage:", result.Output);
    }

    /// <summary>An unknown option is a usage error and exits 2.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void RulesUnknownOptionExits2()
    {
        var result = TestCli.Run("rules", "--root", TestCli.Fixture("rules-basic"), "--diff", "apps/api/src/Demo/X.cs");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option", result.Output);
    }

    /// <summary>A root without a project file exits 2.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void RulesMissingProjectFileExits2()
    {
        using var temp = TestCli.Temp("rules-no-project");
        var result = TestCli.Run("rules", "--root", temp.Path, "apps/api/src/Demo/X.cs");

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.yaml", result.Output);
    }

    /// <summary>A target naming no file or record exits 1.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void RulesUnknownTargetExits1()
    {
        var result = TestCli.Run("rules", "--root", TestCli.Fixture("rules-basic"), "apps/api/src/Demo/NoSuchFile.cs");

        Assert.Equal(1, result.Status);
        Assert.Contains("No file or record", result.Output);
    }

    /// <summary>A handler path lists grouped one-line provisions with their reasons.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesHandlerListsGroupedProvisions()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "Rules for apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs (stack: dotnet, concern: application, artifact: command-handler)",
            result.Output);
        Assert.Contains("Required:", result.Output);
        Assert.Contains("Concern-wide:", result.Output);
        Assert.Contains("- litespec/backend/application#organize-application-by-use-case", result.Output);
        Assert.Contains("path match (artifact command-handler)", result.Output);
        Assert.Contains("record use-case/spending.submit-expense", result.Output);
    }

    /// <summary>A checked provision prints as one line with its analyzer ID and without its statement.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesCheckedProvisionsPrintOneLineWithCodes()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("Checked by a tool:", result.Output);
        Assert.Contains("- litespec/backend/dotnet/application#bind-command-handlers-to-load-call-and-stage: checked by `LS2012`, `LS2013`, `LS2014`, `LS2015` [path match (artifact command-handler)]", result.Output);
        Assert.DoesNotContain("A command handler MUST", result.Output);
    }

    /// <summary>A layer-wide provision prints by identifier only and without its statement.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesLayerWideProvisionsPrintByIdOnly()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("Concern-wide:", result.Output);
        Assert.Contains("- litespec/backend/application#organize-application-by-use-case", result.Output);
        Assert.DoesNotContain("Each command or query MUST own one use case folder", result.Output);
    }

    /// <summary>Full output prints the checked and layer-wide statements that the compact output shortens.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesFullPrintsCheckedAndLayerWideStatements()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Contains("Each command MUST implement one use case with one success path", result.Output);
        Assert.Contains("Each command or query MUST own one use case folder", result.Output);
    }

    /// <summary>A code path in a project that claims no code returns no provisions and says why.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesIntentAndBehaviorCodePathReturnsNoProvisionsAndSaysWhy()
    {
        using var root = CopyFixtureWithClaim("rules-basic", "intent-and-behavior");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("No provisions apply to a code path: the project claims neither the Implementation nor the Proof layer, so provisions about code do not apply. Query a record id for the provisions about records.", result.Output);
        Assert.DoesNotContain("litespec/backend/application#keep-command-handlers-to-load-call-and-stage", result.Output);
        Assert.DoesNotContain("demo/conventions#confirm-large-payouts", result.Output);
    }

    /// <summary>A record identifier in a project that claims intent and behavior returns the provisions about records and no code provisions.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesIntentAndBehaviorRecordIdReturnsRecordsProvisions()
    {
        using var root = CopyFixtureWithClaim("rules-basic", "intent-and-behavior");
        var result = TestCli.Run("rules", "--root", root.Path, "use-case/billing.send-invoice");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules for use-case/billing.send-invoice (concern: records, artifact: use-case)", result.Output);
        Assert.Contains("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
        Assert.DoesNotContain("- litespec/backend/", result.Output);
    }

    /// <summary>A code path in a project that claims its own code and proof returns no provisions about LiteSpec's code.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesOwnCodePathExcludesLiteSpecCodeProvisions()
    {
        using var root = CopyFixtureWithClaim("rules-basic", "own-code-and-proof");
        var result = TestCli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("litespec/backend/application#keep-command-handlers-to-load-call-and-stage", result.Output);
        Assert.DoesNotContain("litespec/backend/application#implement-one-outcome-path-per-command", result.Output);
    }

    /// <summary>A record identifier lists the provisions that govern the record.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRecordIdListsProvisions()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "use-case/spending.submit-expense");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules for use-case/spending.submit-expense (concern: records, artifact: use-case)", result.Output);
        Assert.Contains("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
        Assert.Contains("record use-case/spending.submit-expense", result.Output);
    }

    /// <summary>Full output adds the rationale and example under each item.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesFullPrintsRationaleAndExample()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseEndpoint.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rationale: One endpoint class per route operation", result.Output);
        Assert.Contains("Example: At startup, the application discovers endpoint implementations", result.Output);
    }

    /// <summary>A convention whose scope matches the path prints beside the LiteSpec provisions.</summary>
    /// <proves>litespec/core/scopes#record-each-replaced-default</proves>
    [Fact]
    public void RulesConventionAppearsForMatchingPath()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("- demo/conventions#confirm-large-payouts: A payout above 1,000 euros asks for one confirmation step before it runs. [path match (apps/api/src/**)]", result.Output);
        Assert.DoesNotContain("show-a-loading-state-on-every-route", result.Output);
    }

    /// <summary>A live departure replaces the provision its covers names; an expired one does not.</summary>
    /// <proves>litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure</proves>
    [Fact]
    public void RulesDepartureReplacesCoveredProvision()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "- litespec/backend/application#keep-command-handlers-to-load-call-and-stage: [departure departure/slow-review-departure, expires 2030-01-01] The billing handler calls two aggregates until the split ships.",
            result.Output);
        Assert.Contains("- litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case", result.Output);
        Assert.DoesNotContain("old-layout-departure", result.Output);
    }

    /// <summary>Full output prints the departure consequences instead of the provision example.</summary>
    /// <proves>litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure</proves>
    [Fact]
    public void RulesFullPrintsDepartureConsequences()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Contains("Consequences: The handler loads the invoice and the ledger in one call until the split ships.", result.Output);
    }

    /// <summary>A layer-projects endpoint derives its layer from the project name.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesLayeredEndpointUsesProjectName()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-layered"),
            "apps/api/src/Demo.WebApi/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "Rules for apps/api/src/Demo.WebApi/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs (stack: dotnet, concern: web-api, artifact: endpoint)",
            result.Output);
        Assert.Contains("- litespec/backend/api#map-one-operation-per-endpoint:", result.Output);
    }

    /// <summary>Copies one fixture to a temporary root with its layer claim replaced.</summary>
    /// <param name="fixture">The fixture name.</param>
    /// <param name="claim">The claim name, as <see cref="LiteSpec.Cli.Tests.Consumer.ConsumerFixture.Claim" /> reads it.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixtureWithClaim(string fixture, string claim)
    {
        var source = TestCli.Fixture(fixture);
        var temp = TestCli.Temp("rules-level");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        var project = Path.Combine(temp.Path, ".litespec", "project.yaml");
        var declared = DataFile.Read(project)!.AsObject();
        declared["layers"] = LiteSpec.Cli.Tests.Consumer.ConsumerFixture.Claim(claim);
        File.WriteAllText(project, declared.ToJsonString());

        return temp;
    }
}
