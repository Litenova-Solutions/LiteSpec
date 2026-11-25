using System.Text.RegularExpressions;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the access checks: a role scope needs its glossary term, a permission name opens with a table verb or
///     manage, and use-case access states a duration window, a self scope with its verified caller input, and
///     preconditions that are cited precondition rules.
/// </summary>
public sealed class AccessCheckTests
{
    /// <summary>A role with a scope that has its glossary term passes.</summary>
    /// <proves>litespec/core/access#define-each-scope-level-in-the-glossary</proves>
    [Fact(DisplayName = "A role scope with its glossary term passes")]
    public void ScopeWithItsTermPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Role(temp.Path, "finance", "platform"),
            TestModel.Record("term", "term/platform-scope", Path.Combine(temp.Path, ".litespec", "glossary.yaml"), "{\"title\": \"platform scope\", \"term\": \"platform scope\"}"));

        Assert.DoesNotContain("scope-term-missing", TestModel.Codes(model));
    }

    /// <summary>A role scope with no glossary term titled with the level and Scope is reported with the title it needs.</summary>
    /// <proves>litespec/core/access#define-each-scope-level-in-the-glossary</proves>
    [Fact(DisplayName = "A role scope with no glossary term is reported with its title")]
    public void ScopeWithoutTermIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Role(temp.Path, "finance", "platform"),
            TestModel.Record("term", "term/self-scope", Path.Combine(temp.Path, ".litespec", "glossary.yaml"), "{\"title\": \"self scope\", \"term\": \"self scope\"}"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "scope-term-missing");
        Assert.Equal("Role 'role/finance' names scope 'platform', and no glossary term is titled 'Platform Scope'.", finding.Message);
    }

    /// <summary>A multi-word scope level takes each kebab word capitalized in its title.</summary>
    /// <proves>litespec/core/access#define-each-scope-level-in-the-glossary</proves>
    [Fact(DisplayName = "A multi-word scope level takes its title from each word")]
    public void MultiWordScopeTakesEachWordInItsTitle()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Role(temp.Path, "desk", "lending-desk"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "scope-term-missing");
        Assert.Contains("'Lending Desk Scope'", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A permission opening with a verb of the table, or with manage, passes.</summary>
    /// <param name="id">The permission identifier under test.</param>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Theory(DisplayName = "A permission opening with a table verb or manage passes")]
    [InlineData("permission/approve-expense")]
    [InlineData("permission/list-expenses")]
    [InlineData("permission/manage-policy")]
    public void VerbFromTheTablePasses(string id)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Permission(temp.Path, id));

        Assert.DoesNotContain("permission-verb-unknown", TestModel.Codes(model));
    }

    /// <summary>A permission opening with no verb of the table is reported with the verb it opens with.</summary>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Fact(DisplayName = "A permission opening with no table verb is reported")]
    public void VerbOutsideTheTableIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Permission(temp.Path, "permission/handle-documents"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "permission-verb-unknown");
        Assert.Equal("Permission 'permission/handle-documents' opens with 'handle', which is no verb of the use-case verb table.", finding.Message);
    }

    /// <summary>The baseline verbs the code reads are the verbs the use-cases page's verb table states.</summary>
    /// <proves>litespec/core/access#name-each-permission-by-a-use-case-verb</proves>
    [Fact(DisplayName = "The baseline verbs match the use-cases page verb table")]
    public void BaselineVerbsMatchTheVerbTable()
    {
        var page = File.ReadAllText(Path.Combine(TestCli.Repository, "docs", "core", "use-cases.md"));
        var table = Regex.Match(page, @"### Use-Case Verbs\s*\n(?<body>.*?)\n\nThe baseline vocabulary retires", RegexOptions.Singleline).Groups["body"].Value;
        var verbs = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match row in Regex.Matches(table, @"^\| (?<cells>[^|]+)\|", RegexOptions.Multiline))
        {
            foreach (Match verb in Regex.Matches(row.Groups["cells"].Value, "`(?<verb>[a-z-]+)`"))
            {
                verbs.Add(verb.Groups["verb"].Value);
            }
        }

        Assert.NotEmpty(verbs);
        Assert.Equal(verbs.OrderBy(verb => verb, StringComparer.Ordinal), UseCaseVerbs.Baseline.OrderBy(verb => verb, StringComparer.Ordinal));
    }

    /// <summary>A system grant reached only through modules passes.</summary>
    /// <proves>litespec/core/access#authorize-every-use-case-through-a-declared-grant</proves>
    [Fact(DisplayName = "A system grant reached only through modules passes")]
    public void SystemGrantReachedOnlyThroughModulesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"), """{"kind": "query", "steps": [{"statement": "Spending reads the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "system", "tenant": "none"}}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("grants system", StringComparison.Ordinal));
    }

    /// <summary>A system grant reached through an entry point is reported.</summary>
    /// <proves>litespec/core/access#authorize-every-use-case-through-a-declared-grant</proves>
    [Fact(DisplayName = "A system grant reached through an entry point is reported")]
    public void SystemGrantReachedThroughEntryPointIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"), """{"kind": "query", "steps": [{"statement": "Spending reads the expense."}], "reachedThrough": ["entry-point/web"], "access": {"grant": "system", "tenant": "none"}}"""));

        Assert.Contains(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("grants system", StringComparison.Ordinal));
    }

    /// <summary>A use case stating its sign-in window as a duration passes.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "A use case with a duration sign-in window passes")]
    public void DurationWindowPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "steps": [{"statement": "Spending reimburses the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "recentSignIn": "PT15M"}}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Message.Contains("recentSignIn", StringComparison.Ordinal));
    }

    /// <summary>A use case naming a duration setting as its sign-in window passes.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "A use case with a duration setting window passes")]
    public void DurationSettingWindowPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "steps": [{"statement": "Spending reimburses the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "recentSignIn": "setting/spending.sign-in-window"}}"""),
            TestModel.Record("setting", "setting/spending.sign-in-window", Path.Combine(temp.Path, "window.yaml"), """{"valueType": "duration"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Message.Contains("recentSignIn", StringComparison.Ordinal));
    }

    /// <summary>A sign-in window naming no record is reported by the citation check.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "A sign-in window naming no record is reported")]
    public void UnknownWindowSettingIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "steps": [{"statement": "Spending reimburses the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "recentSignIn": "setting/spending.sign-in-window"}}"""),
            TestModel.Record("module", "module/reporting", Path.Combine(temp.Path, "reporting.yaml"), """{"dependsOn": []}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "reference-unknown" && found.Message.Contains("setting/spending.sign-in-window", StringComparison.Ordinal));
        Assert.Equal("Record 'use-case/spending.reimburse-expense' cites 'setting/spending.sign-in-window', which no record holds.", finding.Message);
    }

    /// <summary>A sign-in window naming a record of another type is reported.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "A sign-in window naming no setting is reported")]
    public void WindowNamingNoSettingIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "steps": [{"statement": "Spending reimburses the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "recentSignIn": "setting/spending.sign-in-window"}}"""),
            TestModel.Record("actor", "setting/spending.sign-in-window", Path.Combine(temp.Path, "window.yaml"), """{"kind": "person"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("recentSignIn", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.reimburse-expense' names recentSignIn 'setting/spending.sign-in-window', which is no setting.", finding.Message);
    }

    /// <summary>A sign-in window naming a setting of another value type is reported.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "A sign-in window naming no duration setting is reported")]
    public void WindowNamingNoDurationSettingIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", Path.Combine(temp.Path, "reimburse.yaml"), """{"kind": "command", "steps": [{"statement": "Spending reimburses the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "recentSignIn": "setting/spending.sign-in-window"}}"""),
            TestModel.Record("setting", "setting/spending.sign-in-window", Path.Combine(temp.Path, "window.yaml"), """{"valueType": "integer"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("recentSignIn", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.reimburse-expense' names recentSignIn 'setting/spending.sign-in-window', which is no duration setting.", finding.Message);
    }

    /// <summary>A self scope naming a verified-claim input identifying an actor passes.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "A self scope with its verified caller input passes")]
    public void SelfScopeWithVerifiedClaimPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "steps": [{"statement": "Spending lists the expenses."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "scope": "self", "identityInput": "employee"}, "input": [{"name": "employee", "meaning": "The caller.", "type": "id", "identifies": "actor/employee", "origin": "verified-claim", "verifiedBy": "entry-point/web", "classification": "personal"}]}"""),
            TestModel.Record("actor", "actor/employee", Path.Combine(temp.Path, "actor.yaml"), """{"kind": "person"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Message.Contains("identity input", StringComparison.Ordinal));
    }

    /// <summary>A self scope naming no input is reported.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "A self scope naming no input is reported")]
    public void SelfScopeNamingNoInputIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "steps": [{"statement": "Spending lists the expenses."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "scope": "self", "identityInput": "nobody"}}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("identity input", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.list-expenses' names identity input 'nobody', which is no input of the use case.", finding.Message);
    }

    /// <summary>A self scope naming an input of another origin is reported.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "A self scope naming a caller input is reported")]
    public void SelfScopeWithCallerOriginIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "steps": [{"statement": "Spending lists the expenses."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "scope": "self", "identityInput": "employee"}, "input": [{"name": "employee", "meaning": "The caller.", "type": "id", "identifies": "actor/employee", "origin": "caller", "required": true, "classification": "personal"}]}"""),
            TestModel.Record("actor", "actor/employee", Path.Combine(temp.Path, "actor.yaml"), """{"kind": "person"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("identity input", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.list-expenses' names identity input 'employee', which takes origin 'caller', not verified-claim.", finding.Message);
    }

    /// <summary>A self scope naming an input that identifies nothing is reported.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "A self scope naming an input that identifies nothing is reported")]
    public void SelfScopeWithoutIdentifiesIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "steps": [{"statement": "Spending lists the expenses."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "scope": "self", "identityInput": "employee"}, "input": [{"name": "employee", "meaning": "The caller.", "type": "text", "origin": "verified-claim", "verifiedBy": "entry-point/web", "required": true, "classification": "personal"}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("identity input", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.list-expenses' names identity input 'employee', which identifies no actor.", finding.Message);
    }

    /// <summary>A self scope naming an input that identifies no actor is reported.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "A self scope naming an input that identifies no actor is reported")]
    public void SelfScopeIdentifyingNoActorIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.list-expenses", Path.Combine(temp.Path, "list.yaml"), """{"kind": "query", "steps": [{"statement": "Spending lists the expenses."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "scope": "self", "identityInput": "expense"}, "input": [{"name": "expense", "meaning": "The expense.", "type": "id", "identifies": "aggregate/spending.expense", "origin": "verified-claim", "verifiedBy": "entry-point/web", "required": true, "classification": "internal"}]}"""),
            TestModel.Record("aggregate", "aggregate/spending.expense", Path.Combine(temp.Path, "expense.yaml"), """{"summary": "An expense."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("identity input", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.list-expenses' names identity input 'expense', which identifies 'aggregate/spending.expense', not an actor.", finding.Message);
    }

    /// <summary>A precondition naming a cited precondition rule passes.</summary>
    /// <proves>litespec/core/access#name-every-authorization-precondition</proves>
    [Fact(DisplayName = "A precondition naming a cited precondition rule passes")]
    public void CitedPreconditionPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "steps": [{"statement": "Spending approves the expense.", "cites": ["rule/expense.approver-is-not-submitter"]}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "preconditions": ["rule/expense.approver-is-not-submitter"]}}"""),
            TestModel.Record("rule", "rule/expense.approver-is-not-submitter", Path.Combine(temp.Path, "rule.yaml"), """{"kind": "precondition"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Message.Contains("precondition", StringComparison.Ordinal));
    }

    /// <summary>A precondition naming a rule its input rules cite passes.</summary>
    /// <proves>litespec/core/access#name-every-authorization-precondition</proves>
    [Fact(DisplayName = "A precondition cited through input rules passes")]
    public void PreconditionCitedThroughInputRulesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "steps": [{"statement": "Spending approves the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "preconditions": ["rule/expense.positive-amount"]}, "input": [{"name": "amount", "meaning": "The amount.", "type": "money", "rules": ["rule/expense.positive-amount"], "origin": "caller", "classification": "internal"}]}"""),
            TestModel.Record("rule", "rule/expense.positive-amount", Path.Combine(temp.Path, "rule.yaml"), """{"kind": "precondition"}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Message.Contains("precondition", StringComparison.Ordinal));
    }

    /// <summary>A precondition naming no rule is reported by the citation check.</summary>
    /// <proves>litespec/core/access#name-every-authorization-precondition</proves>
    [Fact(DisplayName = "A precondition naming no rule is reported")]
    public void UnknownPreconditionIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "steps": [{"statement": "Spending approves the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "preconditions": ["rule/expense.missing"]}}"""),
            TestModel.Record("module", "module/reporting", Path.Combine(temp.Path, "reporting.yaml"), """{"dependsOn": []}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "reference-unknown" && found.Message.Contains("rule/expense.missing", StringComparison.Ordinal));
        Assert.Equal("Record 'use-case/spending.approve-expense' cites 'rule/expense.missing', which no record holds.", finding.Message);
    }

    /// <summary>A precondition naming a rule of another kind is reported.</summary>
    /// <proves>litespec/core/access#name-every-authorization-precondition</proves>
    [Fact(DisplayName = "A precondition naming another rule kind is reported")]
    public void WrongKindPreconditionIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "steps": [{"statement": "Spending approves the expense.", "cites": ["rule/expense.reimbursed-once"]}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "preconditions": ["rule/expense.reimbursed-once"]}}"""),
            TestModel.Record("rule", "rule/expense.reimbursed-once", Path.Combine(temp.Path, "rule.yaml"), """{"kind": "invariant"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("precondition", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.approve-expense' names precondition 'rule/expense.reimbursed-once', which is a rule of kind 'invariant', not a precondition.", finding.Message);
    }

    /// <summary>A precondition naming a rule the use case cites nowhere is reported.</summary>
    /// <proves>litespec/core/access#name-every-authorization-precondition</proves>
    [Fact(DisplayName = "A precondition the use case cites nowhere is reported")]
    public void UncitedPreconditionIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.approve-expense", Path.Combine(temp.Path, "approve.yaml"), """{"kind": "command", "steps": [{"statement": "Spending approves the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "signed-in", "tenant": "own", "preconditions": ["rule/expense.approver-is-not-submitter"]}}"""),
            TestModel.Record("rule", "rule/expense.approver-is-not-submitter", Path.Combine(temp.Path, "rule.yaml"), """{"kind": "precondition"}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "record-invalid" && found.Message.Contains("precondition", StringComparison.Ordinal));
        Assert.Equal("Use case 'use-case/spending.approve-expense' names precondition 'rule/expense.approver-is-not-submitter', which the use case cites nowhere in its steps, inputs, or results.", finding.Message);
    }

    /// <summary>Builds a role record with its scope.</summary>
    /// <param name="root">The consumer root the role file sits under.</param>
    /// <param name="name">The role name, which names its folder and file.</param>
    /// <param name="scope">The scope level the role names.</param>
    /// <returns>The role record.</returns>
    private static Record Role(string root, string name, string scope) =>
        TestModel.Record(
            "role",
            "role/" + name,
            Path.Combine(root, ".litespec", "intent", "roles", name, name + ".yaml"),
            "{\"scope\": \"" + scope + "\", \"summary\": \"A role.\"}");

    /// <summary>Builds a permission record with its meaning.</summary>
    /// <param name="root">The consumer root the permission file sits under.</param>
    /// <param name="id">The permission identifier.</param>
    /// <returns>The permission record.</returns>
    private static Record Permission(string root, string id) =>
        TestModel.Record(
            "permission",
            id,
            Path.Combine(root, ".litespec", "behavior", "permissions.yaml"),
            "{\"meaning\": \"The right to act.\"}");
}
