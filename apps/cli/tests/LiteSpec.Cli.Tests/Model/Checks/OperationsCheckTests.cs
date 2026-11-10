using System.Text.Json.Nodes;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the operations checks: alerts, drills, operations files and the alert inventory.</summary>
public sealed class OperationsCheckTests
{
    /// <summary>An alert naming its requirement and runbook reports nothing.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_With_Requirement_And_Runbook_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.True(check.Status == 0, check.Output);
        Assert.DoesNotContain("alert-link-missing", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A Grafana rule named by title reads like a Prometheus rule named by alert.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Title_Named_Rule_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertGrafana);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.True(check.Status == 0, check.Output);
        Assert.DoesNotContain("alert-link-missing", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An alert without a requirement label reports the rule.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Without_Requirement_Label_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("requirement: requirement/performance.list-loans-latency", "requirement:"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-link-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unlinked, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", unlinked, StringComparison.Ordinal);
        Assert.Contains("carries no requirement label", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a missing requirement reports the id.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_No_Requirement_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("requirement/performance.list-loans-latency", "requirement/performance.no-such-thing"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-link-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unlinked, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.no-such-thing", unlinked, StringComparison.Ordinal);
        Assert.Contains("matches no record", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a functional requirement reports the kind.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_A_Functional_Requirement_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("requirement/performance.list-loans-latency", "requirement/borrowing.max-active-loans"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-link-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("not a quality requirement", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert without a runbook label reports the rule.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Without_Runbook_Label_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("runbook: runbook/slow-listings", "runbook:"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-link-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unlinked, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", unlinked, StringComparison.Ordinal);
        Assert.Contains("carries no runbook label", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a missing runbook reports the id.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_No_Runbook_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("runbook/slow-listings", "runbook/no-such-runbook"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-link-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("runbook/no-such-runbook", unlinked, StringComparison.Ordinal);
        Assert.Contains("matches no record", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a record that is no runbook reports the type.</summary>
    /// <proves>litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_A_Non_Runbook_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("runbook: runbook/slow-listings", "runbook: requirement/borrowing.max-active-loans"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-link-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("not a runbook", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert carrying its measure's numbers reports no drift.</summary>
    /// <proves>litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure</proves>
    [Fact]
    public void Threshold_Carrying_Measure_Numbers_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.True(check.Status == 0, check.Output);
        Assert.DoesNotContain("alert-threshold-drift", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An alert carrying none of its measure's numbers reports advisory drift.</summary>
    /// <proves>litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure</proves>
    [Fact]
    public void Threshold_Missing_Measure_Numbers_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("histogram_quantile(0.99, rate(list_loans_seconds_bucket[5m])) > 0.3", "up == 0"));

        var check = Cli.Run("model", "check", "--root", temp.Path, "--all");
        var drift = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[alert-threshold-drift]", StringComparison.Ordinal));

        Assert.True(check.Status == 0, check.Output);
        Assert.Contains("[A]", drift, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", drift, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.list-loans-latency", drift, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement named by an alert reports nothing, even with no passing run.</summary>
    /// <proves>litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_Covered_By_Alert_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLatencyRequirement(temp.Path);
        WriteRecord(temp.Path, RunbookFile, RunbookJson);
        WriteRecord(temp.Path, DecisionFile, DecisionJson, DecisionExplanation);
        Write(temp.Path, AlertFile, AlertPassing);
        SetPaths(temp.Path, alerts: [AlertDir], checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("requirement-measure-missing", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("quality-alert-missing", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement matched by a check result reports nothing.</summary>
    /// <proves>litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_Covered_By_Check_Result_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLatencyRequirement(temp.Path);
        WriteRecord(temp.Path, DecisionFile, DecisionJson, DecisionExplanation);
        Write(temp.Path, ResultFile, ResultPassing);
        Write(temp.Path, CaddyFile, CaddyPassing);
        SetPaths(temp.Path, operations: [CaddyFile], checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.True(check.Status == 0, check.Output);
        Assert.DoesNotContain("quality-alert-missing", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement a live exception covers reports as known.</summary>
    /// <proves>litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_Covered_By_Exception_Reports_Known()
    {
        using var temp = ModelFixture.Prepare("small");
        SelectDelivery(temp.Path);
        WriteLatencyRequirement(temp.Path);
        WriteRecord(temp.Path, DecisionFile, DecisionJson, DecisionExplanation);
        WriteRecord(temp.Path, ExceptionFile, ExceptionJson, ExceptionExplanation);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("requirement-measure-missing", check.Output, StringComparison.Ordinal);
        Assert.Contains("[known G] [quality-alert-missing]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[G] [quality-alert-missing]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement with no alert or check reports.</summary>
    /// <proves>litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_With_No_Alert_Or_Check_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        SelectDelivery(temp.Path);
        WriteLatencyRequirement(temp.Path);
        WriteRecord(temp.Path, DecisionFile, DecisionJson, DecisionExplanation);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unalerted = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[quality-alert-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[G]", unalerted, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.list-loans-latency", unalerted, StringComparison.Ordinal);
    }

    /// <summary>A passing check result counts toward its requirement's measure.</summary>
    /// <proves>litespec/ext/delivery#record-each-drill-run-as-a-check-result</proves>
    [Fact]
    public void Passing_Check_Result_Counts_Toward_Measure()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLatencyRequirement(temp.Path);
        WriteRecord(temp.Path, DecisionFile, DecisionJson, DecisionExplanation);
        Write(temp.Path, ResultFile, ResultPassing);
        Write(temp.Path, CaddyFile, CaddyPassing);
        SetPaths(temp.Path, operations: [CaddyFile], checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unmeasured = check.Output.Split('\n').Where(candidate =>
            candidate.Contains("[requirement-measure-missing]", StringComparison.Ordinal));

        Assert.True(check.Status == 0, check.Output);
        Assert.All(unmeasured, line =>
            Assert.DoesNotContain("requirement/performance.list-loans-latency", line, StringComparison.Ordinal));
    }

    /// <summary>A failing check result leaves its requirement unmeasured but watched.</summary>
    /// <proves>litespec/ext/delivery#record-each-drill-run-as-a-check-result</proves>
    [Fact]
    public void Failing_Check_Result_Counts_Nothing()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLatencyRequirement(temp.Path);
        WriteRecord(temp.Path, DecisionFile, DecisionJson, DecisionExplanation);
        Write(temp.Path, ResultFile, ResultPassing.Replace("\"passed\": true", "\"passed\": false"));
        SetPaths(temp.Path, checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("requirement-measure-missing", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("quality-alert-missing", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An operations file pointing at its decision reports nothing and follows it.</summary>
    /// <proves>litespec/ext/delivery#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Operations_File_With_Follows_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRecord(temp.Path, DecisionFile, LonelyDecisionJson, DecisionExplanation);
        Write(temp.Path, CaddyFile, CaddyPassing);
        SetPaths(temp.Path, operations: [CaddyFile]);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.True(check.Status == 0, check.Output);
        Assert.DoesNotContain("mark-missing", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("mark-unknown", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("decision-unimplemented", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An operations file with no follows mark reports.</summary>
    /// <proves>litespec/ext/delivery#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Operations_File_Without_Mark_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRecord(temp.Path, DecisionFile, LonelyDecisionJson, DecisionExplanation);
        Write(temp.Path, CaddyFile, CaddyMissing);
        SetPaths(temp.Path, operations: [CaddyFile]);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var missing = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[mark-missing]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", missing, StringComparison.Ordinal);
        Assert.Contains("Caddyfile", missing, StringComparison.Ordinal);
        Assert.Contains("carries no follows mark", missing, StringComparison.Ordinal);
    }

    /// <summary>An operations mark naming no decision reports.</summary>
    /// <proves>litespec/ext/delivery#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Operations_Mark_Naming_Nothing_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRecord(temp.Path, DecisionFile, LonelyDecisionJson, DecisionExplanation);
        Write(temp.Path, CaddyFile, CaddyPassing.Replace("decision/0002-loan-index", "decision/0099-no-such-thing"));
        SetPaths(temp.Path, operations: [CaddyFile]);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unresolved = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("[mark-unknown]", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unresolved, StringComparison.Ordinal);
        Assert.Contains("decision/0099-no-such-thing", unresolved, StringComparison.Ordinal);
        Assert.Contains("matches no decision or exception", unresolved, StringComparison.Ordinal);
    }

    /// <summary>A technical decision with no follower anywhere is still unfollowed.</summary>
    /// <proves>litespec/ext/delivery#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Decision_With_No_Follower_Reports_Unfollowed()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRecord(temp.Path, DecisionFile, LonelyDecisionJson, DecisionExplanation);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("decision-unimplemented", check.Output, StringComparison.Ordinal);
    }

    /// <summary>Render writes the alert inventory from the alert files.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void Alert_Inventory_Renders_Rules()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing);
        Write(temp.Path, LimitsFile, LimitsPage);

        var render = Cli.Run("model", "render", "--root", temp.Path);
        var limits = File.ReadAllText(Path.Combine(temp.Path, LimitsFile.Replace('/', Path.DirectorySeparatorChar)));

        Assert.True(render.Status == 0, render.Output);
        Assert.Contains("| Requirement | Alert | Runbook | Severity | File |", limits, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.list-loans-latency", limits, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", limits, StringComparison.Ordinal);
        Assert.Contains("runbook/slow-listings", limits, StringComparison.Ordinal);
        Assert.Contains("page", limits, StringComparison.Ordinal);
        Assert.Contains("deploy/alerts/lending.yaml", limits, StringComparison.Ordinal);
    }

    // The latency requirement, the decision that decides it, and the list-loans use case that
    // fulfills it, so the requirement reaches Behavior as every Intent record must.
    static void WriteLatencyRequirement(string root)
    {
        WriteRecord(root, RequirementFile, RequirementJson, RequirementExplanation);
        WriteRecord(root, OriginFile, OriginJson, OriginExplanation);
        var useCase = Path.Combine(root, ListLoansFile);
        var text = File.ReadAllText(useCase).Replace("\r\n", "\n", StringComparison.Ordinal);
        const string Fulfills = "fulfills:\n- requirement/borrowing.max-active-loans\n";
        File.WriteAllText(useCase, text.Replace(Fulfills, Fulfills + "- requirement/performance.list-loans-latency\n", StringComparison.Ordinal));
    }

    static void WriteWatchedSetup(string root, string alert)
    {
        WriteLatencyRequirement(root);
        WriteRecord(root, RunbookFile, RunbookJson);
        WriteRecord(root, DecisionFile, DecisionJson, DecisionExplanation);
        Write(root, AlertFile, alert);
        Write(root, ResultFile, ResultPassing);
        Write(root, CaddyFile, CaddyPassing);
        SelectDelivery(root);
        SetPaths(root, alerts: [AlertDir], operations: [CaddyFile], checkResults: CheckResultsDir);
    }

    /// <summary>Selects the delivery extension, whose alert and runbook codes the layer claim gates.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void SelectDelivery(string root)
    {
        var projectFile = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(projectFile)!.AsObject();
        project["selectedExtensions"] = new JsonArray(new JsonObject { ["id"] = "delivery", ["criterion"] = "The project runs alerts and runbooks in production." });
        File.WriteAllText(projectFile, project.ToJsonString());
    }

    static void SetPaths(string root, string[]? alerts = null, string[]? operations = null, string? checkResults = null)
    {
        var projectFile = Path.Combine(root, ".litespec/project.yaml");
        var project = DataFile.Read(projectFile)!.AsObject();
        var paths = project["paths"]!.AsObject();

        if (alerts is not null)
        {
            paths["alerts"] = new JsonArray(alerts.Select(alert => JsonValue.Create(alert)).ToArray());
        }

        if (operations is not null)
        {
            paths["operations"] = new JsonArray(operations.Select(entry => JsonValue.Create(entry)).ToArray());
        }

        if (checkResults is not null)
        {
            paths["checkResults"] = checkResults;
        }

        File.WriteAllText(projectFile, project.ToJsonString());
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    static void WriteRecord(string root, string relative, string json, string? explanation = null)
    {
        TestRecords.Write(root, relative, json);

        if (explanation is not null)
        {
            TestRecords.Explain(root, Path.ChangeExtension(relative, ".md"), explanation);
        }
    }

    const string RequirementFile = ".litespec/intent/modules/lending/features/performance.yaml";
    const string ListLoansFile = ".litespec/behavior/modules/lending/loans/list-loans.yaml";
    const string RunbookFile = ".litespec/implementation/runbooks/slow-listings.yaml";
    const string OriginFile = ".litespec/intent/decisions/0003-term-start-listing.yaml";

    const string DecisionFile = ".litespec/implementation/decisions/0002-loan-index.yaml";
    const string AlertFile = "deploy/alerts/lending.yaml";
    const string AlertDir = "deploy/alerts";
    const string ResultFile = ".litespec/proof/checks/2026-11-20-list-loans-load.json";
    const string CheckResultsDir = ".litespec/proof/checks";
    const string CaddyFile = "deploy/Caddyfile";
    const string LimitsFile = ".litespec/implementation/operations/limits.md";
    const string ExceptionFile = ".litespec/governance/exceptions/list-loans-latency-unwatched.yaml";

    const string RequirementJson = """
        {
          "type": "feature",
          "id": "feature/performance",
          "title": "Performance",
          "offering": "Loan listings answer within 300 milliseconds at p95 during term start.",
          "solves": ["problem/slow-desk-loans"],
          "records": [
            {
              "type": "requirement",
              "id": "requirement/performance.list-loans-latency",
              "title": "List loans latency",
              "kind": "quality",
              "statement": "Listing loans answers within 300 milliseconds at p95.",
              "rationale": "Term start floods the listing.",
              "stimulus": "Two hundred readers list their loans in the first hour of the term.",
              "environment": "Normal operation, the search index warm.",
              "response": "Each listing answers from the index.",
              "measure": "Share of listings answered within 300 milliseconds: at least 99 percent.",
              "check": "list-loans-load"
            }
          ]
        }
        """;

    const string RequirementExplanation = """
        # Performance

        Term start floods the loan listing with readers listing at once.
        """;

    const string RunbookJson = """
        {
          "type": "runbook",
          "id": "runbook/slow-listings",
          "title": "Slow Listings",
          "trigger": "The listing-latency-high alert fires.",
          "impact": "Readers wait on listings past the promised time.",
          "prerequisites": "Access to the listing dashboard and the search index.",
          "procedure": "Check the index lag. Warm the index. Escalate to the search owner.",
          "verification": "Listings answer within 300 milliseconds again.",
          "recovery": "Roll the index back to its last warm snapshot and rewarm it.",
          "escalation": "Page the search owner when listings stay slow after rewarming."
        }
        """;

    // The product decision that decides the latency requirement.
    const string OriginJson = """
        {
          "type": "decision",
          "id": "decision/0003-term-start-listing",
          "title": "Term Start Listing",
          "options": [
            {
              "name": "Answer within 300 milliseconds",
              "pros": "Readers list loans without waiting at term start.",
              "cons": "The listing needs an index."
            }
          ],
          "choice": "Answer within 300 milliseconds",
          "consequences": "The listing reads from an index.",
          "reopenedWhen": "Term start load changes.",
          "decides": [
            "requirement/performance.list-loans-latency"
          ]
        }
        """;

    const string OriginExplanation = """
        # Term Start Listing

        ## Context

        Term start floods the loan listing.

        ## Options

        Answer within 300 milliseconds, or queue readers behind a waiting page.

        ## Decision

        Listing loans answers within 300 milliseconds at p95.

        ## Evidence

        Term start drill, November 2026.
        """;

    const string DecisionJson = """
        {
          "type": "decision",
          "id": "decision/0002-loan-index",
          "title": "Loan Index",
          "options": [
            {
              "name": "Search index",
              "pros": "Listings read from one warm index.",
              "cons": "The index needs warming after deploys."
            }
          ],
          "choice": "Search index",
          "consequences": "The index needs warming after deploys.",
          "reopenedWhen": "Listing load changes shape.",
          "basedOn": [
            "requirement/performance.list-loans-latency"
          ]
        }
        """;

    const string LonelyDecisionJson = """
        {
          "type": "decision",
          "id": "decision/0002-loan-index",
          "title": "Loan Index",
          "options": [
            {
              "name": "Search index",
              "pros": "Listings read from one warm index.",
              "cons": "The index needs warming after deploys."
            }
          ],
          "choice": "Search index",
          "consequences": "The index needs warming after deploys.",
          "reopenedWhen": "Listing load changes shape.",
          "basedOn": [
            "requirement/borrowing.max-active-loans"
          ]
        }
        """;

    const string DecisionExplanation = """
        # Loan Index

        ## Context

        Listings read loans one by one today.

        ## Options

        One warm search index, or a read replica beside the loans.

        ## Decision

        Listings read from one warm search index.

        ## Evidence

        Term start drill, November 2026.
        """;

    const string AlertPassing = """
        groups:
          - name: lending
            rules:
              - alert: listing-latency-high
                expr: histogram_quantile(0.99, rate(list_loans_seconds_bucket[5m])) > 0.3
                for: 2m
                labels:
                  requirement: requirement/performance.list-loans-latency
                  runbook: runbook/slow-listings
                  severity: page
                annotations:
                  summary: Listings answer too slowly.
        """;

    const string AlertGrafana = """
        apiVersion: 1
        groups:
          - orgId: 1
            name: lending
            folder: Library
            interval: 1m
            rules:
              - title: listing-latency-high
                expr: histogram_quantile(0.99, rate(list_loans_seconds_bucket[5m])) > 0.3
                for: 2m
                labels:
                  requirement: requirement/performance.list-loans-latency
                  runbook: runbook/slow-listings
                  severity: page
        """;

    const string ResultPassing = """
        {
          "check": "list-loans-load",
          "passed": true,
          "value": "99.4 percent answered within 300 milliseconds",
          "ranOn": "2026-11-20"
        }
        """;

    const string CaddyPassing = """
        # <follows>decision/0002-loan-index</follows>
        # Library serves loans behind one proxy.

        loans.library.example {
            reverse_proxy localhost:8080
        }
        """;

    const string CaddyMissing = """
        # Library serves loans behind one proxy.

        loans.library.example {
            reverse_proxy localhost:8080
        }
        """;

    const string ExceptionJson = """
        {
          "type": "exception",
          "id": "exception/list-loans-latency-unwatched",
          "title": "List loans latency unwatched",
          "kind": "gap",
          "covers": [
            "quality-alert-missing"
          ],
          "subject": [
            "requirement/performance.list-loans-latency"
          ],
          "expiresOn": "2027-06-01",
          "reason": "The listing alert ships with the search index.",
          "exit": "The listing-latency-high alert ships and watches the requirement."
        }
        """;

    const string ExceptionExplanation = """
        # List loans latency unwatched

        Listing latency has no alert yet.
        """;

    const string LimitsPage = """
        # Limits

        <!-- generated:alert-inventory -->
        <!-- /generated:alert-inventory -->
        """;
}
