using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the definition check: each message declares its pipeline positions beside the message, and each declaration matches its use case record.</summary>
public sealed class DefinitionCheckTests
{
    /// <summary>Definitions matching the record's grant, audit and idempotency report nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void MatchingDefinitionsReportNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}, "idempotency": {"mode": "key", "key": "expense-id", "scope": "tenant"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "(AuditDeclaration.Audited(\"use-case/lending.borrow-copy\") with { Category = \"lending\" }).WithTargetFrom<BorrowCopyCommand>(command => command.CopyId)");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "IdempotencyDeclaration.Idempotent(Key.From(\"expense-id\"))");

        Assert.Empty(Run(model));
    }

    /// <summary>A message without its authorization definition reports definition-missing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void MissingAuthorizationDefinitionReportsDefinitionMissing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-missing", finding.Code);
        Assert.Equal("Message 'BorrowCopyCommand' has no Authorization definition for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A message without its audit definition reports definition-missing when the record declares audit.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void MissingAuditDefinitionReportsDefinitionMissing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-missing", finding.Code);
        Assert.Equal("Message 'BorrowCopyCommand' has no Audit definition for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A message without audit or idempotency on its record needs no audit or idempotency definition.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void UndeclaredPositionsNeedNoDefinitions()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");

        Assert.Empty(Run(model));
    }

    /// <summary>An authorization definition declaring another permission than the record's grant reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuthorizationPolicyDriftReportsDefinitionDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"refuse-expense\"))");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Authorization definition 'BorrowCopyCommandAuthorizationDefinition' declares no permission 'approve-expense' for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An authorization definition naming the record's permission through its constant reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuthorizationPermissionConstantReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(LendingPermissions.ApproveExpense)");

        Assert.Empty(Run(model));
    }

    /// <summary>An authorization definition naming another permission's constant reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuthorizationPermissionConstantDriftReportsDefinitionDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(LendingPermissions.RefuseExpense)");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Authorization definition 'BorrowCopyCommandAuthorizationDefinition' declares no permission 'approve-expense' for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An authorization definition without an exemption for an anonymous grant reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AnonymousGrantWithoutExemptionReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "anonymous", "tenant": "public"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Public() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized()");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Authorization definition 'BorrowCopyCommandAuthorizationDefinition' declares no exemption for the anonymous grant of 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An authorization definition authorizing the anonymous policy for an anonymous grant reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AnonymousGrantWithAnonymousPolicyReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "anonymous", "tenant": "public"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Public() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Anonymous)");

        Assert.Empty(Run(model));
    }

    /// <summary>An authorization definition exempting an anonymous grant reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AnonymousGrantWithExemptionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "anonymous", "tenant": "public"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Public() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Exempt(\"The catalog is open to every caller.\")");

        Assert.Empty(Run(model));
    }

    /// <summary>An authorization definition authorizing another policy than the anonymous grant reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AnonymousGrantWithPermissionPolicyReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "anonymous", "tenant": "public"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Public() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Authorization definition 'BorrowCopyCommandAuthorizationDefinition' declares no exemption for the anonymous grant of 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An authorization definition without the RecentSignIn policy the record requires reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void RecentSignInWithoutPolicyReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "signed-in", "tenant": "own", "recentSignInWindow": "setting/security.recent-sign-in-window"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized()");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Authorization definition 'BorrowCopyCommandAuthorizationDefinition' declares no RecentSignIn policy for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An audit definition without the exemption an exempt record needs reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditExemptWithoutExemptionReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"exemptBecause": "reading only"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "(AuditDeclaration.Audited(\"use-case/lending.borrow-copy\") with { Category = \"lending\" }).WithTargetFrom<BorrowCopyCommand>(command => command.CopyId)");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Audit definition 'BorrowCopyCommandAuditDefinition' declares no audit exemption for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An audit definition declaring the exemption an exempt record needs reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditExemptWithExemptionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"exemptBecause": "reading only"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "AuditDeclaration.Exempt(\"Reads leave their trail in the loan states and events.\")");

        Assert.Empty(Run(model));
    }

    /// <summary>An audit definition declaring an exemption for a record that requires audit reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditExemptWhenRequiredReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "AuditDeclaration.Exempt(\"Reads leave their trail in the loan states and events.\")");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Audit definition 'BorrowCopyCommandAuditDefinition' declares an audit exemption, but 'use-case/lending.borrow-copy' requires audit.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An audit definition without a target for a record that requires audit reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditWithoutTargetReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "AuditDeclaration.Audited(\"use-case/lending.borrow-copy\")");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Audit definition 'BorrowCopyCommandAuditDefinition' declares no target for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An audit definition without a target extraction reports nothing when the handler adds the target it learns.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditTargetAddedByHandlerReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "AuditDeclaration.Audited(\"use-case/lending.borrow-copy\") with { Category = \"lending\", TargetKind = \"copy\" }");
        File.WriteAllText(Path.Combine(folder, "BorrowCopyCommandHandler.cs"), "internal sealed class BorrowCopyCommandHandler\n{\n    private void Handle() => _audit.WithTarget(copy.Id);\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>An audit exemption naming the use case reports nothing when a sibling definition audits its action.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditExemptCoveredBySiblingReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "AuditDeclaration.Exempt(\"The hold only prepares use-case/lending.borrow-copy; the borrowing records the change once it completes.\")");
        WriteDefinition(folder, "ReserveCopyCommandAuditDefinition.cs", "(AuditDeclaration.Audited(\"use-case/lending.borrow-copy\") with { Category = \"lending\" }).WithTargetFrom<ReserveCopyCommand>(command => command.CopyId)");

        Assert.Empty(Run(model));
    }

    /// <summary>An audit exemption naming the use case reports definition-drift when no sibling audits its action.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void AuditExemptWithoutAuditingSiblingReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "audit": {"level": "required", "targetKind": "expense"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandAuditDefinition.cs", "AuditDeclaration.Exempt(\"The hold only prepares use-case/lending.borrow-copy; the borrowing records the change once it completes.\")");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Audit definition 'BorrowCopyCommandAuditDefinition' declares an audit exemption, but 'use-case/lending.borrow-copy' requires audit.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An idempotency definition without the exemption a non-idempotent record needs reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void IdempotencyNoneWithoutExemptionReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "none", "reason": "reading only"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "IdempotencyDeclaration.Idempotent(Key.From(\"copy-id\"))");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares no idempotency exemption for 'use-case/lending.borrow-copy'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An idempotency definition declaring through the idempotency declaration reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void IdempotencyDeclarationShapeReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        File.WriteAllText(
            Path.Combine(folder, "BorrowCopyCommandIdempotencyDefinition.cs"),
            "internal sealed class BorrowCopyCommandIdempotencyDefinition : IIdempotencyDefinition<BorrowCopyCommand>\n{\n    public IdempotencyDeclaration Idempotency { get; } = new IdempotencyDeclaration(static message => \"copy-id\");\n}\n");

        Assert.Empty(Run(model));
    }

    /// <summary>An idempotency definition exempting a command the record makes idempotent reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void NotIdempotentWhenKeyReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "IdempotencyDeclaration.NotIdempotent()");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares an idempotency exemption, but 'use-case/lending.borrow-copy' requires idempotency.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An idempotency definition declaring the exemption with its reason for a non-idempotent record reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void IdempotencyNoneWithExemptReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "none", "reason": "reading only"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "new Exempt<IdempotencyDeclaration>(\"Each call lends its own copy, and lending an unavailable copy is refused.\")");

        Assert.Empty(Run(model));
    }

    /// <summary>An idempotency definition declaring the exemption for a command its record recognizes by state reports nothing.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void IdempotencyStateWithExemptReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "state", "recognizedBy": "the copy already lent to the member", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "declarations.Exempt<IdempotencyDeclaration>(\"recognized by state: the copy already lent to the member\")");

        Assert.Empty(Run(model));
    }

    /// <summary>An idempotency definition keying a command its record recognizes by state reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void IdempotencyStateWithKeyReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "state", "recognizedBy": "the copy already lent to the member", "replay": "returns-first-result"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "IdempotencyDeclaration.Idempotent(Key.From(\"copy-id\"))");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares no idempotency exemption for 'use-case/lending.borrow-copy', which recognizes a repeat by state; a pipeline key would outlive that state.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An idempotency definition declaring the exemption for a command the record makes idempotent reports definition-drift.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void ExemptWhenKeyReportsDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = DefinitionModel(temp.Path, folder, """{"access": {"grant": "permission/approve-expense", "tenant": "own"}, "idempotency": {"mode": "key", "key": "copy-id", "scope": "tenant"}}""");
        WriteDefinition(folder, "BorrowCopyCommandTenancyDefinition.cs", "TenancyDeclaration.Required() : ITenancyDefinition<BorrowCopyCommand>");
        WriteDefinition(folder, "BorrowCopyCommandAuthorizationDefinition.cs", "AuthorizationDeclaration.Authorized(AccessPolicies.Permission(\"approve-expense\"))");
        WriteDefinition(folder, "BorrowCopyCommandIdempotencyDefinition.cs", "new Exempt<IdempotencyDeclaration>(\"Each call lends its own copy, and lending an unavailable copy is refused.\")");

        var finding = Assert.Single(Run(model));

        Assert.Equal("definition-drift", finding.Code);
        Assert.Equal("Idempotency definition 'BorrowCopyCommandIdempotencyDefinition' declares an idempotency exemption, but 'use-case/lending.borrow-copy' requires idempotency.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A handler without a message type keeps no definition rule.</summary>
    /// <proves>litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis</proves>
    [Fact]
    public void HandlerWithoutMessageTypeReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(RecordTypes.UseCase, "use-case/lending.borrow-copy", Path.Combine(temp.Path, ".litespec", "borrow-copy.yaml"), """{"access": {"grant": "permission/approve-expense", "tenant": "own"}}"""));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            "BorrowCopyCommandHandler"));

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the definition check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The definition findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        DefinitionChecks.Run(model, findings);

        return findings.Where(finding => finding.Code is "definition-drift" or "definition-missing").ToList();
    }

    /// <summary>A model with the use case record and its handler in the Application folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="folder">The Application folder holding the handler.</param>
    /// <param name="json">The use case record fields as a JSON object.</param>
    /// <returns>The project model holding the record and its handler.</returns>
    static ProjectModel DefinitionModel(string root, string folder, string json)
    {
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

    /// <summary>The use case Application folder holding the handler and its definitions.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The Application folder.</returns>
    static string ApplicationFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "BorrowCopy");
        Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>Writes one definition file declaring the text.</summary>
    /// <param name="folder">The Application folder holding the definition.</param>
    /// <param name="file">The definition file name.</param>
    /// <param name="declaration">The declaration text the file holds.</param>
    static void WriteDefinition(string folder, string file, string declaration)
    {
        File.WriteAllText(Path.Combine(folder, file), "internal sealed class " + Path.GetFileNameWithoutExtension(file) + "\n{\n    // " + declaration + "\n}\n");
    }
}
