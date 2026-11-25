using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the declarations generator: one catalog source per module from the use case records, with skipped messages listed.</summary>
public sealed class GenerateDeclarationsTests
{
    /// <summary>Declarations render the golden catalog source for the fixture's lending use cases.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void DeclarationsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.EndsWith("LendingDeclarations.generated.cs", file.AbsolutePath, StringComparison.Ordinal);
        GenerateGoldens.AssertEqual("declarations-dotnet.txt", file.Content);
        Assert.Equal(
            new List<string>
            {
                "use-case/lending.merge-holds",
                "use-case/lending.waive-fine",
            },
            plan.Skips.Select(skip => skip.UseCaseId).ToList());
    }

    /// <summary>Resolved declarations render the golden catalog source for the resolution fixture's lending use cases.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void ResolutionDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("declarations-resolution");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.EndsWith("LendingDeclarations.generated.cs", file.AbsolutePath, StringComparison.Ordinal);
        GenerateGoldens.AssertEqual("declarations-resolution-dotnet.txt", file.Content);
        Assert.Equal(
            new List<string>
            {
                "use-case/lending.cancel-hold",
                "use-case/lending.merge-holds",
            },
            plan.Skips.Select(skip => skip.UseCaseId).ToList());
    }

    /// <summary>A message declared outside its use case folder renders with its real namespace, not the conventional one.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MessageInAnotherNamespaceRendersRealUsing()
    {
        var content = ResolutionCatalog();

        Assert.Contains("using Library.Lending.Contracts.Loans.ReadLoan;", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Library.Lending.Application.Loans.ReadLoan", content, StringComparison.Ordinal);
    }

    /// <summary>A key template and an audit target render through the message's real member when it differs from the input name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void InputMapsToRealMember()
    {
        var content = ResolutionCatalog();

        Assert.Contains("(command => FormattableString.Invariant($\"{command.Copy}-for-{command.ReaderId}\"))", content, StringComparison.Ordinal);
        Assert.Contains("(command => command.Copy.ToString())", content, StringComparison.Ordinal);
        Assert.DoesNotContain("command.CopyId", content, StringComparison.Ordinal);
    }

    /// <summary>A date in a key template renders round-trip inside the culture-invariant expression.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DateKeyRendersCultureInvariant()
    {
        var content = ResolutionCatalog();

        Assert.Contains("(command => FormattableString.Invariant($\"{command.LoanId}-{command.ExtendedTo:O}\"))", content, StringComparison.Ordinal);
        Assert.Contains("using System;", content, StringComparison.Ordinal);
    }

    /// <summary>A message declared twice resolves beside its handler instead of skipping as ambiguous.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AmbiguousMessageResolvesBesideItsHandler()
    {
        var content = ResolutionCatalog();

        Assert.Contains("using Library.Lending.Application.Loans.RenewLoan;", content, StringComparison.Ordinal);
        Assert.Contains(".Describe<RenewLoanCommand>", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Library.Lending.Contracts.Loans.RenewLoan", content, StringComparison.Ordinal);
    }

    /// <summary>A message declared by a hand file and a generated file resolves from the generated one instead of skipping.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GeneratedMessageElsewhereWinsOverHandFile()
    {
        using var temp = ModelFixture.Prepare("declarations-resolution");
        var folder = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending.Contracts", "Holds", "MergeHolds");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "MergeHoldsCommand.generated.cs"),
            """
            namespace Library.Lending.Contracts.Holds.MergeHolds;

            public sealed record MergeHoldsCommand(Guid HoldId, Guid BranchId);
            """);
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.merge-holds");
        Assert.Contains("using Library.Lending.Contracts.Holds.MergeHolds;", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Describe<MergeHoldsCommand>", file.Content, StringComparison.Ordinal);
        Assert.Contains("{command.HoldId}-{command.BranchId}", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case whose message or member cannot be resolved skips with the reason instead of rendering code that cannot compile.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnresolvableMembersSkipWithReasons()
    {
        using var temp = ModelFixture.Prepare("declarations-resolution");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Equal(
            new List<(string, string)>
            {
                ("use-case/lending.cancel-hold", "message 'CancelHoldCommand' resolves to no single declaring file in the backend source"),
                ("use-case/lending.merge-holds", "key template names 'branchId', which matches no member of message 'MergeHoldsCommand'"),
            },
            plan.Skips.Select(skip => (skip.UseCaseId, skip.Reason)).ToList());
    }

    /// <summary>Generate lists each skipped use case with its reason and still writes the catalog.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateListsSkippedMessagesWithReasons()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("LendingDeclarations.generated.cs", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("skipped use-case/lending.merge-holds: authorization condition 'rule/hold.single-active-hold' names a guard check the policy vocabulary cannot state.", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("use-case/lending.transfer-hold", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("use-case/lending.list-branches", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("skipped use-case/lending.waive-fine: audit level 'none' states no exemption rationale.", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("1 files rewritten.", result.Stdout, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingDeclarations.generated.cs")));
    }

    /// <summary>A declarations target for a TypeScript stack is invalid and generate exits 1 naming it.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DeclarationsTargetNeedsDotnetStack()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: svelte
              path: apps/web/src/generated
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var error = Assert.Single(plan.Errors);

        Assert.Contains("needs a .NET stack, not 'svelte'", error, StringComparison.Ordinal);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("needs a .NET stack", result.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A policy form without its placeholders is invalid, naming the field and the missing placeholder.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PolicyFormWithoutPlaceholdersIsInvalid()
    {
        Assert.Contains(
            "holds neither {permission} nor {name}",
            PlanError("  permissionPolicy: constant"),
            StringComparison.Ordinal);
        Assert.Contains(
            "holds no {policy}",
            PlanError("  selfPolicy: Self"),
            StringComparison.Ordinal);
        Assert.Contains(
            "holds no {window}",
            PlanError("  recentSignInPolicy: RecentSignIn({policy})"),
            StringComparison.Ordinal);
        Assert.Contains(
            "holds no {policy}",
            PlanError("  runsAsPolicy: SystemOr"),
            StringComparison.Ordinal);
        Assert.Contains(
            "holds no {policy}",
            PlanError("  crossPolicy: Cross"),
            StringComparison.Ordinal);
        Assert.Contains(
            "holds no {policy}",
            PlanError("  publicPolicy: Public"),
            StringComparison.Ordinal);
        Assert.Contains(
            "holds no {policy}",
            PlanError("  nonePolicy: WithoutTenant"),
            StringComparison.Ordinal);
    }

    /// <summary>Custom policy forms render through every grant and wrap of the fixture.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CustomPolicyFormsRender()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              permissionPolicy: "{name}-policy"
              signedInPolicy: in
              anonymousPolicy: anon
              systemPolicy: sys
              selfPolicy: Own({policy})
              recentSignInPolicy: Fresh({policy},{window})
              runsAsPolicy: Sys({policy})
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains(".Authorized(\"Fresh(borrow-copy-policy,PT4H)\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"Sys(place-hold-policy)\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"Own(borrow-copy-policy)\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"Own(read-loans-policy)\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"Sys(Fresh(Own(borrow-copy-policy),PT30M))\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"in\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"anon\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"sys\")", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case reaching across tenants renders its grant's policy instead of skipping.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CrossTenantRendersGrantPolicy()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains(".Describe<TransferHoldCommand>(declarations => declarations", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"permission/place-hold\")", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.transfer-hold");
    }

    /// <summary>A use case touching no tenant data renders its grant's policy instead of skipping.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NoneTenantRendersGrantPolicy()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains(".Describe<ListBranchesQuery>(declarations => declarations", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"signed-in\")", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.list-branches");
    }

    /// <summary>A use case beyond tenant boundaries renders its grant's policy instead of skipping.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PublicTenantRendersGrantPolicy()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        RewriteTenant(temp.Path, "behavior/modules/lending/use-cases/list-branches/list-branches.yaml", "none", "public");

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains(".Describe<ListBranchesQuery>(declarations => declarations", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"signed-in\")", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.list-branches");
    }

    /// <summary>Tenant forms wrap the grant's policy first, before the self, recent-sign-in, and system-run wraps.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantPolicyFormsWrapTheGrantPolicy()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              crossPolicy: Cross({policy})
              nonePolicy: WithoutTenant({policy})
            """);
        RewriteTenant(temp.Path, "behavior/modules/lending/aggregates/loans/use-cases/extend-loan/extend-loan.yaml", "own", "cross");

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains(".Authorized(\"Cross(permission/place-hold)\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"WithoutTenant(signed-in)\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".Authorized(\"RecentSignIn(Cross(permission/borrow-copy),PT4H)\")", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case stating an unknown tenant scope skips with the reason instead of rendering.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnknownTenantSkipsWithReason()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        RewriteTenant(temp.Path, "behavior/modules/lending/aggregates/holds/use-cases/cancel-hold/cancel-hold.yaml", "own", "bogus");

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.DoesNotContain(".Describe<CancelHoldCommand>", file.Content, StringComparison.Ordinal);
        Assert.Contains(
            plan.Skips,
            skip => skip.UseCaseId == "use-case/lending.cancel-hold"
                && skip.Reason == "tenant 'bogus' is no own, cross, public or none tenant scope");
    }

    /// <summary>A key template renders as a culture-invariant interpolated expression over the message's properties, with literals escaped.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void KeyTemplateRendersAsAnInterpolatedExpression()
    {
        var call = DeclarationsRenderer.KeyedIdempotencyCall(
            "SubmitExpenseCommand",
            "command",
            [
                new DeclarationsRenderer.KeyPart("a\"b\\{c}", null),
                new DeclarationsRenderer.KeyPart(null, "ExpenseId"),
            ],
            "account",
            false);

        Assert.Equal(
            @".Declare(IdempotencyDeclaration.KeyedBy<SubmitExpenseCommand>(command => FormattableString.Invariant($""a\""b\\{{c}}{command.ExpenseId}"")) with { Scope = ""account"", ReplayResult = false })",
            call);
    }

    /// <summary>A date key part renders with its round-trip format inside the culture-invariant expression.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DateKeyPartRendersItsFormat()
    {
        var call = DeclarationsRenderer.KeyedIdempotencyCall(
            "ExtendLoanCommand",
            "command",
            [
                new DeclarationsRenderer.KeyPart(null, "LoanId"),
                new DeclarationsRenderer.KeyPart("-", null),
                new DeclarationsRenderer.KeyPart(null, "ExtendedTo", "O"),
            ],
            "tenant",
            false);

        Assert.Equal(
            @".Declare(IdempotencyDeclaration.KeyedBy<ExtendLoanCommand>(command => FormattableString.Invariant($""{command.LoanId}-{command.ExtendedTo:O}"")) with { Scope = ""tenant"", ReplayResult = false })",
            call);
    }

    /// <summary>A dotted key part renders through the member path inside the culture-invariant expression.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DottedKeyPartRendersThroughTheMemberPath()
    {
        var call = DeclarationsRenderer.KeyedIdempotencyCall(
            "ExtendLoanCommand",
            "command",
            [
                new DeclarationsRenderer.KeyPart(null, "LoanId.Branch"),
                new DeclarationsRenderer.KeyPart("-", null),
                new DeclarationsRenderer.KeyPart(null, "ExtendedTo", "O"),
            ],
            "tenant",
            false);

        Assert.Equal(
            @".Declare(IdempotencyDeclaration.KeyedBy<ExtendLoanCommand>(command => FormattableString.Invariant($""{command.LoanId.Branch}-{command.ExtendedTo:O}"")) with { Scope = ""tenant"", ReplayResult = false })",
            call);
    }

    /// <summary>A dotted key placeholder resolves its head to the message member and renders through the member path.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DottedKeyPlaceholderResolvesThroughTheMessageMember()
    {
        using var temp = ModelFixture.Prepare("declarations-resolution");
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "extend-loan", "extend-loan.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "key: \"{loanId}-{extendedTo}\"",
                "key: \"{loanId.branch}-{extendedTo}\"",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var content = Assert.Single(plan.Files).Content;

        Assert.DoesNotContain("use-case/lending.extend-loan", plan.Skips.Select(skip => skip.UseCaseId));
        Assert.Contains("(command => FormattableString.Invariant($\"{command.LoanId.Branch}-{command.ExtendedTo:O}\"))", content, StringComparison.Ordinal);
    }

    /// <summary>A key template without placeholders renders as a plain literal.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ConstantKeyRendersAsALiteral()
    {
        var call = DeclarationsRenderer.KeyedIdempotencyCall(
            "SubmitExpenseCommand",
            "command",
            [new DeclarationsRenderer.KeyPart("fixed", null)],
            "tenant",
            true);

        Assert.Contains("(command => \"fixed\")", call, StringComparison.Ordinal);
        Assert.Contains("ReplayResult = true", call, StringComparison.Ordinal);
    }

    /// <summary>An audit target on an identifier converts through ToString, and on text reads as is.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AuditTargetSelectorFollowsThePropertyType()
    {
        Assert.Equal(
            @".Declare<AuditDeclaration>(AuditDeclaration.Audited(""use-case/lending.borrow-copy"", category: ""personal-data-read"", targetKind: ""aggregate/lending.copy"").WithTargetFrom<BorrowCopyCommand>(command => command.CopyId.ToString()))",
            DeclarationsRenderer.AuditedWithTargetCall(
                "use-case/lending.borrow-copy", "personal-data-read", "aggregate/lending.copy",
                "BorrowCopyCommand", "command", "CopyId", targetIsText: false));

        Assert.Equal(
            @".Declare<AuditDeclaration>(AuditDeclaration.Audited(""use-case/lending.tag-copy"").WithTargetFrom<TagCopyCommand>(command => command.Tag))",
            DeclarationsRenderer.AuditedWithTargetCall(
                "use-case/lending.tag-copy", null, null,
                "TagCopyCommand", "command", "Tag", targetIsText: true));
    }

    /// <summary>A composite audit target joins its message properties with a slash in listed order.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CompositeAuditTargetJoinsItsProperties()
    {
        Assert.Equal(
            @".Declare<AuditDeclaration>(AuditDeclaration.Audited(""use-case/lending.borrow-copy"", category: ""personal-data-read"", targetKind: ""aggregate/lending.loan"").WithTargetFrom<BorrowCopyCommand>(command => $""{command.CopyId.ToString()}/{command.ReaderId.ToString()}""))",
            DeclarationsRenderer.AuditedWithCompositeTargetCall(
                "use-case/lending.borrow-copy", "personal-data-read", "aggregate/lending.loan",
                "BorrowCopyCommand", "command", [("CopyId", false), ("ReaderId", false)]));

        Assert.Equal(
            @".Declare<AuditDeclaration>(AuditDeclaration.Audited(""use-case/lending.tag-copy"").WithTargetFrom<TagCopyCommand>(command => $""{command.Tag}/{command.CopyId.ToString()}""))",
            DeclarationsRenderer.AuditedWithCompositeTargetCall(
                "use-case/lending.tag-copy", null, null,
                "TagCopyCommand", "command", [("Tag", true), ("CopyId", false)]));
    }

    /// <summary>A composite audit target renders its golden catalog source with the joined selector.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void CompositeAuditTargetMatchesGolden()
    {
        var text = DeclarationsRenderer.RenderDotnet(
            "lending",
            [
                new DeclarationsRenderer.DeclaredMessage(
                    "use-case/lending.borrow-copy", "BorrowCopyCommand", "Library.Lending.Application.Loans.BorrowCopy", "command",
                    DeclarationsRenderer.AuthorizedCall("permission/borrow-copy"),
                    DeclarationsRenderer.AuditedWithCompositeTargetCall(
                        "use-case/lending.borrow-copy", "personal-data-read", "aggregate/lending.loan",
                        "BorrowCopyCommand", "command", [("CopyId", false), ("ReaderId", false)]),
                    null),
            ],
            "Library.Lending");

        GenerateGoldens.AssertEqual("declarations-audit-composite.txt", text);
    }

    /// <summary>A target the handler determines renders its golden catalog source without a selector.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void FromOutputAuditTargetMatchesGolden()
    {
        var text = DeclarationsRenderer.RenderDotnet(
            "spending",
            [
                new DeclarationsRenderer.DeclaredMessage(
                    "use-case/spending.submit-expense", "SubmitExpenseCommand", "Claimwick.Spending.Application.Expenses.SubmitExpense", "command",
                    DeclarationsRenderer.AuthorizedCall("permission/request-expense"),
                    DeclarationsRenderer.AuditedCall("use-case/spending.submit-expense", "monetary-action", "aggregate/spending.expense"),
                    null),
            ],
            "Claimwick.Spending");

        GenerateGoldens.AssertEqual("declarations-audit-from-output.txt", text);
    }

    /// <summary>A message with one declaration renders on one line, and a catalog without audit imports no auditing namespace.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleCallMessageRendersOnOneLine()
    {
        var text = DeclarationsRenderer.RenderDotnet(
            "lending",
            [
                new DeclarationsRenderer.DeclaredMessage(
                    "use-case/lending.list-loans", "ListLoansQuery", "Library.Lending.Application.ListLoans", "query",
                    DeclarationsRenderer.AuthorizedCall("signed-in"), null, null),
            ],
            "Library.Lending");

        Assert.Contains("internal sealed partial class LendingDeclarations : IMultiMessageDefinition", text, StringComparison.Ordinal);
        Assert.Contains(".Describe<ListLoansQuery>(declarations => declarations.Authorized(\"signed-in\"));", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Auditing", text, StringComparison.Ordinal);
    }

    /// <summary>A composite target in the record renders the joined selector through the plan.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CompositeTargetInRecordRendersJoinedSelector()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        var borrow = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(borrow, File.ReadAllText(borrow).Replace(
            "  target: copyId\n  targetKind: aggregate/lending.copy",
            "  target: [copyId, readerId]\n  targetKind: aggregate/lending.loan",
            StringComparison.Ordinal));

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains("targetKind: \"aggregate/lending.loan\"", file.Content, StringComparison.Ordinal);
        Assert.Contains("$\"{command.CopyId.ToString()}/{command.ReaderId.ToString()}\"", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A target read from output renders no selector through the plan, leaving the target to the handler.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void FromOutputTargetInRecordRendersWithoutSelector()
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);
        var borrow = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        var yaml = File.ReadAllText(borrow).Replace(
            "  target: copyId\n  targetKind: aggregate/lending.copy",
            "  target:\n    from: output\n    field: loanId\n  targetKind: aggregate/lending.loan",
            StringComparison.Ordinal);
        yaml += "\noutput:\n- name: loanId\n  meaning: The created loan.\n  type: id\n  identifies: aggregate/lending.loan\n";
        File.WriteAllText(borrow, yaml);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains(".Audited(\"use-case/lending.borrow-copy\", category: \"personal-data-read\", targetKind: \"aggregate/lending.loan\")", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("WithTargetFrom<BorrowCopyCommand>", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A module whose every use case skips writes no file, and generate still lists each skip.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AllSkippedModuleWritesNoFile()
    {
        using var temp = ModelFixture.Prepare("declarations");

        foreach (var folder in new[]
            {
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "return-copy"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "extend-loan"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "renew-loan"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "list-loans"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "list-branches"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "holds", "use-cases", "place-hold"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "holds", "use-cases", "transfer-hold"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "holds", "use-cases", "cancel-hold"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "register-copy"),
                Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy"),
            })
        {
            Directory.Delete(folder, recursive: true);
        }

        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Equal(2, plan.Skips.Count);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("0 files rewritten.", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("skipped use-case/lending.waive-fine: ", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A message without its own definition files reports nothing when a declarations target covers the project.</summary>
    /// <proves>litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void MissingDefinitionsWithDeclarationsTargetReportNothing()
    {
        using var temp = TestCli.Temp("generate");
        var folder = ApplicationFolder(temp.Path);
        var model = CatalogModel(
            temp.Path,
            folder,
            """{"access": {"grant": "permission/borrow-copy", "tenant": "own"}, "audit": {"exemptBecause": "Reads trail nothing."}, "idempotency": {"mode": "none", "reason": "Each call lends its own copy."}}""");

        Assert.Empty(RunDefinitionChecks(model));
    }

    /// <summary>A definition file that exists beside a declarations target is still judged against its record.</summary>
    /// <proves>litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void DriftingDefinitionWithDeclarationsTargetStillReportsDrift()
    {
        using var temp = TestCli.Temp("generate");
        var folder = ApplicationFolder(temp.Path);
        var model = CatalogModel(
            temp.Path,
            folder,
            """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}""");
        File.WriteAllText(
            Path.Combine(folder, "BorrowCopyCommandAuthorizationDefinition.cs"),
            "internal sealed class BorrowCopyCommandAuthorizationDefinition\n{\n    public string Policy => AuthorizationDeclaration.Authorized(\"permission/other-thing\").ToString();\n}\n");

        var finding = Assert.Single(RunDefinitionChecks(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Authorization definition 'BorrowCopyCommandAuthorizationDefinition' declares no permission 'approve-expense' for 'use-case/lending.borrow-copy'.", finding.Message);
    }

    /// <summary>The single target error a declarations target carrying the field reports.</summary>
    /// <param name="field">The target field line, indented two spaces.</param>
    /// <returns>The target error.</returns>
    static string PlanError(string field)
    {
        using var temp = ModelFixture.Prepare("declarations");
        AppendGenerate(
            temp.Path,
            "generate:\n- kind: declarations\n  stack: dotnet\n  path: apps/api/src/Modules/Library.{Module}\n  namespace: Library.{Module}\n" + field + "\n");

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        return Assert.Single(plan.Errors);
    }

    /// <summary>Runs the definition check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The definition findings.</returns>
    static List<Finding> RunDefinitionChecks(ProjectModel model)
    {
        var findings = new List<Finding>();
        DefinitionChecks.Run(model, findings);

        return findings.Where(finding => finding.Code is "definition-drift" or "definition-missing").ToList();
    }

    /// <summary>A model with a declarations target, the use case record and its handler in the Application folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="folder">The Application folder holding the handler.</param>
    /// <param name="json">The use case record fields as a JSON object.</param>
    /// <returns>The project model holding the target, the record and its handler.</returns>
    static ProjectModel CatalogModel(string root, string folder, string json)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var model = TestModel.Model(
            TestModel.Config(root),
            TestModel.Record(RecordTypes.UseCase, "use-case/lending.borrow-copy", Path.Combine(root, ".litespec", "borrow-copy.yaml"), json));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            "BorrowCopyCommandHandler",
            MessageType: "BorrowCopyCommand"));

        return model;
    }

    /// <summary>The use case Application folder holding the handler.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The created folder.</returns>
    static string ApplicationFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "BorrowCopy");
        Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>Renders the resolution fixture's catalog source for the declarations target.</summary>
    /// <returns>The rendered catalog text.</returns>
    static string ResolutionCatalog()
    {
        using var temp = ModelFixture.Prepare("declarations-resolution");
        AppendGenerate(temp.Path, """
            generate:
            - kind: declarations
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        return Assert.Single(plan.Files).Content;
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");

    /// <summary>Rewrites the tenant scope of one fixture use case record.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="record">The record path under .litespec, with forward slashes.</param>
    /// <param name="from">The tenant scope the record states.</param>
    /// <param name="tenant">The tenant scope to state.</param>
    static void RewriteTenant(string root, string record, string from, string tenant)
    {
        var path = Path.Combine(root, ".litespec", record.Replace('/', Path.DirectorySeparatorChar));

        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace($"  tenant: {from}", $"  tenant: {tenant}", StringComparison.Ordinal));
    }
}
