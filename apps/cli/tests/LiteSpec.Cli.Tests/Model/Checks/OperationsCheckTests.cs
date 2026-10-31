using System.Text.Json.Nodes;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the operations checks: alerts, drills, operations files and the alert inventory.</summary>
public sealed class OperationsCheckTests
{
    /// <summary>An alert naming its requirement and runbook reports nothing.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_With_Requirement_And_Runbook_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("alert-unlinked", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A Grafana rule named by title reads like a Prometheus rule named by alert.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Title_Named_Rule_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertGrafana);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("alert-unlinked", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An alert without a requirement label reports the rule.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Without_Requirement_Label_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("requirement: requirement/performance.list-loans-latency", "requirement:"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-unlinked", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unlinked, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", unlinked, StringComparison.Ordinal);
        Assert.Contains("carries no requirement label", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a missing requirement reports the id.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_No_Requirement_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("requirement/performance.list-loans-latency", "requirement/performance.no-such-thing"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-unlinked", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unlinked, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.no-such-thing", unlinked, StringComparison.Ordinal);
        Assert.Contains("matches no record", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a functional requirement reports the kind.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_A_Functional_Requirement_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("requirement/performance.list-loans-latency", "requirement/borrowing.max-active-loans"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-unlinked", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("not a quality requirement", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert without a runbook label reports the rule.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Without_Runbook_Label_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("runbook: runbook/slow-listings", "runbook:"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-unlinked", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unlinked, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", unlinked, StringComparison.Ordinal);
        Assert.Contains("carries no runbook label", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a missing runbook reports the id.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_No_Runbook_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("runbook/slow-listings", "runbook/no-such-runbook"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-unlinked", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("runbook/no-such-runbook", unlinked, StringComparison.Ordinal);
        Assert.Contains("matches no record", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert naming a record that is no runbook reports the type.</summary>
    /// <proves>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</proves>
    [Fact]
    public void Alert_Naming_A_Non_Runbook_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("runbook: runbook/slow-listings", "runbook: requirement/borrowing.max-active-loans"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unlinked = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-unlinked", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("not a runbook", unlinked, StringComparison.Ordinal);
    }

    /// <summary>An alert carrying its measure's numbers reports no drift.</summary>
    /// <proves>litespec/quality/operations#keep-each-alert-threshold-to-its-requirements-measure</proves>
    [Fact]
    public void Threshold_Carrying_Measure_Numbers_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("alert-threshold-drift", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An alert carrying none of its measure's numbers reports advisory drift.</summary>
    /// <proves>litespec/quality/operations#keep-each-alert-threshold-to-its-requirements-measure</proves>
    [Fact]
    public void Threshold_Missing_Measure_Numbers_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteWatchedSetup(temp.Path, AlertPassing.Replace("histogram_quantile(0.99, rate(list_loans_seconds_bucket[5m])) > 0.3", "up == 0"));

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var drift = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("alert-threshold-drift", StringComparison.Ordinal));

        Assert.Equal(0, check.Status);
        Assert.Contains("[A]", drift, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", drift, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.list-loans-latency", drift, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement named by an alert reports nothing, even with no passing run.</summary>
    /// <proves>litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_Covered_By_Alert_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementFile, RequirementPage);
        Write(temp.Path, OriginFile, OriginPage);
        Write(temp.Path, RunbookFile, RunbookPage);
        Write(temp.Path, DecisionFile, DecisionPage);
        Write(temp.Path, AlertFile, AlertPassing);
        SetPaths(temp.Path, alerts: [AlertDir], checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("requirement-unmeasured", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("quality-unalerted", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement matched by a check result reports nothing.</summary>
    /// <proves>litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_Covered_By_Check_Result_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementFile, RequirementPage);
        Write(temp.Path, OriginFile, OriginPage);
        Write(temp.Path, DecisionFile, DecisionPage);
        Write(temp.Path, ResultFile, ResultPassing);
        SetPaths(temp.Path, checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("quality-unalerted", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement listed by an open known gap reports as known.</summary>
    /// <proves>litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_Covered_By_Known_Gap_Reports_Known()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementFile, RequirementPage);
        Write(temp.Path, OriginFile, OriginPage);
        Write(temp.Path, DecisionFile, DecisionPage);
        Write(temp.Path, GapFile, GapPage);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("requirement-unmeasured", check.Output, StringComparison.Ordinal);
        Assert.Contains("[known G] [quality-unalerted]", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("[G] [quality-unalerted]", check.Output, StringComparison.Ordinal);
    }

    /// <summary>A quality requirement with no alert or check reports.</summary>
    /// <proves>litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check</proves>
    [Fact]
    public void Quality_With_No_Alert_Or_Check_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementFile, RequirementPage);
        Write(temp.Path, OriginFile, OriginPage);
        Write(temp.Path, DecisionFile, DecisionPage);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unalerted = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("quality-unalerted", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[G]", unalerted, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.list-loans-latency", unalerted, StringComparison.Ordinal);
    }

    /// <summary>A passing check result counts toward its requirement's measure.</summary>
    /// <proves>litespec/quality/operations#record-each-drill-run-as-a-check-result</proves>
    [Fact]
    public void Passing_Check_Result_Counts_Toward_Measure()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementFile, RequirementPage);
        Write(temp.Path, OriginFile, OriginPage);
        Write(temp.Path, DecisionFile, DecisionPage);
        Write(temp.Path, ResultFile, ResultPassing);
        SetPaths(temp.Path, checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unmeasured = check.Output.Split('\n').Where(candidate =>
            candidate.Contains("requirement-unmeasured", StringComparison.Ordinal));

        Assert.Equal(0, check.Status);
        Assert.All(unmeasured, line =>
            Assert.DoesNotContain("requirement/performance.list-loans-latency", line, StringComparison.Ordinal));
    }

    /// <summary>A failing check result leaves its requirement unmeasured but watched.</summary>
    /// <proves>litespec/quality/operations#record-each-drill-run-as-a-check-result</proves>
    [Fact]
    public void Failing_Check_Result_Counts_Nothing()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementFile, RequirementPage);
        Write(temp.Path, OriginFile, OriginPage);
        Write(temp.Path, DecisionFile, DecisionPage);
        Write(temp.Path, ResultFile, ResultPassing.Replace("\"passed\": true", "\"passed\": false"));
        SetPaths(temp.Path, checkResults: CheckResultsDir);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(1, check.Status);
        Assert.Contains("requirement-unmeasured", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("quality-unalerted", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An operations file pointing at its decision reports nothing and follows it.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Operations_File_With_Follows_Passes()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, DecisionFile, LonelyDecisionPage);
        Write(temp.Path, CaddyFile, CaddyPassing);
        SetPaths(temp.Path, operations: [CaddyFile]);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.DoesNotContain("mark-missing", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("mark-unresolved", check.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("decision-unfollowed", check.Output, StringComparison.Ordinal);
    }

    /// <summary>An operations file with no follows mark reports.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Operations_File_Without_Mark_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, DecisionFile, LonelyDecisionPage);
        Write(temp.Path, CaddyFile, CaddyMissing);
        SetPaths(temp.Path, operations: [CaddyFile]);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var missing = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("mark-missing", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", missing, StringComparison.Ordinal);
        Assert.Contains("Caddyfile", missing, StringComparison.Ordinal);
        Assert.Contains("carries no follows mark", missing, StringComparison.Ordinal);
    }

    /// <summary>An operations mark naming no decision reports.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Operations_Mark_Naming_Nothing_Reports()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, DecisionFile, LonelyDecisionPage);
        Write(temp.Path, CaddyFile, CaddyPassing.Replace("decision/0002-loan-index", "decision/0099-no-such-thing"));
        SetPaths(temp.Path, operations: [CaddyFile]);

        var check = Cli.Run("model", "check", "--root", temp.Path);
        var unresolved = check.Output.Split('\n').Single(candidate =>
            candidate.Contains("mark-unresolved", StringComparison.Ordinal));

        Assert.Equal(1, check.Status);
        Assert.Contains("[S]", unresolved, StringComparison.Ordinal);
        Assert.Contains("decision/0099-no-such-thing", unresolved, StringComparison.Ordinal);
        Assert.Contains("matches no decision or deviation", unresolved, StringComparison.Ordinal);
    }

    /// <summary>A technical decision with no follower anywhere is still unfollowed.</summary>
    /// <proves>litespec/quality/operations#point-each-operations-file-at-its-decision</proves>
    [Fact]
    public void Decision_With_No_Follower_Reports_Unfollowed()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, DecisionFile, LonelyDecisionPage);

        var check = Cli.Run("model", "check", "--root", temp.Path);

        Assert.Equal(0, check.Status);
        Assert.Contains("decision-unfollowed", check.Output, StringComparison.Ordinal);
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

        Assert.Equal(0, render.Status);
        Assert.Contains("| Requirement | Alert | Runbook | Severity | File |", limits, StringComparison.Ordinal);
        Assert.Contains("requirement/performance.list-loans-latency", limits, StringComparison.Ordinal);
        Assert.Contains("listing-latency-high", limits, StringComparison.Ordinal);
        Assert.Contains("runbook/slow-listings", limits, StringComparison.Ordinal);
        Assert.Contains("page", limits, StringComparison.Ordinal);
        Assert.Contains("deploy/alerts/lending.yaml", limits, StringComparison.Ordinal);
    }

    static void WriteWatchedSetup(string root, string alert)
    {
        Write(root, RequirementFile, RequirementPage);
        Write(root, OriginFile, OriginPage);
        Write(root, RunbookFile, RunbookPage);
        Write(root, DecisionFile, DecisionPage);
        Write(root, AlertFile, alert);
        Write(root, ResultFile, ResultPassing);
        SetPaths(root, alerts: [AlertDir], checkResults: CheckResultsDir);
    }

    static void SetPaths(string root, string[]? alerts = null, string[]? operations = null, string? checkResults = null)
    {
        var projectFile = Path.Combine(root, "litespec.project.json");
        var project = JsonNode.Parse(File.ReadAllText(projectFile))!.AsObject();
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

    const string RequirementFile = "docs/product/lending/performance.md";
    const string RunbookFile = "docs/behavior/runbooks/slow-listings.md";
    const string OriginFile = "docs/product/decisions/0003-term-start-listing.md";

    const string DecisionFile = "docs/implementation/decisions/0002-loan-index.md";
    const string AlertFile = "deploy/alerts/lending.yaml";
    const string AlertDir = "deploy/alerts";
    const string ResultFile = "docs/proof/checks/2026-11-20-list-loans-load.json";
    const string CheckResultsDir = "docs/proof/checks";
    const string CaddyFile = "deploy/Caddyfile";
    const string LimitsFile = "docs/behavior/operations/limits.md";
    const string GapFile = "docs/known-gaps/list-loans-latency-unwatched.md";

    const string RequirementPage = """
        ---
        {
          "type": "topic",
          "id": "topic/performance",
          "title": "Performance",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-02-01",
          "reviewBy": "2027-04-01",
          "records": [
            {
              "type": "requirement",
              "id": "requirement/performance.list-loans-latency",
              "title": "List loans latency",
              "status": "draft",
              "decider": "lending-team",
              "decideBy": "2027-02-01",
              "kind": "quality",
              "statement": "Listing loans answers within 300 milliseconds at p95.",
              "rationale": "Term start floods the listing.",
              "stimulus": "Two hundred readers list their loans in the first hour of the term.",
              "environment": "Normal operation, the search index warm.",
              "response": "Each listing answers from the index.",
              "measure": "Share of listings answered within 300 milliseconds: at least 99 percent.",
              "dueBy": "2027-01-15",
              "check": "list-loans-load"
            }
          ]
        }
        ---

        # Performance

        Listing loans answers fast at term start.
        """;

    const string RunbookPage = """
        ---
        {
          "type": "runbook",
          "id": "runbook/slow-listings",
          "title": "Slow Listings",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-02-01",
          "trigger": "The listing-latency-high alert fires."
        }
        ---

        # Slow Listings

        ## Impact

        Readers wait on listings past the promised time.

        ## Prerequisites

        Access to the listing dashboard and the search index.

        ## Procedure

        Check the index lag. Warm the index. Escalate to the search owner.

        ## Verification

        Listings answer within 300 milliseconds again.
        """;

    // The product decision that decides the latency requirement.
    const string OriginPage = """
        ---
        {
          "type": "decision",
          "id": "decision/0003-term-start-listing",
          "title": "Term Start Listing",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-02-01",
          "options": [
            {
              "name": "Answer within 300 milliseconds",
              "pros": "Readers list loans without waiting at term start.",
              "cons": "The listing needs an index."
            }
          ],
          "reopenedWhen": "Term start load changes.",
          "decides": [
            "requirement/performance.list-loans-latency"
          ]
        }
        ---

        # Term Start Listing

        ## Context

        Term start floods the loan listing.

        ## Decision

        Listing loans answers within 300 milliseconds at p95.

        ## Consequences

        The listing reads from an index.
        """;

    const string DecisionPage = """
        ---
        {
          "type": "decision",
          "id": "decision/0002-loan-index",
          "title": "Loan Index",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-02-01",
          "options": [
            {
              "name": "Search index",
              "pros": "Listings read from one warm index.",
              "cons": "The index needs warming after deploys."
            }
          ],
          "reopenedWhen": "Listing load changes shape.",
          "basedOn": [
            "requirement/performance.list-loans-latency"
          ]
        }
        ---

        # Loan Index

        ## Context

        Listings read loans one by one today.

        ## Decision

        Listings read from one warm search index.

        ## Consequences

        The index needs warming after deploys.
        """;

    const string LonelyDecisionPage = """
        ---
        {
          "type": "decision",
          "id": "decision/0002-loan-index",
          "title": "Loan Index",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-02-01",
          "options": [
            {
              "name": "Search index",
              "pros": "Listings read from one warm index.",
              "cons": "The index needs warming after deploys."
            }
          ],
          "reopenedWhen": "Listing load changes shape.",
          "basedOn": [
            "requirement/borrowing.max-active-loans"
          ]
        }
        ---

        # Loan Index

        ## Context

        Listings read loans one by one today.

        ## Decision

        Listings read from one warm search index.

        ## Consequences

        The index needs warming after deploys.
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

    const string GapPage = """
        ---
        {
          "type": "known-gap",
          "id": "known-gap/list-loans-latency-unwatched",
          "title": "List loans latency unwatched",
          "owner": "lending-team",
          "status": "draft",
          "decider": "lending-team",
          "decideBy": "2027-02-01",
          "check": "quality-unalerted",
          "subject": [
            "requirement/performance.list-loans-latency"
          ],
          "endsBy": "2027-06-01",
          "reason": "The listing alert ships with the search index."
        }
        ---

        # List loans latency unwatched

        Listing latency has no alert yet.
        """;

    const string LimitsPage = """
        # Limits

        <!-- generated:alert-inventory -->
        <!-- /generated:alert-inventory -->
        """;
}
