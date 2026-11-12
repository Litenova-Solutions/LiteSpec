using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model;

/// <summary>Proves <c>model render</c> writes the decision index and the departure index into their folders.</summary>
public sealed class DecisionIndexTests
{
    /// <summary>Decision Index Groups By Kind Then Concern.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void DecisionIndexGroupsByKindThenConcern()
    {
        using var temp = TestCli.Temp("model");
        WriteProject(temp.Path);
        var config = TestModel.Config(temp.Path);
        var readme = WriteReadme(temp.Path, ".litespec/implementation/decisions/README.md", "# Technical Decisions", "decision-index");
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0001-store-orders-in-domain", DecisionFile(config, "0001-store-orders-in-domain"), DecisionJson("Store Orders in Domain", "package", "Documents", "Queries pass one hundred a day.", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("decision", "decision/0002-serve-loans-behind-one-proxy", DecisionFile(config, "0002-serve-loans-behind-one-proxy"), DecisionJson("Serve Loans Behind One Proxy", "package", "One proxy", "Traffic outgrows one proxy.", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("decision", "decision/0003-render-loans-as-pdf", DecisionFile(config, "0003-render-loans-as-pdf"), DecisionJson("Render Loans as PDF", "topology", "PDF library", "Loans stop needing files.", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(temp.Path, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));
        model.Code.Elements.Add(FollowsOnly("BillingCache", ModuleFile(temp.Path, "Billing", "Holds", "BillingCache.cs"), "decision/0001-store-orders-in-domain"));
        model.Code.Elements.Add(FollowsOnly("LoanStore", ModuleFile(temp.Path, "Booking", "Domain", "LoanStore.cs"), "decision/0002-serve-loans-behind-one-proxy"));
        model.Code.Elements.Add(FollowsOnly("LoanRenderer", ModuleFile(temp.Path, "Booking", "Holds", "LoanRenderer.cs"), "decision/0003-render-loans-as-pdf"));

        var changed = GeneratedRegions.Render(model);
        var rendered = File.ReadAllText(readme);

        Assert.Contains(".litespec/implementation/decisions/README.md", changed);
        Assert.Contains("| Decision | Kind | Concern | Choice | Followers | Reopened When | Flags |", rendered);
        Assert.DoesNotContain("stale", rendered);
        var first = rendered.IndexOf("0001 Store Orders in Domain", StringComparison.Ordinal);
        var second = rendered.IndexOf("0002 Serve Loans Behind One Proxy", StringComparison.Ordinal);
        var third = rendered.IndexOf("0003 Render Loans as PDF", StringComparison.Ordinal);
        Assert.True(first >= 0 && second >= 0 && third >= 0, rendered);
        Assert.True(first < second && second < third, rendered);
    }

    /// <summary>Decision Index Row Holds Choice Followers Reopened And Modules.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void DecisionIndexRowHoldsChoiceFollowersReopenedAndModules()
    {
        using var temp = TestCli.Temp("model");
        WriteProject(temp.Path);
        var config = TestModel.Config(temp.Path);
        var readme = WriteReadme(temp.Path, ".litespec/implementation/decisions/README.md", "# Technical Decisions", "decision-index");
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0005-render-receipts-as-pdf", DecisionFile(config, "0005-render-receipts-as-pdf"), DecisionJson("Render Receipts as PDF", "package", "PDF library", "Receipts stop needing files.", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(temp.Path, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));
        model.Code.Elements.Add(FollowsOnly("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "decision/0005-render-receipts-as-pdf"));
        model.Code.Elements.Add(FollowsOnly("InvoiceCache", ModuleFile(temp.Path, "Billing", "Invoices", "InvoiceCache.cs"), "decision/0005-render-receipts-as-pdf"));

        GeneratedRegions.Render(model);
        var line = File.ReadAllLines(readme).Single(candidate => candidate.Contains("0005 Render Receipts as PDF", StringComparison.Ordinal));

        Assert.Contains("[0005 Render Receipts as PDF](0005-render-receipts-as-pdf.yaml)", line);
        Assert.Contains("| package |", line);
        Assert.Contains("| application |", line);
        Assert.Contains("| PDF library |", line);
        Assert.Contains("| 2 (Billing, Booking) |", line);
        Assert.Contains("| Receipts stop needing files. |", line);
    }

    /// <summary>Decision Index Marks Unimplemented Basis Missing And Scope Invalid.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void DecisionIndexMarksUnimplementedBasisMissingAndScopeInvalid()
    {
        using var temp = TestCli.Temp("model");
        WriteProject(temp.Path);
        var config = TestModel.Config(temp.Path);
        var readme = WriteReadme(temp.Path, ".litespec/implementation/decisions/README.md", "# Technical Decisions", "decision-index");
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0006-loan-index", DecisionFile(config, "0006-loan-index"), DecisionJson("Loan Index", "format", "B-tree", "Reads pass ten thousand a day.", null)),
            TestModel.Record("decision", "decision/0007-hold-cache", DecisionFile(config, "0007-hold-cache"), DecisionJson("Hold Cache", "boundary", "Memory cache", "Holds outgrow memory.", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command"}"""),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(temp.Path, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));
        model.Code.Elements.Add(ImplementsAndFollows("HoldRoomCache", ModuleFile(temp.Path, "Booking", "Holds", "HoldRoomCache.cs"), "use-case/booking.hold-room", "decision/0007-hold-cache"));

        GeneratedRegions.Render(model);
        var lines = File.ReadAllLines(readme);
        var lonely = lines.Single(candidate => candidate.Contains("0006 Loan Index", StringComparison.Ordinal));
        var local = lines.Single(candidate => candidate.Contains("0007 Hold Cache", StringComparison.Ordinal));

        Assert.Contains("`decision-unimplemented`", lonely);
        Assert.Contains("`decision-basis-missing`", lonely);
        Assert.Contains("`decision-scope-invalid`", local);
        Assert.DoesNotContain("decision-unimplemented", local);
    }

    /// <summary>Departure Index Groups Gaps Before Permanent Ordered By Expires.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void DepartureIndexGroupsGapsBeforePermanentOrderedByExpires()
    {
        using var temp = TestCli.Temp("model");
        WriteProject(temp.Path);
        var config = TestModel.Config(temp.Path);
        var readme = WriteReadme(temp.Path, ".litespec/governance/departures/README.md", "# Departures", "departure-index");
        var model = TestModel.Model(
            config,
            TestModel.Record("departure", "departure/polish-unfinished", DepartureFile(config, "polish-unfinished"), DepartureJson("Polish Unfinished", ["use-case-unimplemented"], "2027-06-01")),
            TestModel.Record("departure", "departure/screens-unwatched", DepartureFile(config, "screens-unwatched"), DepartureJson("Screens Unwatched", ["quality-alert-missing"], "2026-01-01")),
            TestModel.Record("departure", "departure/weekend-export", DepartureFile(config, "weekend-export"), DepartureJson("Weekend Export", ["litespec/quality/security#authorize-each-target-resource"], null)));

        GeneratedRegions.Render(model);
        var rendered = File.ReadAllText(readme);

        Assert.Contains("### Gaps", rendered);
        Assert.Contains("### Permanent", rendered);
        Assert.True(rendered.IndexOf("### Gaps", StringComparison.Ordinal) < rendered.IndexOf("### Permanent", StringComparison.Ordinal), rendered);
        Assert.True(rendered.IndexOf("Screens Unwatched", StringComparison.Ordinal) < rendered.IndexOf("Polish Unfinished", StringComparison.Ordinal), rendered);
        Assert.Contains("[Screens Unwatched](screens-unwatched.yaml)", rendered);
        Assert.Contains("| 2026-01-01 | `quality-alert-missing` |", rendered);
        Assert.Contains("`litespec/quality/security#authorize-each-target-resource`", rendered);
    }

    /// <summary>Decision Index Lists Only Its Own Folder.</summary>
    /// <proves>litespec/core/model#render-generated-regions-from-the-model</proves>
    [Fact]
    public void DecisionIndexListsOnlyItsOwnFolder()
    {
        using var temp = TestCli.Temp("model");
        WriteProject(temp.Path);
        var config = TestModel.Config(temp.Path);
        var readme = WriteReadme(temp.Path, ".litespec/implementation/decisions/README.md", "# Technical Decisions", "decision-index");
        var model = TestModel.Model(
            config,
            TestModel.Record("decision", "decision/0008-store-loans-as-documents", DecisionFile(config, "0008-store-loans-as-documents"), DecisionJson("Store Loans as Documents", "package", "Documents", "Queries pass one hundred a day.", "constraint/receipts-stay-in-the-eu")),
            TestModel.Record("decision", "decision/0001-receipt-threshold", Path.Combine(config.Root, ".litespec", "intent", "decisions", "0001-receipt-threshold.yaml"), DecisionJson("Receipt Threshold", null, "Always", "Paper costs double.", null)),
            TestModel.Record("constraint", "constraint/receipts-stay-in-the-eu", Path.Combine(temp.Path, "retention.yaml"), """{"authority": "Tax code, article 9.", "kind": "technical", "applies": true, "appliesBecause": "The shop stores receipts."}"""));

        GeneratedRegions.Render(model);
        var rendered = File.ReadAllText(readme);

        Assert.Contains("0008 Store Loans as Documents", rendered);
        Assert.DoesNotContain("Receipt Threshold", rendered);
    }

    /// <summary>Writes the minimal project file naming the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    static void WriteProject(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            "{\"owners\": [\"lending-team\"], \"marks\": \"xml\", \"stackConfig\": {\"dotnet\": {\"modules\": \"apps/api/src/Library.Modules\"}}}");
    }

    /// <summary>Writes a README carrying one empty generated region.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="relative">The README path under the root, with forward slashes.</param>
    /// <param name="title">The README title line.</param>
    /// <param name="region">The generated region name.</param>
    /// <returns>The README full path.</returns>
    static string WriteReadme(string root, string relative, string title, string region)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"{title}\n\n<!-- generated:{region} -->\nstale\n<!-- /generated:{region} -->\n");

        return path;
    }

    /// <summary>One technical element carrying only a follows mark on the decision.</summary>
    /// <param name="class">The class name carrying the mark.</param>
    /// <param name="file">The file the element sits in.</param>
    /// <param name="follows">The decision id the follows mark names.</param>
    /// <returns>The marked code element.</returns>
    static MarkedElement FollowsOnly(string @class, string file, string follows) =>
        new(
            CodeKind.Technical, @class, null, file, 3,
            [new CodeMark(follows, MarkStyle.Xml, MarkVerb.Follows, file, 1)],
            InTests: false);

    /// <summary>One technical element carrying an implements mark and a follows mark on the decision.</summary>
    /// <param name="class">The class name carrying the marks.</param>
    /// <param name="file">The file the element sits in.</param>
    /// <param name="implements">The record id the implements mark names.</param>
    /// <param name="follows">The decision id the follows mark names.</param>
    /// <returns>The marked code element.</returns>
    static MarkedElement ImplementsAndFollows(string @class, string file, string implements, string follows) =>
        new(
            CodeKind.Technical, @class, null, file, 3,
            [
                new CodeMark(implements, MarkStyle.Xml, MarkVerb.Implements, file, 1),
                new CodeMark(follows, MarkStyle.Xml, MarkVerb.Follows, file, 2),
            ],
            InTests: false);

    /// <summary>One file under the modules root.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="segments">The path segments under the modules root.</param>
    /// <returns>The full path to the file.</returns>
    static string ModuleFile(string root, params string[] segments)
    {
        var parts = new List<string> { root, "apps", "api", "src", "Library.Modules" };
        parts.AddRange(segments);

        return Path.Combine([.. parts]);
    }

    /// <summary>A decision naming its title, kind, choice, reopening condition and basis.</summary>
    /// <param name="title">The decision title.</param>
    /// <param name="kind">The decision kind, or null to leave the field out.</param>
    /// <param name="choice">The selected option.</param>
    /// <param name="reopenedWhen">What would overturn the choice.</param>
    /// <param name="basedOn">The target id placed in the decision basedOn list, or null to leave the field out.</param>
    /// <returns>The decision fields as JSON.</returns>
    static string DecisionJson(string title, string? kind, string choice, string reopenedWhen, string? basedOn) =>
        "{\"title\": \"" + title + "\""
        + ", \"options\": [{\"name\": \"Documents\", \"pros\": \"Reads stay simple.\", \"cons\": \"Queries need an index.\"}]"
        + ", \"choice\": \"" + choice + "\""
        + ", \"consequences\": \"The document store owns order persistence.\""
        + ", \"reopenedWhen\": \"" + reopenedWhen + "\""
        + (kind is null ? "" : ", \"kind\": \"" + kind + "\"")
        + (basedOn is null ? "" : ", \"basedOn\": [\"" + basedOn + "\"]")
        + "}";

    /// <summary>A departure naming its title, covers and expiry.</summary>
    /// <param name="title">The departure title.</param>
    /// <param name="covers">The codes, provisions or pages the departure covers.</param>
    /// <param name="expiresOn">The expiry date, or null for a lasting departure.</param>
    /// <returns>The departure fields as JSON.</returns>
    static string DepartureJson(string title, string[] covers, string? expiresOn) =>
        "{\"title\": \"" + title + "\""
        + ", \"covers\": [" + string.Join(", ", covers.Select(cover => "\"" + cover + "\"")) + "]"
        + ", \"subject\": [\"use-case/spending.export-expenses\"]"
        + ", \"reason\": \"Export waits for the finance file format.\""
        + (expiresOn is null ? "" : ", \"exit\": \"The gap closes when the format lands.\", \"expiresOn\": \"" + expiresOn + "\"")
        + "}";

    /// <summary>A decision file under the product implementation decisions folder.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <param name="name">The decision file name without its folder.</param>
    /// <returns>The full path to the decision file.</returns>
    static string DecisionFile(ConsumerConfig config, string name) =>
        Path.Combine(config.Root, ".litespec", "implementation", "decisions", $"{name}.yaml");

    /// <summary>A departure file under the product departures folder.</summary>
    /// <param name="config">The consumer config carrying the root folder.</param>
    /// <param name="name">The departure file name without its folder.</param>
    /// <returns>The full path to the departure file.</returns>
    static string DepartureFile(ConsumerConfig config, string name) =>
        Path.Combine(config.Root, ".litespec", "governance", "departures", $"{name}.yaml");
}
