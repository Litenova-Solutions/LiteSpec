using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> usage, exit codes, output shape, conventions and departures.</summary>
public sealed class RulesCommandTests
{
    /// <summary>Help prints the usage text and exits zero.</summary>
    [Fact]
    public void Rules_Help_Prints_Usage()
    {
        var result = Cli.Run("rules", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("Usage:", result.Output);
    }

    /// <summary>No target prints the usage text and exits 2.</summary>
    [Fact]
    public void Rules_No_Target_Prints_Usage()
    {
        var result = Cli.Run("rules", "--root", Cli.Fixture("rules-basic"));

        Assert.Equal(2, result.Status);
        Assert.Contains("Usage:", result.Output);
    }

    /// <summary>An unknown option is a usage error and exits 2.</summary>
    [Fact]
    public void Rules_Unknown_Option_Exits_2()
    {
        var result = Cli.Run("rules", "--root", Cli.Fixture("rules-basic"), "--diff", "apps/api/src/Demo/X.cs");

        Assert.Equal(2, result.Status);
        Assert.Contains("Unknown option", result.Output);
    }

    /// <summary>A root without a project file exits 2.</summary>
    [Fact]
    public void Rules_Missing_Project_File_Exits_2()
    {
        using var temp = Cli.Temp("rules-no-project");
        var result = Cli.Run("rules", "--root", temp.Path, "apps/api/src/Demo/X.cs");

        Assert.Equal(2, result.Status);
        Assert.Contains("holds no .litespec/project.yaml", result.Output);
    }

    /// <summary>A target naming no file or record exits 1.</summary>
    [Fact]
    public void Rules_Unknown_Target_Exits_1()
    {
        var result = Cli.Run("rules", "--root", Cli.Fixture("rules-basic"), "apps/api/src/Demo/NoSuchFile.cs");

        Assert.Equal(1, result.Status);
        Assert.Contains("No file or record", result.Output);
    }

    /// <summary>A handler path lists grouped one-line provisions with their reasons.</summary>
    [Fact]
    public void Rules_Handler_Lists_Grouped_Provisions()
    {
        var root = Path.Combine(Cli.Repository, "examples", "claimwick");
        var result = Cli.Run(
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
    [Fact]
    public void Rules_Checked_Provisions_Print_One_Line_With_Codes()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("Checked by a tool:", result.Output);
        Assert.Contains("- litespec/backend/application#implement-one-outcome-path-per-command: checked by `LS2014` [path match (artifact command-handler)]", result.Output);
        Assert.DoesNotContain("Each command MUST implement one use case with one success path", result.Output);
    }

    /// <summary>A layer-wide provision prints by identifier only and without its statement.</summary>
    [Fact]
    public void Rules_Layer_Wide_Provisions_Print_By_Id_Only()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("Concern-wide:", result.Output);
        Assert.Contains("- litespec/backend/application#organize-application-by-use-case", result.Output);
        Assert.DoesNotContain("Each command or query MUST own one use case folder", result.Output);
    }

    /// <summary>Full output prints the checked and layer-wide statements that the compact output shortens.</summary>
    [Fact]
    public void Rules_Full_Prints_Checked_And_Layer_Wide_Statements()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Contains("Each command MUST implement one use case with one success path", result.Output);
        Assert.Contains("Each command or query MUST own one use case folder", result.Output);
    }

    /// <summary>A code path in a project that claims no code returns no provisions and says why.</summary>
    [Fact]
    public void Rules_Intent_And_Behavior_Code_Path_Returns_No_Provisions_And_Says_Why()
    {
        using var root = CopyFixtureWithClaim("rules-basic", "intent-and-behavior");
        var result = Cli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("No provisions apply to a code path: the project claims neither the Implementation nor the Proof layer, so provisions about code do not apply. Query a record id for the provisions about records.", result.Output);
        Assert.DoesNotContain("litespec/backend/application#keep-command-handlers-to-load-call-and-stage", result.Output);
        Assert.DoesNotContain("demo/conventions#confirm-large-payouts", result.Output);
    }

    /// <summary>A record identifier in a project that claims intent and behavior returns the provisions about records and no code provisions.</summary>
    [Fact]
    public void Rules_Intent_And_Behavior_Record_Id_Returns_Records_Provisions()
    {
        using var root = CopyFixtureWithClaim("rules-basic", "intent-and-behavior");
        var result = Cli.Run("rules", "--root", root.Path, "use-case/billing.send-invoice");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules for use-case/billing.send-invoice (concern: records, artifact: use-case)", result.Output);
        Assert.Contains("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
        Assert.DoesNotContain("- litespec/backend/", result.Output);
    }

    /// <summary>A code path in a project that claims its own code and proof returns no provisions about LiteSpec's code.</summary>
    [Fact]
    public void Rules_Own_Code_Path_Excludes_LiteSpec_Code_Provisions()
    {
        using var root = CopyFixtureWithClaim("rules-basic", "own-code-and-proof");
        var result = Cli.Run(
            "rules",
            "--root", root.Path,
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("litespec/backend/application#keep-command-handlers-to-load-call-and-stage", result.Output);
        Assert.DoesNotContain("litespec/backend/application#implement-one-outcome-path-per-command", result.Output);
    }

    /// <summary>A record identifier lists the provisions that govern the record.</summary>
    [Fact]
    public void Rules_Record_Id_Lists_Provisions()
    {
        var root = Path.Combine(Cli.Repository, "examples", "claimwick");
        var result = Cli.Run("rules", "--root", root, "use-case/spending.submit-expense");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules for use-case/spending.submit-expense (concern: records, artifact: use-case)", result.Output);
        Assert.Contains("- litespec/core/use-cases#connect-each-use-case-to-intent:", result.Output);
        Assert.Contains("record use-case/spending.submit-expense", result.Output);
    }

    /// <summary>Full output adds the rationale and example under each item.</summary>
    [Fact]
    public void Rules_Full_Prints_Rationale_And_Example()
    {
        var root = Path.Combine(Cli.Repository, "examples", "claimwick");
        var result = Cli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseEndpoint.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rationale: One endpoint class per route operation", result.Output);
        Assert.Contains("Example: At startup, the application discovers endpoint implementations", result.Output);
    }

    /// <summary>A convention whose scope matches the path prints beside the LiteSpec provisions.</summary>
    [Fact]
    public void Rules_Convention_Appears_For_Matching_Path()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("- demo/conventions#confirm-large-payouts: A payout above 1,000 euros asks for one confirmation step before it runs. [path match (apps/api/src/**)]", result.Output);
        Assert.DoesNotContain("show-a-loading-state-on-every-route", result.Output);
    }

    /// <summary>A live departure replaces the provision its covers names; an expired one does not.</summary>
    [Fact]
    public void Rules_Departure_Replaces_Covered_Provision()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "- litespec/backend/application#keep-command-handlers-to-load-call-and-stage: [departure exception/slow-review-departure, expires 2030-01-01] The billing handler calls two aggregates until the split ships.",
            result.Output);
        Assert.Contains("- litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case", result.Output);
        Assert.DoesNotContain("old-layout-departure", result.Output);
    }

    /// <summary>Full output prints the departure consequences instead of the provision example.</summary>
    [Fact]
    public void Rules_Full_Prints_Departure_Consequences()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs",
            "--full");

        Assert.Equal(0, result.Status);
        Assert.Contains("Consequences: The handler loads the invoice and the ledger in one call until the split ships.", result.Output);
    }

    /// <summary>A layer-projects endpoint derives its layer from the project name.</summary>
    [Fact]
    public void Rules_Layered_Endpoint_Uses_Project_Name()
    {
        var result = Cli.Run(
            "rules",
            "--root", Cli.Fixture("rules-layered"),
            "apps/api/src/Demo.WebApi/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains(
            "Rules for apps/api/src/Demo.WebApi/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs (stack: dotnet, concern: web-api, artifact: endpoint)",
            result.Output);
        Assert.Contains("- litespec/backend/api#map-one-operation-per-endpoint:", result.Output);
    }

    /// <summary>Copies one fixture to a temporary root with its layer claim replaced.</summary>
    /// <param name="fixture">The fixture name.</param>
    /// <param name="claim">The claim name, as <see cref="LiteSpec.Tests.Consumer.ConsumerFixture.Claim" /> reads it.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixtureWithClaim(string fixture, string claim)
    {
        var source = Cli.Fixture(fixture);
        var temp = Cli.Temp("rules-level");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        var project = Path.Combine(temp.Path, ".litespec", "project.yaml");
        var declared = DataFile.Read(project)!.AsObject();
        declared["layers"] = LiteSpec.Tests.Consumer.ConsumerFixture.Claim(claim);
        File.WriteAllText(project, declared.ToJsonString());

        return temp;
    }
}
