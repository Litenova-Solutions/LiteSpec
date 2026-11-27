using LiteSpec.Cli.RecordEditing;
using LiteSpec.Cli.Tests.Consumer;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec new entry</c> and the release record template.</summary>
public sealed class NewEntryTests
{
    const string RequirementPage = ".litespec/intent/problems/waiting-for-money/features/approval/approval.yaml";
    const string PermissionPage = ".litespec/behavior/access/permissions.yaml";

    const string FeatureJson = """
        {
          "type": "feature",
          "id": "feature/approval",
          "title": "Approval"
        }
        """;

    const string PermissionJson = """
        {
          "type": "permission-set",
          "title": "Permissions",
          "permissions": [
            {
              "id": "permission/submit-expense",
              "title": "Submit expense",
              "meaning": "Submit an expense and attach receipts.",
              "grantedTo": ["role/owner"]
            }
          ]
        }
        """;

    /// <summary>New entry refuses a requirement and names the command that writes one.</summary>
    /// <proves>litespec/core/intent#identify-every-requirement</proves>
    [Fact(DisplayName = "new entry refuses a requirement and names new requirement")]
    public void RefusesRequirementEntry()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, RequirementPage);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "entry", RequirementPage, "requirement", "decided-within-five-days",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("litespec new requirement <feature>.decided-within-five-days", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <summary>New entry points a rule to the scaffolding command instead of appending it.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry points a rule to new rule")]
    public void EntryPointsToNewRule()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "entry", PermissionPage, "rule", "receipt-filed-once",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("use litespec new rule <home>.receipt-filed-once", result.Output);
    }

    /// <summary>New entry appends a permission without a home.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry appends a permission without a home")]
    public void AppendsPermissionWithoutHome()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, PermissionPage);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "entry", PermissionPage, "permission", "approve-expense",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended permission/approve-expense to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "id: permission/approve-expense");
        Assert.Contains("meaning: This permission allows the action.", inserted);
        Assert.Contains("- role/owner", inserted);
        Assert.DoesNotContain("scope:", inserted);
    }

    /// <summary>New entry refuses a taken permission id and leaves the page alone.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry refuses a taken permission id and leaves the page alone")]
    public void RefusesTakenPermissionId()
    {
        using var consumer = Consumer();
        var page = Path.Combine(consumer.Path, PermissionPage);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "entry", PermissionPage, "permission", "submit-expense",
            "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <summary>New help documents the entry form.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new help documents the entry form")]
    public void HelpDocumentsEntryForm()
    {
        var result = TestCli.Run("new", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("new entry <page-id> <type> <name>", result.Stdout);
    }

    public static TheoryData<string[]> RejectedArgs() => new()
    {
        { ["entry", PermissionPage, "result", "ordered"] },
        { ["entry", PermissionPage, "requirement", "Bad_Name"] },
        { ["entry", PermissionPage, "requirement", "123"] },
        { ["entry", PermissionPage, "rule", "receipt-filed-once"] },
        { ["entry", PermissionPage, "setting", "grace-days"] },
        { ["entry", PermissionPage, "domain-event", "reimbursed"] },
        { ["entry", RequirementPage, "permission", "approve-expense"] },
        { ["entry", ".litespec/intent/problems/waiting-for-money/features/no-such-page.yaml", "permission", "approve-expense"] },
        { ["entry", PermissionPage, "permission"] },
        { ["entry", PermissionPage, "permission", "approve-expense", "--code", "spending.x"] },
        { ["entry", PermissionPage, "permission", "approve-expense", "--outcome", "success"] },
        { ["entry", PermissionPage, "permission", "approve-expense", "--result", "result/x.y"] },
        { ["entry", PermissionPage, "permission", "submit-expense"] },
    };

    /// <summary>Run refuses invalid entries and leaves every page alone.</summary>
    /// <param name="args">One rejected argument set from the theory data.</param>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "run refuses invalid entries and leaves every page alone")]
    [MemberData(nameof(RejectedArgs))]
    public void RunRefusesInvalidEntries(string[] args)
    {
        using var consumer = Consumer();
        var before = AllPages(consumer.Path);

        Assert.Equal(2, Append(consumer, args));
        Assert.Equal(before, AllPages(consumer.Path));
    }

    /// <summary>The filled release record validates.</summary>
    /// <proves>litespec/core/release#record-the-release-artifact</proves>
    [Fact(DisplayName = "the filled release record validates")]
    public void FilledReleaseRecordValidates()
    {
        using var folder = ConsumerFixture.Create();
        var template = File.ReadAllText(Path.Join(TestCli.Repository, "templates/consumer/release-record.md"));
        var filled = template
            .Replace("release/2026-12-01", "release/2026-08-21", StringComparison.Ordinal)
            .Replace("December Release", "First Release", StringComparison.Ordinal)
            .Replace("<source commit>", "9f2c4ab1e0d34f7a8b6c5d2e1f0a3948576c1d2e", StringComparison.Ordinal)
            .Replace("<artifact reference>", "registry.example.com/shop:2026-08-21", StringComparison.Ordinal)
            .Replace(
                "- flow/<name> (<the outcome it proves, or remove this line>)",
                "- None; the release ships no flow.",
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
            .Replace("<the security result with its reference>", "Pass (no open item)", StringComparison.Ordinal)
            .Replace("<the flow result with its reference>", "Pass (no flow included)", StringComparison.Ordinal)
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

        ConsumerFixture.WriteFile(folder.Path, ".litespec/releases/2026-08-21.md", filled);

        var result = TestCli.Run("model", "check", "--root", folder.Path);

        Assert.NotEqual(2, result.Status);
        Assert.DoesNotContain("releases/2026-08-21.md", result.Output);
    }

    /// <summary>A minimal consumer holding one record file per entry type.</summary>
    /// <returns>The temporary consumer folder holding the record files.</returns>
    static TempFolder Consumer()
    {
        var temp = TestCli.Temp("new-entry");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, RequirementPage, FeatureJson);
        TestRecords.Write(temp.Path, ".litespec/intent/problems/waiting-for-money/features/approval/requirements/nobody-approves-own-expense/nobody-approves-own-expense.yaml", """
            {
              "type": "requirement",
              "id": "requirement/approval.nobody-approves-own-expense",
              "title": "Nobody Approves Their Own Expense",
              "kind": "functional",
              "statement": "Nobody approves their own expense.",
              "rationale": "Self approval invites fraud."
            }
            """);
        TestRecords.Write(temp.Path, PermissionPage, PermissionJson);
        return temp;
    }

    /// <summary>Runs the entry path in process against the consumer.</summary>
    /// <param name="consumer">The fixture consumer to append to.</param>
    /// <param name="args">The entry arguments, without the root option the helper adds.</param>
    /// <returns>The entry run exit code.</returns>
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

    /// <summary>Every list record file's text, for the untouched proof.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The text of every list record file in the fixture.</returns>
    static List<string> AllPages(string root) =>
    [
        File.ReadAllText(Path.Combine(root, RequirementPage)),
        File.ReadAllText(Path.Combine(root, PermissionPage)),
    ];

    /// <summary>Proves the page grew by one contiguous insertion holding the new entry.</summary>
    /// <param name="before">The page text before the insertion.</param>
    /// <param name="after">The page text after the insertion.</param>
    /// <param name="marker">The text the inserted block must contain.</param>
    /// <returns>The inserted text.</returns>
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
