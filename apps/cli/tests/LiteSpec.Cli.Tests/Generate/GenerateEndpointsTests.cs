using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the endpoints generator writes one module endpoints class mapping each use case route.</summary>
public sealed class GenerateEndpointsTests
{
    /// <summary>Every HTTP-reached use case renders its golden nested endpoint class in the module file.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void EndpointsDotnetMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Equal(
            "apps/api/src/Modules/Library.Lending/LendingEndpoints.generated.cs",
            Path.GetRelativePath(temp.Path, file.AbsolutePath).Replace('\\', '/'));

        GenerateGoldens.AssertEqual("endpoints-dotnet.txt", file.Content);
    }

    /// <summary>Generate writes the endpoints file, and a second check finds it current.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateWritesEndpointsFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "LendingEndpoints.generated.cs");
        Assert.True(File.Exists(file), "endpoints file");
        Assert.Contains("internal static partial class LendingEndpoints", File.ReadAllText(file), StringComparison.Ordinal);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);
    }

    /// <summary>An endpoints target without a claim reader reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EndpointsTargetWithoutClaimReaderReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("claimReader", StringComparison.Ordinal));
    }

    /// <summary>An endpoints target with an unqualified claim reader reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EndpointsTargetWithUnqualifiedClaimReaderReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: ActorClaims.ReadActorId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("qualified", StringComparison.Ordinal));
    }

    /// <summary>An endpoints target for a TypeScript stack reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EndpointsTargetForTypeScriptReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: svelte
              path: apps/web/src/generated
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("'dotnet'", StringComparison.Ordinal));
    }

    /// <summary>A use case opting out with generated false renders no endpoint and lists no skip.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OptedOutUseCaseRendersNothing()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var borrow = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(borrow, File.ReadAllText(borrow).Replace(
            "  route: /api/loans\n",
            "  route: /api/loans\n  generated: false\n",
            StringComparison.Ordinal));

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.DoesNotContain("BorrowCopyEndpoint", file.Content, StringComparison.Ordinal);
        Assert.Contains("ReturnCopyEndpoint", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case carrying a file input binds the upload from the form and maps it to the bytes.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void FileInputBindsUploadToBytes()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(retire, "\n" + """
            input:
            - name: photo
              meaning: The photo of the retired copy.
              type: file
              contentTypes:
              - image/jpeg
              maxSize: 5 MB
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("using System.IO;", file.Content, StringComparison.Ordinal);
        Assert.Contains("RetireCopyEndpoint", file.Content, StringComparison.Ordinal);
        Assert.Contains("IFormFile photo", file.Content, StringComparison.Ordinal);
        Assert.Contains("await photo.CopyToAsync(photoStream, cancellationToken);", file.Content, StringComparison.Ordinal);
        Assert.Contains("Photo: photoBytes", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("RetireCopyRequest", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case carrying a file beside body inputs renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void FileInputWithBodyInputsSkipsWithReason()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(retire, "\n" + """
            input:
            - name: photo
              meaning: The photo of the retired copy.
              type: file
              contentTypes:
              - image/jpeg
              maxSize: 5 MB
              required: true
              origin: caller
              binding: body
              classification: internal
            - name: note
              meaning: Why the copy retires.
              type: text
              required: false
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var file = Assert.Single(plan.Files);
        Assert.DoesNotContain("RetireCopyEndpoint", file.Content, StringComparison.Ordinal);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        Assert.Contains("carries a file with body inputs", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A money input builds the shared money object from the bound amount with its currency.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyInputBuildsMoneyObject()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(retire, "\n" + """
            input:
            - name: fine
              meaning: The fine for the retired copy.
              type: money
              currency: EUR
              required: true
              origin: caller
              binding: query
              classification: internal
            - name: deposit
              meaning: The deposit returned for the retired copy.
              type: money
              currencyField: depositCurrency
              required: true
              origin: caller
              binding: query
              classification: internal
            - name: depositCurrency
              meaning: The ISO 4217 currency code of the deposit.
              type: text
              maxLength: 3
              required: true
              origin: caller
              binding: query
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("Fine: Money.Create(fine, \"EUR\")", file.Content, StringComparison.Ordinal);
        Assert.Contains("Deposit: Money.Create(deposit, depositCurrency)", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case implying a tenant key renders no endpoint without a tenantReader and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantImpliedEndpointSkipsWithReason()
    {
        using var temp = ModelFixture.Prepare("small");
        var project = Path.Combine(temp.Path, ".litespec", "project.yaml");
        File.WriteAllText(
            project,
            File.ReadAllText(project).Replace(
                "selectedExtensions: []",
                "selectedExtensions:\n- id: tenancy\n  criterion: Independent customers share one deployment.\n  tenant: term/workspace",
                StringComparison.Ordinal));
        var glossary = Path.Combine(temp.Path, ".litespec", "glossary.yaml");
        File.WriteAllText(
            glossary,
            File.ReadAllText(glossary).Replace(
                "- id: term/loan",
                "- id: term/workspace\n  term: workspace\n  meaning: One customer organization sharing the deployment.\n  layer: behavior\n- id: term/loan",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.borrow-copy");
        Assert.Contains("implies a tenant key", skip.Reason, StringComparison.Ordinal);
        Assert.Contains("tenantReader", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A use case implying a tenant key renders its endpoint reading the key from the caller through the tenantReader.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TenantImpliedEndpointRendersThroughTenantReader()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteTenancy(temp.Path);
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
              tenantReader: Library.Platform.Http.TenantClaims.ReadTenantId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("using Library.Platform.Http;", file.Content, StringComparison.Ordinal);
        Assert.Contains("ClaimsPrincipal user", file.Content, StringComparison.Ordinal);
        Assert.Contains("WorkspaceId: TenantClaims.ReadTenantId(user),", file.Content, StringComparison.Ordinal);
    }

    /// <summary>An endpoints target with an unqualified tenant reader reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EndpointsTargetWithUnqualifiedTenantReaderReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
              tenantReader: TenantClaims.ReadTenantId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("tenantReader", StringComparison.Ordinal));
    }

    /// <summary>A use case carrying a map input renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MapInputSkipsWithReason()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(retire, "\n" + """
            input:
            - name: countsByState
              meaning: The retired copies counted by lifecycle state.
              type: map
              keyType: text
              valueType: integer
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var file = Assert.Single(plan.Files);
        Assert.DoesNotContain("RetireCopyEndpoint", file.Content, StringComparison.Ordinal);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        Assert.Contains("carries a map", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An integer input stating size 64 binds as a long.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SizedIntegerBindsLong()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var loan = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "extend-loan", "extend-loan.yaml");
        File.WriteAllText(
            loan,
            File.ReadAllText(loan).Replace(
                "- name: days\n  meaning: The number of days the loan is extended.\n  type: integer\n",
                "- name: days\n  meaning: The number of days the loan is extended.\n  type: integer\n  size: 64\n",
                StringComparison.Ordinal));

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("[FromQuery(Name = \"days\")] long days", file.Content, StringComparison.Ordinal);
        Assert.Contains("Days: days", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A command stating a created location answers 201 with the location over the shaped result.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CreatedCommandRendersLocationAndShape()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [CreatedEndpoint()],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            ["Claimwick.Spending.Contracts.Expenses"]);

        Assert.Contains(".Produces<SubmitExpenseResponseModel>(StatusCodes.Status201Created)", text, StringComparison.Ordinal);
        Assert.Contains("return TypedResults.Created(", text, StringComparison.Ordinal);
        Assert.Contains("$\"/api/expenses/{result.ExpenseId}\"", text, StringComparison.Ordinal);
        Assert.Contains("ShapeSubmitExpenseResponse(result)", text, StringComparison.Ordinal);
        Assert.Contains("static partial SubmitExpenseResponseModel ShapeSubmitExpenseResponse(SubmitExpenseCommandResult result);", text, StringComparison.Ordinal);
    }

    /// <summary>An unlisted choice label answers 400 naming the set before the message runs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ChoiceGuardRefusesUnknownLabel()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [new EndpointsRenderer.EndpointData(
                "use-case/spending.submit-expense",
                "SubmitExpense",
                "Submit Expense",
                null,
                "command",
                "POST",
                "/api/expenses",
                null,
                [new EndpointsRenderer.BoundInput(
                    "category", "Category", "category", "category", "choice", "body", true,
                    "The kind of cost.", "ExpenseCategory", null, null, "caller")],
                new Dictionary<string, string>(),
                false,
                [422],
                "SubmitExpenseCommand",
                "Claimwick.Spending.Application.Expenses.SubmitExpense",
                "SubmitExpenseCommandResult",
                "SubmitExpenseResponseModel",
                "Claimwick.Spending.WebApi.Expenses.SubmitExpense",
                MarkStyle.Xml)],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            []);

        Assert.Contains("if (!ExpenseCategory.TryFromCode(request.Category, out var category))", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Unknown expense category.\"", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status400BadRequest)", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status422UnprocessableEntity)", text, StringComparison.Ordinal);
        Assert.Contains("Category: category", text, StringComparison.Ordinal);
    }

    /// <summary>A derived value the clock rule does not cover renders a derivation partial the sibling implements.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DerivedValueRendersDerivationPartial()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [new EndpointsRenderer.EndpointData(
                "use-case/spending.change-reminder-preference",
                "ChangeReminderPreference",
                "Change Reminder Preference",
                null,
                "command",
                "PUT",
                "/api/reminder-preferences/{managerId}",
                null,
                [
                    new EndpointsRenderer.BoundInput(
                        "manager", "ManagerId", "managerId", "managerId", "id", "route", true,
                        "The manager the preference belongs to.", null, null, null, "caller"),
                    new EndpointsRenderer.BoundInput(
                        "changerIsFinance", "ChangerIsFinance", "changerIsFinance", "changerIsFinance", "boolean", "derived", false,
                        "Whether the changer holds the finance role.", null, null, null, "derived"),
                ],
                new Dictionary<string, string>(),
                false,
                [],
                "ChangeReminderPreferenceCommand",
                "Claimwick.Spending.Application.ReminderPreferences.ChangeReminderPreference",
                "ChangeReminderPreferenceCommandResult",
                "ChangeReminderPreferenceResponseModel",
                "Claimwick.Spending.WebApi.ReminderPreferences.ChangeReminderPreference",
                MarkStyle.Xml)],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            []);

        Assert.Contains("ClaimsPrincipal user", text, StringComparison.Ordinal);
        Assert.Contains("ChangerIsFinance: DeriveChangeReminderPreferenceChangerIsFinance(user)", text, StringComparison.Ordinal);
        Assert.Contains("static partial bool DeriveChangeReminderPreferenceChangerIsFinance(ClaimsPrincipal user);", text, StringComparison.Ordinal);
    }

    /// <summary>An optional body id maps an absent value to null and a carried one through its factory.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OptionalBodyIdMapsNull()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [new EndpointsRenderer.EndpointData(
                "use-case/spending.submit-expense",
                "SubmitExpense",
                "Submit Expense",
                null,
                "command",
                "POST",
                "/api/expenses",
                null,
                [new EndpointsRenderer.BoundInput(
                    "receipt", "ReceiptId", "receiptId", "receiptId", "id", "body", false,
                    "The proof of payment.", null, "ReceiptId", null, "caller")],
                new Dictionary<string, string>(),
                false,
                [],
                "SubmitExpenseCommand",
                "Claimwick.Spending.Application.Expenses.SubmitExpense",
                "SubmitExpenseCommandResult",
                "SubmitExpenseResponseModel",
                "Claimwick.Spending.WebApi.Expenses.SubmitExpense",
                MarkStyle.Xml)],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            []);

        Assert.Contains("internal sealed partial record SubmitExpenseRequest(Guid? ReceiptId);", text, StringComparison.Ordinal);
        Assert.Contains("ReceiptId: request.ReceiptId is null ? null : ReceiptId.From(request.ReceiptId.Value)", text, StringComparison.Ordinal);
    }

    /// <summary>A project with attribute marks renders the use case mark as an attribute.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AttributeMarksRenderAsAttributes()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.borrow-copy",
                "BorrowCopy",
                "Borrow copy",
                null,
                "command",
                "POST",
                "/api/loans",
                null,
                [],
                new Dictionary<string, string>(),
                false,
                [409],
                "BorrowCopyCommand",
                "Library.Lending.Application.Loans.BorrowCopy",
                "BorrowCopyCommandResult",
                "BorrowCopyResponseModel",
                "Library.Lending.WebApi.Loans.BorrowCopy",
                MarkStyle.Attribute)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("[Implements(\"use-case/lending.borrow-copy\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("<implements>", text, StringComparison.Ordinal);
    }

    /// <summary>A route input renamed for the URL binds under its route parameter name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void RenamedRouteInputMatchesGolden()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [new EndpointsRenderer.EndpointData(
                "use-case/spending.change-reminder-preference",
                "ChangeReminderPreference",
                "Change Reminder Preference",
                null,
                "command",
                "PUT",
                "/api/reminder-preferences/{id}",
                null,
                [new EndpointsRenderer.BoundInput(
                    "manager", "ManagerId", "id", "id", "id", "route", true,
                    "The manager the preference belongs to.", null, null, null, "caller")],
                new Dictionary<string, string>(),
                false,
                [],
                "ChangeReminderPreferenceCommand",
                "Claimwick.Spending.Application.ReminderPreferences.ChangeReminderPreference",
                "ChangeReminderPreferenceCommandResult",
                "ChangeReminderPreferenceResponseModel",
                "Claimwick.Spending.WebApi.ReminderPreferences.ChangeReminderPreference",
                MarkStyle.Xml)],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            []);

        GenerateGoldens.AssertEqual("endpoints-route-parameter.txt", text);
    }

    /// <summary>An upstream refusal answers 503 Service Unavailable.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UpstreamRefusalRendersServiceUnavailable()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [new EndpointsRenderer.EndpointData(
                "use-case/spending.reimburse-expense",
                "ReimburseExpense",
                "Reimburse Expense",
                null,
                "command",
                "POST",
                "/api/expenses/{expenseId}:reimburse",
                null,
                [new EndpointsRenderer.BoundInput(
                    "expense", "ExpenseId", "expenseId", "expenseId", "id", "route", true,
                    "The approved expense to reimburse.", null, "ExpenseId", null, "caller")],
                new Dictionary<string, string>(),
                false,
                [503],
                "ReimburseExpenseCommand",
                "Claimwick.Spending.Application.Expenses.ReimburseExpense",
                "ReimburseExpenseCommandResult",
                "ReimburseExpenseResponseModel",
                "Claimwick.Spending.WebApi.Expenses.ReimburseExpense",
                MarkStyle.Xml)],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            []);

        Assert.Contains(".ProducesProblem(StatusCodes.Status503ServiceUnavailable)", text, StringComparison.Ordinal);
    }

    /// <summary>A route parameter name in the record binds the endpoint under that name through the plan.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RouteParameterNameInRecordBindsUnderThatName()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);
        var loan = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "return-copy", "return-copy.yaml");
        var yaml = File.ReadAllText(loan)
            .Replace("route: /api/loans/{loanId}:return", "route: /api/loans/{id}:return", StringComparison.Ordinal)
            .Replace("- name: loanId\n  meaning: The loan whose copy the reader returns.", "- name: loan\n  meaning: The loan whose copy the reader returns.", StringComparison.Ordinal)
            .Replace("  binding: route\n  classification: internal", "  binding: route\n  routeParameter: id\n  classification: internal", StringComparison.Ordinal);
        File.WriteAllText(loan, yaml);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));
        var file = Assert.Single(plan.Files);

        Assert.Contains("[FromRoute(Name = \"id\")]", file.Content, StringComparison.Ordinal);
        Assert.Contains("Loan: id", file.Content, StringComparison.Ordinal);
    }

    /// <summary>One submit endpoint stating a created location, for the created-response test.</summary>
    /// <returns>The endpoint the plan would read from the submit-expense record.</returns>
    static EndpointsRenderer.EndpointData CreatedEndpoint() =>
        new(
            "use-case/spending.submit-expense",
            "SubmitExpense",
            "Submit Expense",
            "An employee files a work expense so it can be decided and reimbursed.",
            "command",
            "POST",
            "/api/expenses",
            "/api/expenses/{expenseId}",
            [
                new EndpointsRenderer.BoundInput(
                    "expenseId", "ExpenseId", "expenseId", "expenseId", "id", "body", true,
                    "The identifier the app mints for one submission attempt.", null, "ExpenseId", null, "caller"),
                new EndpointsRenderer.BoundInput(
                    "employee", "EmployeeId", "employeeId", "employeeId", "id", "claim", false,
                    "The signed-in employee filing the expense.", null, null, "employee", "verified-claim"),
            ],
            new Dictionary<string, string> { ["expenseId"] = "ExpenseId" },
            true,
            [422],
            "SubmitExpenseCommand",
            "Claimwick.Spending.Application.Expenses.SubmitExpense",
            "SubmitExpenseCommandResult",
            "SubmitExpenseResponseModel",
            "Claimwick.Spending.WebApi.Expenses.SubmitExpense",
            MarkStyle.Xml);

    /// <summary>An endpoint reading its tenant key from the caller renders its golden file.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void TenantEndpointMatchesGolden()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.borrow-copy",
                "BorrowCopy",
                "Borrow copy",
                null,
                "command",
                "POST",
                "/api/loans",
                null,
                [
                    new EndpointsRenderer.BoundInput(
                        "copyId", "CopyId", "copyId", "copyId", "id", "body", true,
                        "The copy the reader borrows.", null, "CopyId", null, "caller"),
                    new EndpointsRenderer.BoundInput(
                        "readerId", "ReaderId", "readerId", "readerId", "id", "claim", false,
                        "The signed-in reader borrowing the copy, read from verified claims.", null, null, "reader", "verified-claim"),
                ],
                new Dictionary<string, string>(),
                false,
                [409],
                "BorrowCopyCommand",
                "Library.Lending.Application.Loans.BorrowCopy",
                "BorrowCopyCommandResult",
                "BorrowCopyResponseModel",
                "Library.Lending.WebApi.Loans.BorrowCopy",
                MarkStyle.Xml,
                new MessagesRenderer.TenantKey("WorkspaceId", "WorkspaceId"))],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            [],
            "Library.Platform.Http.TenantClaims.ReadTenantId");

        GenerateGoldens.AssertEqual("endpoints-tenant-dotnet.txt", text);
    }

    /// <summary>Selects tenancy with the workspace term and declares the term, so use cases imply the tenant key.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteTenancy(string root)
    {
        var project = Path.Combine(root, ".litespec", "project.yaml");
        File.WriteAllText(
            project,
            File.ReadAllText(project).Replace(
                "selectedExtensions: []",
                "selectedExtensions:\n- id: tenancy\n  criterion: Independent customers share one deployment.\n  tenant: term/workspace",
                StringComparison.Ordinal));
        var glossary = Path.Combine(root, ".litespec", "glossary.yaml");
        File.WriteAllText(
            glossary,
            File.ReadAllText(glossary).Replace(
                "- id: term/loan",
                "- id: term/workspace\n  term: workspace\n  meaning: One customer organization sharing the deployment.\n  layer: behavior\n- id: term/loan",
                StringComparison.Ordinal));
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
