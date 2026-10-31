using LiteSpec;
using LiteSpec.Authoring;
using LiteSpec.Tests.Consumer;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>Tests for <c>litespec new entry</c> and the release record template.</summary>
public sealed class NewEntryTests
{
    const string RequirementPage = "docs/intent/spending/approval.md";
    const string RulePage = "docs/behavior/rules/spending-rules.md";
    const string PermissionPage = "docs/behavior/access/permissions.md";
    const string SettingPage = "docs/behavior/modules/spending/settings.md";

    const string RequirementText = """
        ---
        {
          "type": "topic",
          "id": "topic/approval",
          "title": "Approval",
          "owner": "fixture",
          "status": "draft",
          "decider": "fixture",
          "decideBy": "2026-12-01",
          "reviewBy": "2027-04-01",
          "records": [
            {
              "type": "requirement",
              "id": "requirement/approval.no-self-approval",
              "title": "No self approval",
              "kind": "functional",
              "statement": "Nobody approves their own expense.",
              "rationale": "Self approval invites fraud."
            }
          ]
        }
        ---
        # Approval
        """;

    const string RuleText = """
        ---
        {
          "type": "rule-set",
          "title": "Spending Rules",
          "owner": "fixture",
          "status": "draft",
          "decider": "fixture",
          "decideBy": "2026-12-01",
          "records": [
            {
              "type": "rule",
              "id": "rule/spending.receipt-filed-once",
              "title": "Receipt filed once",
              "kind": "cross-aggregate",
              "statement": "A receipt file supports one expense across spending."
            }
          ]
        }
        ---
        # Spending Rules
        """;

    const string PermissionText = """
        ---
        {
          "type": "permission-list",
          "title": "Permissions",
          "owner": "fixture",
          "status": "draft",
          "decider": "fixture",
          "decideBy": "2026-12-01",
          "records": [
            {
              "type": "permission",
              "id": "permission/submit-expense",
              "title": "Submit expense",
              "meaning": "Submit an expense and attach receipts.",
              "scope": "self"
            }
          ]
        }
        ---
        # Permissions
        """;

    const string SettingText = """
        ---
        {
          "type": "setting-set",
          "title": "Settings",
          "owner": "fixture",
          "status": "draft",
          "decider": "fixture",
          "decideBy": "2026-12-01",
          "records": [
            {
              "type": "setting",
              "id": "setting/spending.receipt-threshold",
              "title": "Receipt Threshold",
              "valueType": "money",
              "setBy": "decider"
            }
          ]
        }
        ---
        # Settings
        """;

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry appends a requirement last and reserves its identifier")]
    public void AppendsRequirementLast()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, RequirementPage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", RequirementPage, "requirement", "decided-within-five-days",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended requirement/approval.decided-within-five-days to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "\"id\": \"requirement/approval.decided-within-five-days\"");
        Assert.Contains("\"kind\": \"functional\"", inserted);
        Assert.Contains("\"statement\": \"<The commitment in one sentence>\"", inserted);
        Assert.Contains("\"rationale\": \"<Why this commitment holds, in one sentence>\"", inserted);

        var existing = after.IndexOf("requirement/approval.no-self-approval", StringComparison.Ordinal);
        var added = after.IndexOf("requirement/approval.decided-within-five-days", StringComparison.Ordinal);
        Assert.True(0 <= existing && existing < added);
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry appends a rule under the page's existing home")]
    public void AppendsRuleUnderExistingHome()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, RulePage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", RulePage, "rule", "receipt-kept-seven-years",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended rule/spending.receipt-kept-seven-years to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "\"id\": \"rule/spending.receipt-kept-seven-years\"");
        Assert.Contains("\"kind\": \"invariant\"", inserted);
        Assert.Contains("\"statement\": \"<The rule in one sentence>\"", inserted);
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry appends a permission without a home")]
    public void AppendsPermissionWithoutHome()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, PermissionPage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", PermissionPage, "permission", "approve-expense",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended permission/approve-expense to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "\"id\": \"permission/approve-expense\"");
        Assert.Contains("\"meaning\": \"<What this permission allows in one sentence>\"", inserted);
        Assert.Contains("\"scope\": \"<scope, such as platform, tenant or self>\"", inserted);
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry appends a setting under its module")]
    public void AppendsSettingUnderModule()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, SettingPage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", SettingPage, "setting", "approval-reminder-delay",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended setting/spending.approval-reminder-delay to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "\"id\": \"setting/spending.approval-reminder-delay\"");
        Assert.Contains("\"valueType\": \"integer\"", inserted);
        Assert.Contains("\"setBy\": \"decider\"", inserted);
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a taken requirement id and leaves the page alone")]
    public void RefusesTakenRequirementId()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, RequirementPage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", RequirementPage, "requirement", "no-self-approval",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a taken rule id and leaves the page alone")]
    public void RefusesTakenRuleId()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, RulePage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", RulePage, "rule", "receipt-filed-once",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a taken permission id and leaves the page alone")]
    public void RefusesTakenPermissionId()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, PermissionPage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", PermissionPage, "permission", "submit-expense",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a taken setting id and leaves the page alone")]
    public void RefusesTakenSettingId()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, SettingPage);
        var before = File.ReadAllText(page);

        var result = Cli.Run(
            "new", "entry", SettingPage, "setting", "receipt-threshold",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry takes the requirement home from an empty page's file stem")]
    public void DerivesRequirementHomeFromFileStem()
    {
        using var consumer = Consumer();
        const string relative = "docs/intent/spending/fees.md";
        WritePage(consumer.Path, relative, "topic", "Fees");

        var result = Cli.Run(
            "new", "entry", relative, "requirement", "late-fee-cap",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended requirement/fees.late-fee-cap to ", result.Stdout);
        Assert.Contains(
            "\"id\": \"requirement/fees.late-fee-cap\"",
            File.ReadAllText(Path.Combine(consumer.Path, relative)));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry takes the setting home from the parent modules folder")]
    public void DerivesSettingModuleFromPath()
    {
        using var consumer = Consumer();
        const string relative = "docs/behavior/modules/billing/settings.md";
        WritePage(consumer.Path, relative, "setting-set", "Settings");

        var result = Cli.Run(
            "new", "entry", relative, "setting", "grace-days",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended setting/billing.grace-days to ", result.Stdout);
        Assert.Contains(
            "\"id\": \"setting/billing.grace-days\"",
            File.ReadAllText(Path.Combine(consumer.Path, relative)));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a setting page outside a modules folder with no settings")]
    public void RefusesSettingWithoutAModule()
    {
        using var consumer = Consumer();
        const string relative = "docs/behavior/settings.md";
        WritePage(consumer.Path, relative, "setting-set", "Settings");
        var before = File.ReadAllText(Path.Combine(consumer.Path, relative));

        var result = Cli.Run(
            "new", "entry", relative, "setting", "grace-days",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("outside a modules folder", result.Output);
        Assert.Equal(before, File.ReadAllText(Path.Combine(consumer.Path, relative)));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a type the page does not own")]
    public void RefusesTypeThePageDoesNotOwn()
    {
        using var consumer = Consumer();
        var before = File.ReadAllText(Path.Combine(consumer.Path, RequirementPage));

        var result = Cli.Run(
            "new", "entry", RequirementPage, "rule", "receipt-filed-once",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("belongs on a rule-set page", result.Output);
        Assert.Equal(before, File.ReadAllText(Path.Combine(consumer.Path, RequirementPage)));
    }

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new help documents the entry form")]
    public void HelpDocumentsEntryForm()
    {
        var result = Cli.Run("new", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("new entry <page-id> <type> <name>", result.Stdout);
    }

    public static TheoryData<string[]> RejectedArgs() => new()
    {
        { ["entry", RequirementPage, "result", "ordered"] },
        { ["entry", RequirementPage, "requirement", "Bad_Name"] },
        { ["entry", RequirementPage, "requirement", "123"] },
        { ["entry", RequirementPage, "rule", "receipt-filed-once"] },
        { ["entry", RulePage, "requirement", "decided-within-five-days"] },
        { ["entry", SettingPage, "permission", "approve-expense"] },
        { ["entry", PermissionPage, "setting", "grace-days"] },
        { ["entry", "docs/intent/spending/no-such-page.md", "requirement", "decided-within-five-days"] },
        { ["entry", RequirementPage, "requirement"] },
        { ["entry", RequirementPage, "requirement", "decided-within-five-days", "--code", "spending.x"] },
        { ["entry", RequirementPage, "requirement", "decided-within-five-days", "--outcome", "success"] },
        { ["entry", RequirementPage, "requirement", "decided-within-five-days", "--result", "result/x.y"] },
        { ["entry", RequirementPage, "requirement", "no-self-approval"] },
        { ["entry", PermissionPage, "permission", "submit-expense"] },
    };

    /// <proves>litespec/core/authoring#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "run refuses invalid entries and leaves every page alone")]
    [MemberData(nameof(RejectedArgs))]
    public void RunRefusesInvalidEntries(string[] args)
    {
        using var consumer = Consumer();
        var before = AllPages(consumer.Path);

        Assert.Equal(2, Append(consumer, args));
        Assert.Equal(before, AllPages(consumer.Path));
    }

    /// <proves>litespec/core/release#record-the-release-artifact</proves>
    [Fact(DisplayName = "the filled release record validates")]
    public void FilledReleaseRecordValidates()
    {
        using var folder = ConsumerFixture.Create();
        var template = File.ReadAllText(Path.Join(Cli.Repository, "templates/consumer/release-record.md"));
        var filled = template
            .Replace("<Release display title>", "First Release", StringComparison.Ordinal)
            .Replace("<Release Display Title>", "First Release", StringComparison.Ordinal)
            .Replace("<owner>", "fixture", StringComparison.Ordinal)
            .Replace("<YYYY-MM-DD>", "2026-08-21", StringComparison.Ordinal)
            .Replace("<source commit>", "9f2c4ab1e0d34f7a8b6c5d2e1f0a3948576c1d2e", StringComparison.Ordinal)
            .Replace("<artifact reference>", "registry.example.com/shop:2026-08-21", StringComparison.Ordinal)
            .Replace(
                "- journey/<name> (<the outcome it proves, or remove this line>)",
                "- None; the release ships no journey.",
                StringComparison.Ordinal)
            .Replace(
                "- use-case/<module>.<name> (<the behavior it ships, or remove this line>)",
                "- use-case/orders.cancel-order (A customer cancels an order claim before shipment.)",
                StringComparison.Ordinal)
            .Replace(
                "- <extension id, or remove this line when no extension applies>",
                "- None selected.",
                StringComparison.Ordinal)
            .Replace("<the build result with its reference>", "Pass (fixture build 1)", StringComparison.Ordinal)
            .Replace("<the test result with its reference>", "Pass (fixture suites, 41 passing)", StringComparison.Ordinal)
            .Replace("<the contract result with its reference>", "Pass (no contract change)", StringComparison.Ordinal)
            .Replace("<the security result with its reference>", "Pass (no queue item)", StringComparison.Ordinal)
            .Replace("<the journey result with its reference>", "Pass (no journey included)", StringComparison.Ordinal)
            .Replace("<the schema result with its reference>", "Pass (no migration)", StringComparison.Ordinal)
            .Replace("<the backup result with its reference>", "Pass (nightly backup restored on 2026-08-20)", StringComparison.Ordinal)
            .Replace("<the deployment result with its reference>", "Pass (staging deploy 118)", StringComparison.Ordinal)
            .Replace("<the readiness result with its reference>", "Pass (probes answer on staging)", StringComparison.Ordinal)
            .Replace("<the diagnostics result with its reference>", "Pass (trace sample reviewed)", StringComparison.Ordinal)
            .Replace("<the smoke-test result with its reference>", "Pass (cancel order smoke run)", StringComparison.Ordinal)
            .Replace("<the rollback result with its reference>", "Pass (previous image kept)", StringComparison.Ordinal)
            .Replace("<the operating limits, or none>", "100 orders a minute", StringComparison.Ordinal)
            .Replace("<the alerts watching this release, or none>", "order failures page the owner", StringComparison.Ordinal)
            .Replace("<runbook/<name>, or none>", "None", StringComparison.Ordinal)
            .Replace("<the known operating conditions, or none>", "None", StringComparison.Ordinal)
            .Replace("<the skipped checks with their reasons, or none>", "None", StringComparison.Ordinal);
        Assert.DoesNotContain("<", filled);

        ConsumerFixture.WriteFile(folder.Path, "docs/releases/2026-08-21.md", filled);

        var result = Cli.Run("consumer", "--root", folder.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("PASS: project configuration, page frontmatter and glossary are valid.", result.Stdout);
    }

    /// <summary>A minimal consumer holding one page per entry type.</summary>
    static TempFolder Consumer()
    {
        var temp = Cli.Temp("new-entry");
        File.WriteAllText(Path.Combine(temp.Path, "litespec.project.json"), "{}");
        WriteText(temp.Path, RequirementPage, RequirementText);
        WriteText(temp.Path, RulePage, RuleText);
        WriteText(temp.Path, PermissionPage, PermissionText);
        WriteText(temp.Path, SettingPage, SettingText);
        return temp;
    }

    /// <summary>Writes one list page with an empty records array.</summary>
    static void WritePage(string root, string relative, string type, string title)
    {
        WriteText(root, relative, $$"""
            ---
            {
              "type": "{{type}}",
              "title": "{{title}}",
              "owner": "fixture",
              "status": "draft",
              "decider": "fixture",
              "decideBy": "2026-12-01",
              "records": []
            }
            ---
            # {{title}}
            """);
    }

    /// <summary>Writes one consumer-relative file, creating its directory.</summary>
    static void WriteText(string root, string relative, string contents)
    {
        var file = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, contents);
    }

    /// <summary>Runs the entry path in process against the consumer.</summary>
    static int Append(TempFolder consumer, params string[] args)
    {
        var parsed = CliArgs.Parse(
            [.. args, "--root", consumer.Path],
            [],
            ["--root", "--code", "--outcome", "--result"],
            maxPositionals: 4);
        Assert.Null(parsed.Error);
        return AppendListEntry.Run(parsed);
    }

    /// <summary>Every list page's text, for the untouched proof.</summary>
    static List<string> AllPages(string root) =>
    [
        File.ReadAllText(Path.Combine(root, RequirementPage)),
        File.ReadAllText(Path.Combine(root, RulePage)),
        File.ReadAllText(Path.Combine(root, PermissionPage)),
        File.ReadAllText(Path.Combine(root, SettingPage)),
    ];

    /// <summary>Proves the page grew by one contiguous insertion holding the new entry.</summary>
    static string AssertSingleInsertion(string before, string after, string marker)
    {
        // Lines, not characters: a new entry shares its opening lines with its neighbour,
        // and a character scan would cut the insertion mid-line.
        var old = before.Split('\n');
        var grown = after.Split('\n');
        var prefix = 0;

        while (prefix < old.Length && prefix < grown.Length && old[prefix] == grown[prefix])
        {
            prefix++;
        }

        var suffix = 0;

        while (suffix < old.Length - prefix && suffix < grown.Length - prefix
            && old[old.Length - 1 - suffix] == grown[grown.Length - 1 - suffix])
        {
            suffix++;
        }

        Assert.Equal(old.Length, prefix + suffix);

        var inserted = string.Join('\n', grown[prefix..(grown.Length - suffix)]);
        Assert.Contains(marker, inserted);

        return inserted;
    }
}
