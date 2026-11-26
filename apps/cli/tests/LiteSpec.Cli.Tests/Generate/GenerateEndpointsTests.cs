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
        Assert.Contains("Fine: new Money(fine, \"EUR\")", file.Content, StringComparison.Ordinal);
        Assert.Contains("Deposit: new Money(deposit, depositCurrency)", file.Content, StringComparison.Ordinal);
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
        Assert.Contains("WorkspaceId: TenantClaims.ReadTenantId<Guid>(user),", file.Content, StringComparison.Ordinal);
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

    /// <summary>A use case carrying an object input binds it from the body as one request row mapped to the message row type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ObjectInputBindsSingleRowFromBody()
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
            - name: shelfMark
              meaning: The shelf mark the retirement files the copy under.
              type: object
              rowType: ShelfMark
              rowShape:
                fields:
                - name: shelf
                  meaning: The shelf.
                  type: text
                  required: true
                - name: slot
                  meaning: The slot on the shelf.
                  type: integer
                  required: true
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("internal sealed partial record RetireCopyShelfMarkRow(string Shelf, int Slot);", file.Content, StringComparison.Ordinal);
        Assert.Contains("RetireCopyShelfMarkRow ShelfMark", file.Content, StringComparison.Ordinal);
        Assert.Contains("Shelf: request.ShelfMark.Shelf,", file.Content, StringComparison.Ordinal);
        Assert.Contains("Slot: request.ShelfMark.Slot", file.Content, StringComparison.Ordinal);
        Assert.Contains("ShelfMark: shelfMark", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case carrying a list that reuses an aggregate part renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PartRowListSkipsWithReason()
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
            - name: renewals
              meaning: The renewals the retirement files the copy under.
              type: list
              rowType: aggregate/lending.loan#renewal
              rowShape:
                fields:
                - name: renewedAt
                  meaning: The renewal time.
                  type: instant
                  required: true
                - name: note
                  meaning: The renewal note.
                  type: text
                  required: false
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
        Assert.Contains("reusing an aggregate part", skip.Reason, StringComparison.Ordinal);
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
        Assert.Contains("private static partial SubmitExpenseResponseModel ShapeSubmitExpenseResponse(SubmitExpenseCommandResult result);", text, StringComparison.Ordinal);
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

    /// <summary>A DELETE endpoint with body inputs states its body binding, because minimal APIs infer a body only for POST, PUT and PATCH.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DeleteBodyBindsFromBody()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [new EndpointsRenderer.EndpointData(
                "use-case/spending.withdraw-expense",
                "WithdrawExpense",
                "Withdraw Expense",
                null,
                "command",
                "DELETE",
                "/api/expenses",
                null,
                [new EndpointsRenderer.BoundInput(
                    "reason", "Reason", "reason", "reason", "text", "body", true,
                    "Why the expense is withdrawn.", null, null, null, "caller")],
                new Dictionary<string, string>(),
                false,
                [],
                "WithdrawExpenseCommand",
                "Claimwick.Spending.Application.Expenses.WithdrawExpense",
                "WithdrawExpenseCommandResult",
                "WithdrawExpenseResponseModel",
                "Claimwick.Spending.WebApi.Expenses.WithdrawExpense",
                MarkStyle.Xml)],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            []);

        Assert.Contains("[FromBody] WithdrawExpenseRequest request,", text, StringComparison.Ordinal);
        Assert.Contains("using Microsoft.AspNetCore.Mvc;", text, StringComparison.Ordinal);
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
        Assert.Contains("private static partial bool? DeriveChangeReminderPreferenceChangerIsFinance(ClaimsPrincipal user);", text, StringComparison.Ordinal);
    }

    /// <summary>An optional body id maps an absent value to null and a carried one through its non-throwing parse, answering 400 for a value the identifier refuses.</summary>
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
        Assert.Contains("ReceiptId? receiptId = null;", text, StringComparison.Ordinal);
        Assert.Contains("if (!ReceiptId.TryParse(request.ReceiptId.Value.ToString(), null, out var parsed))", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Invalid receipt.\"", text, StringComparison.Ordinal);
        Assert.Contains("ReceiptId: receiptId", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".From(", text, StringComparison.Ordinal);
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
                MarkStyle.Xml,
                null,
                null,
                true)],
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
                new MessagesRenderer.TenantKey("WorkspaceId", "WorkspaceId"),
                null,
                true)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            [],
            "Library.Platform.Http.TenantClaims.ReadTenantId");

        GenerateGoldens.AssertEqual("endpoints-tenant-dotnet.txt", text);
    }

    /// <summary>A use case carrying a body list renders a request row record mapping each row to the message row type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListBodyInputBindsRowsToMessage()
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
            - name: splits
              meaning: The cost splits of the retirement, one row per cost center.
              type: list
              rowShape:
                fields:
                - name: costCenter
                  meaning: The cost center carrying the split.
                  type: text
                  required: true
                - name: share
                  meaning: The share of the cost the split carries.
                  type: decimal
                  required: true
              rowType: RetireSplit
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("internal sealed partial record RetireCopySplitsRow(string CostCenter, decimal Share);", file.Content, StringComparison.Ordinal);
        Assert.Contains("RetireCopySplitsRow[] Splits", file.Content, StringComparison.Ordinal);
        Assert.Contains("var splitsRows = new List<RetireSplit>(request.Splits.Length);", file.Content, StringComparison.Ordinal);
        Assert.Contains("foreach (var row in request.Splits)", file.Content, StringComparison.Ordinal);
        Assert.Contains("CostCenter: row.CostCenter,", file.Content, StringComparison.Ordinal);
        Assert.Contains("Splits: splitsRows", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A list row maps identifiers through their non-throwing parse and money through the shared object, answering 400 for a value the identifier refuses.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListRowWithIdentifierAndFixedMoneyBinds()
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
            - name: fines
              meaning: The fines cleared by the retirement, one row per fine.
              type: list
              rowShape:
                fields:
                - name: copy
                  meaning: The copy the fine was charged on.
                  type: id
                  identifies: aggregate/lending.copy
                  required: true
                - name: amount
                  meaning: The cleared amount.
                  type: money
                  currency: EUR
                  required: false
              rowType: ClearedFine
              required: false
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("internal sealed partial record RetireCopyFinesRow(Guid Copy, decimal? Amount);", file.Content, StringComparison.Ordinal);
        Assert.Contains("List<ClearedFine>? finesRows = null;", file.Content, StringComparison.Ordinal);
        Assert.Contains("if (!CopyId.TryParse(row.Copy.ToString(), null, out var finesCopy))", file.Content, StringComparison.Ordinal);
        Assert.Contains("title: \"Invalid copy.\"", file.Content, StringComparison.Ordinal);
        Assert.Contains("Copy: finesCopy,", file.Content, StringComparison.Ordinal);
        Assert.Contains("Amount: row.Amount is null ? null : new Money(row.Amount.Value, \"EUR\")", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(".From(", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A single-field list without a row type binds as an array of its wire type.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleFieldListBindsArray()
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
            - name: tags
              meaning: The labels the retirement is filed under.
              type: list
              rowShape:
                fields:
                - name: tag
                  meaning: One label.
                  type: text
                  required: true
              required: false
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("string[]? Tags", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("TagsRow", file.Content, StringComparison.Ordinal);
        Assert.Contains("Tags: request.Tags", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A use case carrying a list outside the body renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ListOutsideBodySkipsWithReason()
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
        var list = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "list-loans", "list-loans.yaml");
        File.WriteAllText(
            list,
            File.ReadAllText(list).Replace(
                "  classification: personal\noutput:",
                "  classification: personal\n- name: states\n  meaning: The loan states to list.\n  type: list\n  rowShape:\n    fields:\n    - name: state\n      meaning: One state.\n      type: text\n      required: true\n  required: false\n  origin: caller\n  binding: query\n  classification: internal\noutput:",
                StringComparison.Ordinal));

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var file = Assert.Single(plan.Files);
        Assert.DoesNotContain("ListLoansEndpoint", file.Content, StringComparison.Ordinal);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.list-loans");
        Assert.Contains("carries a list outside the body", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A many-valued body input binds as an array.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ManyBodyInputBindsArray()
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
            - name: labels
              meaning: The labels the retirement is filed under.
              type: text
              many: true
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("string[] Labels", file.Content, StringComparison.Ordinal);
        Assert.Contains("Labels: request.Labels", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A many-valued query input binds as a repeated query parameter.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ManyQueryInputBindsRepeatedParameter()
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
        var list = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "list-loans", "list-loans.yaml");
        File.WriteAllText(
            list,
            File.ReadAllText(list).Replace(
                "  classification: personal\noutput:",
                "  classification: personal\n- name: states\n  meaning: The loan states to list, all states when absent.\n  type: text\n  many: true\n  required: false\n  origin: caller\n  binding: query\n  classification: internal\noutput:",
                StringComparison.Ordinal));

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("[FromQuery(Name = \"states\")] string[]? states", file.Content, StringComparison.Ordinal);
        Assert.Contains("States: states", file.Content, StringComparison.Ordinal);
    }

    /// <summary>An unlisted label in a many-valued choice answers 400 naming the set before the message runs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ManyChoiceInputGuardsEachLabel()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.list-loans",
                "ListLoans",
                "List loans",
                null,
                "query",
                "GET",
                "/api/loans",
                null,
                [new EndpointsRenderer.BoundInput(
                    "states", "States", "states", "states", "choice", "query", false,
                    "The loan states to list.", "LoanState", null, null, "caller", Many: true)],
                new Dictionary<string, string>(),
                true,
                [],
                "ListLoansQuery",
                "Library.Lending.Application.Loans.ListLoans",
                "ListLoansQueryResult",
                "ListLoansResponseModel",
                "Library.Lending.WebApi.Loans.ListLoans",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("[FromQuery(Name = \"states\")] string[]? states", text, StringComparison.Ordinal);
        Assert.Contains("List<LoanState>? statesItems = null;", text, StringComparison.Ordinal);
        Assert.Contains("foreach (var statesCode in states)", text, StringComparison.Ordinal);
        Assert.Contains("if (!LoanState.TryFromCode(statesCode, out var statesMember))", text, StringComparison.Ordinal);
        Assert.Contains("statesItems.Add(statesMember);", text, StringComparison.Ordinal);
        Assert.Contains("States: statesItems", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status400BadRequest)", text, StringComparison.Ordinal);
    }

    /// <summary>A money input taking its currency from an aggregate binds the amount as a decimal.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyWithCurrencyHomeBindsDecimal()
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
            - name: fee
              meaning: The processing fee of the retirement, in the currency of the copy record.
              type: money
              currencyHome: aggregate/lending.copy
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("decimal Fee", file.Content, StringComparison.Ordinal);
        Assert.Contains("Fee: request.Fee", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Money.Create", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A non-identity claim input reads through the target's claim value reader.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NonIdentityClaimReadsThroughClaimValueReader()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
              claimValueReader: Library.Platform.Http.ClaimValues.ReadClaimValue
            """);
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(retire, "\n" + """
            input:
            - name: isStaff
              meaning: Whether the caller holds a staff role, read from verified claims.
              type: boolean
              required: false
              origin: verified-claim
              verifiedBy: entry-point/library-app
              binding: claim
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("using Library.Platform.Http;", file.Content, StringComparison.Ordinal);
        Assert.Contains("IsStaff: ClaimValues.ReadClaimValue<bool>(user, \"isStaff\")", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A non-identity claim input without a claim value reader renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NonIdentityClaimWithoutValueReaderSkipsWithReason()
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
            - name: isStaff
              meaning: Whether the caller holds a staff role, read from verified claims.
              type: boolean
              required: false
              origin: verified-claim
              verifiedBy: entry-point/library-app
              binding: claim
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var file = Assert.Single(plan.Files);
        Assert.DoesNotContain("RetireCopyEndpoint", file.Content, StringComparison.Ordinal);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        Assert.Contains("names no claimValueReader", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An endpoints target with an unqualified claim value reader reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EndpointsTargetWithUnqualifiedClaimValueReaderReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
              claimValueReader: ClaimValues.ReadClaimValue
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("claimValueReader", StringComparison.Ordinal));
    }

    /// <summary>A list row taking its currency from an optional sibling binds the amount as a decimal the handler joins.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RowMoneyWithOptionalSiblingCurrencyBindsDecimal()
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
            - name: fines
              meaning: The fines cleared by the retirement, one row per fine.
              type: list
              rowShape:
                fields:
                - name: amount
                  meaning: The cleared amount.
                  type: money
                  currencyField: currency
                  required: true
                - name: currency
                  meaning: The ISO 4217 code of the amount, the fine currency when absent.
                  type: text
                  required: false
              rowType: ClearedFine
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("internal sealed partial record RetireCopyFinesRow(decimal Amount, string? Currency);", file.Content, StringComparison.Ordinal);
        Assert.Contains("Amount: row.Amount,", file.Content, StringComparison.Ordinal);
        Assert.Contains("Currency: row.Currency", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Money.Create", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A multi-field list without a row type renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MultiFieldListWithoutRowTypeSkipsWithReason()
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
            - name: splits
              meaning: The cost splits of the retirement, one row per cost center.
              type: list
              rowShape:
                fields:
                - name: costCenter
                  meaning: The cost center carrying the split.
                  type: text
                  required: true
                - name: share
                  meaning: The share of the cost the split carries.
                  type: decimal
                  required: true
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
        Assert.Contains("no rowType", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An endpoint binding collections, aggregate-currency money, and a claim value renders its golden file.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void BindingCollectionsMatchesGolden()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.retire-copy",
                "RetireCopy",
                "Retire copy",
                null,
                "command",
                "POST",
                "/api/copies:retire",
                null,
                [
                    new EndpointsRenderer.BoundInput(
                        "labels", "Labels", "labels", "labels", "text", "body", true,
                        "The labels the retirement is filed under.", null, null, null, "caller", Many: true),
                    new EndpointsRenderer.BoundInput(
                        "splits", "Splits", "splits", "splits", "list", "body", true,
                        "The cost splits of the retirement, one row per cost center.", null, null, null, "caller",
                        RowType: "RetireSplit",
                        Rows:
                        [
                            new EndpointsRenderer.RowField(
                                "costCenter", "CostCenter", "text", true,
                                "The cost center carrying the split.", null, null, null, null, null),
                            new EndpointsRenderer.RowField(
                                "share", "Share", "money", true,
                                "The share of the cost the split carries.", null, null, null, null, "aggregate/lending.copy"),
                        ]),
                    new EndpointsRenderer.BoundInput(
                        "fee", "Fee", "fee", "fee", "money", "body", true,
                        "The processing fee of the retirement, in the currency of the copy record.", null, null, null, "caller",
                        CurrencyHome: "aggregate/lending.copy"),
                    new EndpointsRenderer.BoundInput(
                        "isStaff", "IsStaff", "isStaff", "isStaff", "boolean", "claim", false,
                        "Whether the caller holds a staff role, read from verified claims.", null, null, "isStaff", "verified-claim"),
                ],
                new Dictionary<string, string>(),
                false,
                [409],
                "RetireCopyCommand",
                "Library.Lending.Application.Copies.RetireCopy",
                "RetireCopyCommandResult",
                "RetireCopyResponseModel",
                "Library.Lending.WebApi.Copies.RetireCopy",
                MarkStyle.Xml,
                null,
                null,
                true)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            [],
            "",
            "Library.Lending.WebApi.ActorClaims.ReadClaimValue");

        GenerateGoldens.AssertEqual("endpoints-binding-dotnet.txt", text);
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

    /// <summary>A list of one file per row binds from the multipart form, one part per row.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleFileRowListBindsFromForm()
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
            - name: photos
              meaning: The condition photos the retirement files the copy under.
              type: list
              rowShape:
                fields:
                - name: photo
                  meaning: One condition photo.
                  type: file
                  contentTypes:
                  - image/png
                  maxSize: 5 MB
                  required: true
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("RetireCopyEndpoint", file.Content, StringComparison.Ordinal);
        Assert.Contains("IFormFileCollection photos", file.Content, StringComparison.Ordinal);
        Assert.Contains("foreach (var part in photos)", file.Content, StringComparison.Ordinal);
        Assert.Contains("Photos: photosBytes", file.Content, StringComparison.Ordinal);
        Assert.Contains("using System.IO;", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A list of one file per row renders its golden nested endpoint class in the module file.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void FileRowListMatchesGolden()
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
            - name: photos
              meaning: The condition photos the retirement files the copy under.
              type: list
              rowShape:
                fields:
                - name: photo
                  meaning: One condition photo.
                  type: file
                  contentTypes:
                  - image/png
                  maxSize: 5 MB
                  required: true
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);

        GenerateGoldens.AssertEqual("endpoints-file-rows-dotnet.txt", file.Content);
    }

    /// <summary>A row mixing a file with other fields renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MixedFileRowSkipsWithReason()
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
            - name: photos
              meaning: The condition photos with titles the retirement files the copy under.
              type: list
              rowType: ConditionPhoto
              rowShape:
                fields:
                - name: title
                  meaning: The photo title.
                  type: text
                  required: true
                - name: photo
                  meaning: One condition photo.
                  type: file
                  contentTypes:
                  - image/png
                  maxSize: 5 MB
                  required: true
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
        Assert.Contains("is a file beside other fields", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A row holding several files renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SeveralFilesInOneRowSkipWithReason()
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
            - name: photos
              meaning: The condition photo pair the retirement files the copy under.
              type: list
              rowType: ConditionPhotoPair
              rowShape:
                fields:
                - name: front
                  meaning: The front photo.
                  type: file
                  contentTypes:
                  - image/png
                  maxSize: 5 MB
                  required: true
                - name: back
                  meaning: The back photo.
                  type: file
                  contentTypes:
                  - image/png
                  maxSize: 5 MB
                  required: true
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
        Assert.Contains("is a file beside other fields", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A paged query binds its cursor and limit from the query, applies the default limit, and refuses a limit past the maximum.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PagedQueryBindsCursorAndLimitFromQuery()
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
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("[FromQuery(Name = \"cursor\")] string? cursor = null", file.Content, StringComparison.Ordinal);
        Assert.Contains("[FromQuery(Name = \"limit\")] int? limit = null", file.Content, StringComparison.Ordinal);
        Assert.Contains("limit ?? 25", file.Content, StringComparison.Ordinal);
        Assert.Contains("effectiveLimit > 100", file.Content, StringComparison.Ordinal);
        Assert.Contains("Cursor: cursor", file.Content, StringComparison.Ordinal);
        Assert.Contains("Limit: effectiveLimit", file.Content, StringComparison.Ordinal);
    }

    /// <summary>Every operation id is the camelCase use case name.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OperationIdUsesCamelCaseUseCaseName()
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
        var file = Assert.Single(plan.Files);
        Assert.Contains(".WithName(\"borrowCopy\")", file.Content, StringComparison.Ordinal);
        Assert.Contains(".WithName(\"listLoans\")", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain(".WithName(\"BorrowCopy\")", file.Content, StringComparison.Ordinal);
    }

    /// <summary>The summary carries the record title and the description carries the purpose sentence.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SummaryCarriesTitleAndDescriptionCarriesPurpose()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "spending",
            [CreatedEndpoint()],
            "Claimwick.Spending.WebApi",
            "Claimwick.Platform.Http.ActorClaims.ReadActorId",
            ["Claimwick.Spending.Contracts.Expenses"]);

        Assert.Contains(".WithSummary(\"Submit Expense\")", text, StringComparison.Ordinal);
        Assert.Contains(".WithDescription(\"An employee files a work expense so it can be decided and reimbursed.\")", text, StringComparison.Ordinal);
    }

    /// <summary>Problem responses derive from the record: 400 for caller inputs, 401 and 403 for the grant, 404 for a route aggregate id, and each refusal status.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ProblemResponsesDeriveFromInputsAccessRouteAndRefusals()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.renew-loan",
                "RenewLoan",
                "Renew loan",
                null,
                "command",
                "POST",
                "/api/loans/{loanId}:renew",
                null,
                [
                    new EndpointsRenderer.BoundInput(
                        "loan", "LoanId", "loanId", "loanId", "id", "route", true,
                        "The loan the reader renews.", null, "LoanId", null, "caller"),
                    new EndpointsRenderer.BoundInput(
                        "note", "Note", "note", "note", "text", "query", false,
                        "Why the reader renews.", null, null, null, "caller"),
                ],
                new Dictionary<string, string>(),
                false,
                [409],
                "RenewLoanCommand",
                "Library.Lending.Application.Loans.RenewLoan",
                "RenewLoanCommandResult",
                "RenewLoanResponseModel",
                "Library.Lending.WebApi.Loans.RenewLoan",
                MarkStyle.Xml,
                null,
                null,
                true)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains(".ProducesProblem(StatusCodes.Status400BadRequest)", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status401Unauthorized)", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status403Forbidden)", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status404NotFound)", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status409Conflict)", text, StringComparison.Ordinal);
        Assert.Contains("Results<NoContent, ProblemHttpResult>", text, StringComparison.Ordinal);
    }

    /// <summary>A use case open to anonymous callers lists no authentication problems.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AnonymousUseCaseOmitsAuthenticationProblems()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.search-catalog",
                "SearchCatalog",
                "Search catalog",
                null,
                "query",
                "GET",
                "/api/catalog",
                null,
                [new EndpointsRenderer.BoundInput(
                    "term", "Term", "term", "term", "text", "query", false,
                    "The words to search for.", null, null, null, "caller")],
                new Dictionary<string, string>(),
                true,
                [],
                "SearchCatalogQuery",
                "Library.Lending.Application.Catalog.SearchCatalog",
                "SearchCatalogQueryResult",
                "SearchCatalogResponseModel",
                "Library.Lending.WebApi.Catalog.SearchCatalog",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains(".ProducesProblem(StatusCodes.Status400BadRequest)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Status401Unauthorized", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Status403Forbidden", text, StringComparison.Ordinal);
    }

    /// <summary>A PATCH endpoint with a body declares the merge-patch media type it reads.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PatchEndpointDeclaresMergePatchAccepts()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.change-loan-note",
                "ChangeLoanNote",
                "Change loan note",
                null,
                "command",
                "PATCH",
                "/api/loans/{loanId}/note",
                null,
                [
                    new EndpointsRenderer.BoundInput(
                        "loan", "LoanId", "loanId", "loanId", "id", "route", true,
                        "The loan whose note changes.", null, "LoanId", null, "caller"),
                    new EndpointsRenderer.BoundInput(
                        "note", "Note", "note", "note", "text", "body", false,
                        "The new note.", null, null, null, "caller"),
                ],
                new Dictionary<string, string>(),
                false,
                [],
                "ChangeLoanNoteCommand",
                "Library.Lending.Application.Loans.ChangeLoanNote",
                "ChangeLoanNoteCommandResult",
                "ChangeLoanNoteResponseModel",
                "Library.Lending.WebApi.Loans.ChangeLoanNote",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("MapPatch(\"/api/loans/{loanId}/note\", HandleAsync)", text, StringComparison.Ordinal);
        Assert.Contains(".Accepts<ChangeLoanNoteRequest>(\"application/merge-patch+json\")", text, StringComparison.Ordinal);
    }

    /// <summary>A money input taking its currency from an optional sibling binds the amount as a decimal the handler joins.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void MoneyWithOptionalSiblingCurrencyBindsDecimal()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.AppendAllText(retire, "\n" + """
            input:
            - name: fee
              meaning: The processing fee of the retirement, in the stated currency or the copy currency.
              type: money
              currencyField: feeCurrency
              required: true
              origin: caller
              binding: body
              classification: internal
            - name: feeCurrency
              meaning: The ISO 4217 currency code of the fee, the copy currency when absent.
              type: text
              maxLength: 3
              required: false
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files, candidate => candidate.AbsolutePath.EndsWith("LendingEndpoints.generated.cs", StringComparison.Ordinal));
        Assert.Contains("decimal Fee", file.Content, StringComparison.Ordinal);
        Assert.Contains("string? FeeCurrency", file.Content, StringComparison.Ordinal);
        Assert.Contains("Fee: request.Fee", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Money.Create", file.Content, StringComparison.Ordinal);
        var message = Assert.Single(plan.Files, candidate => candidate.AbsolutePath.EndsWith("RetireCopyCommand.generated.cs", StringComparison.Ordinal));
        Assert.Contains("decimal Fee", message.Content, StringComparison.Ordinal);
    }

    /// <summary>An explicit tenant input keeps the message member name in the endpoint named arguments.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ExplicitTenantInputKeepsMessageMemberNames()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteTenancy(temp.Path);
        var folder = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "workspaces");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "aggregate.yaml"),
            """
            type: aggregate
            id: aggregate/lending.workspace
            title: Workspace
            states:
            - name: Active
              meaning: The workspace lends.
            """);
        var path = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace(
                "- name: copyId",
                "- name: workspace\n  meaning: The workspace the loan belongs to.\n  type: id\n  identifies: aggregate/lending.workspace\n  required: true\n  origin: caller\n  binding: body\n  classification: internal\n- name: copyId",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.borrow-copy");
        var file = Assert.Single(plan.Files, candidate => candidate.AbsolutePath.EndsWith("LendingEndpoints.generated.cs", StringComparison.Ordinal));
        Assert.Contains("if (!WorkspaceId.TryParse(request.Workspace.ToString(), null, out var workspace))", file.Content, StringComparison.Ordinal);
        Assert.Contains("Workspace: workspace", file.Content, StringComparison.Ordinal);
        var message = Assert.Single(plan.Files, candidate => candidate.AbsolutePath.EndsWith("BorrowCopyCommand.generated.cs", StringComparison.Ordinal));
        Assert.Contains("WorkspaceId Workspace", message.Content, StringComparison.Ordinal);
    }

    /// <summary>Endpoints import each message namespace as the messages targets place it, choosing the internal or public target by use case visibility, never deriving it from the endpoints namespace.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EndpointsImportMessageNamespacesFromMessagesTargets()
    {
        using var temp = ModelFixture.Prepare("small");
        var list = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "list-loans", "list-loans.yaml");
        File.WriteAllText(
            list,
            File.ReadAllText(list).Replace(
                "reachedThrough:\n- entry-point/library-app",
                "reachedThrough:\n- entry-point/library-app\n- module/catalog",
                StringComparison.Ordinal));
        AppendGenerate(temp.Path, """
            generate:
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            - kind: messages
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Contracts.{Aggregate}.{UseCase}
              visibility: public
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Application/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Application.{Aggregate}.{UseCase}
            - kind: results
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}.Contracts/{Aggregate}/{UseCase}
              namespace: Library.{Module}.Contracts.{Aggregate}.{UseCase}
              visibility: public
            - kind: endpoints
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}
              namespace: Library.{Module}.WebApi
              claimReader: Library.Lending.WebApi.ActorClaims.ReadActorId
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        var file = Assert.Single(plan.Files, candidate => candidate.AbsolutePath.EndsWith("LendingEndpoints.generated.cs", StringComparison.Ordinal));
        Assert.Contains("namespace Library.Lending.WebApi;", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Library.Lending.WebApi.WebApi", file.Content, StringComparison.Ordinal);
        Assert.Contains("using Library.Lending.Application.Loans.BorrowCopy;", file.Content, StringComparison.Ordinal);
        Assert.Contains("using Library.Lending.Contracts.Loans.ListLoans;", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Library.Lending.WebApi.Application", file.Content, StringComparison.Ordinal);
        Assert.Contains("ShapeListLoansResponse(ListLoansResult result)", file.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("ListLoansQueryResult", file.Content, StringComparison.Ordinal);
    }

    /// <summary>Each claim identity reads through the generic claim reader with its member type as the type argument, so one module serves several actor kinds.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ClaimReaderRendersMemberTypeAsTypeArgument()
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
                        "reader", "ReaderId", "reader", "reader", "id", "claim", true,
                        "The signed-in reader borrowing the copy.", null, "ReaderId", "reader", "verified-claim"),
                    new EndpointsRenderer.BoundInput(
                        "workspace", "WorkspaceId", "workspace", "workspace", "id", "claim", true,
                        "The workspace the loan belongs to.", null, "WorkspaceId", "workspace", "verified-claim"),
                    new EndpointsRenderer.BoundInput(
                        "holder", "HolderId", "holder", "holder", "id", "claim", false,
                        "The signed-in holder, read from verified claims.", null, null, "holder", "verified-claim"),
                ],
                new Dictionary<string, string>(),
                false,
                [],
                "BorrowCopyCommand",
                "Library.Lending.Application.Loans.BorrowCopy",
                "BorrowCopyCommandResult",
                "BorrowCopyResponseModel",
                "Library.Lending.WebApi.Loans.BorrowCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("ReaderId: ActorClaims.ReadActorId<ReaderId>(user, \"reader\")", text, StringComparison.Ordinal);
        Assert.Contains("WorkspaceId: ActorClaims.ReadActorId<WorkspaceId>(user, \"workspace\")", text, StringComparison.Ordinal);
        Assert.Contains("HolderId: ActorClaims.ReadActorId<Guid>(user, \"holder\")", text, StringComparison.Ordinal);
    }

    /// <summary>A list reusing an aggregate part binds from the body as request rows mapped to the part type when the part members resolve from the backend source.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void PartRowListBindsWhenPartResolves()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteRenewalPart(temp.Path);
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
            - name: renewals
              meaning: The renewals the retirement files the copy under.
              type: list
              rowType: aggregate/lending.loan#renewal
              rowShape:
                fields:
                - name: renewedAt
                  meaning: The renewal time.
                  type: instant
                  required: true
                - name: note
                  meaning: The renewal note.
                  type: text
                  required: false
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        var file = Assert.Single(plan.Files);
        Assert.Contains("internal sealed partial record RetireCopyRenewalsRow(DateTimeOffset RenewedAt, string? Note);", file.Content, StringComparison.Ordinal);
        Assert.Contains("var renewalsRows = new List<Library.Lending.Domain.Loans.Parts.Renewal>(request.Renewals.Length);", file.Content, StringComparison.Ordinal);
        Assert.Contains("renewalsRows.Add(new Library.Lending.Domain.Loans.Parts.Renewal(", file.Content, StringComparison.Ordinal);
        Assert.Contains("RenewedAt: row.RenewedAt,", file.Content, StringComparison.Ordinal);
        Assert.Contains("Note: row.Note", file.Content, StringComparison.Ordinal);
        Assert.Contains("Renewals: renewalsRows", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A command stating an output answers 200 with the shaped response, the same shaping queries use.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void CommandWithOutputAnswersOk()
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
            output:
            - name: copyId
              meaning: The retired copy.
              type: id
              identifies: aggregate/lending.copy
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains(".Produces<RetireCopyResponseModel>(StatusCodes.Status200OK)", file.Content, StringComparison.Ordinal);
        Assert.Contains("return TypedResults.Ok(ShapeRetireCopyResponse(result));", file.Content, StringComparison.Ordinal);
        Assert.Contains("private static partial RetireCopyResponseModel ShapeRetireCopyResponse(RetireCopyCommandResult result);", file.Content, StringComparison.Ordinal);
    }

    /// <summary>A query with one optional output group answers 404 naming the details when the result carries no group.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AbsentGroupAnswersNotFound()
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
        WriteReadLoan(temp.Path);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("if (result.Loan is null)", file.Content, StringComparison.Ordinal);
        Assert.Contains("title: \"Unknown loan.\"", file.Content, StringComparison.Ordinal);
        Assert.Contains("detail: $\"No loan answers '{loanId}'.\"", file.Content, StringComparison.Ordinal);
        Assert.Contains("statusCode: StatusCodes.Status404NotFound);", file.Content, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status404NotFound)", file.Content, StringComparison.Ordinal);
    }

    /// <summary>An input stating equals for the tenant key answers 403 before the message runs when the route value is not the caller's key.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EqualsTenantAnswersForbidden()
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
        var retire = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "copies", "use-cases", "retire-copy", "retire-copy.yaml");
        File.WriteAllText(
            retire,
            File.ReadAllText(retire).Replace(
                "route: /api/copies:retire",
                "route: /api/{workspaceId}/copies:retire",
                StringComparison.Ordinal));
        File.AppendAllText(retire, "\n" + """
            input:
            - name: workspace
              meaning: The workspace the copy retires in.
              type: id
              required: true
              origin: caller
              binding: route
              equals: tenant
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.DoesNotContain(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        var file = Assert.Single(plan.Files);
        Assert.Contains("if (!Equals(workspaceId, TenantClaims.ReadTenantId<Guid>(user)))", file.Content, StringComparison.Ordinal);
        Assert.Contains("title: \"Forbidden workspace.\"", file.Content, StringComparison.Ordinal);
        Assert.Contains("The workspace '{workspaceId}' is not the caller's workspace.", file.Content, StringComparison.Ordinal);
        Assert.Contains("statusCode: StatusCodes.Status403Forbidden);", file.Content, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status403Forbidden)", file.Content, StringComparison.Ordinal);
    }

    /// <summary>An input stating equals for a sibling input answers 400 before the message runs when the two values disagree.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EqualsSiblingAnswersBadRequest()
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
        File.WriteAllText(
            retire,
            File.ReadAllText(retire).Replace(
                "route: /api/copies:retire",
                "route: /api/loans/{loanId}/copies:retire",
                StringComparison.Ordinal));
        File.AppendAllText(retire, "\n" + """
            input:
            - name: loanId
              meaning: The loan the copy retires under.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              binding: route
              equals: loanRef
              classification: internal
            - name: loanRef
              meaning: The loan the request names.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              binding: body
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Skips);
        var file = Assert.Single(plan.Files);
        Assert.Contains("if (!LoanId.TryParse(request.LoanRef.ToString(), null, out var loanRef))", file.Content, StringComparison.Ordinal);
        Assert.Contains("if (!Equals(loanId, loanRef))", file.Content, StringComparison.Ordinal);
        Assert.Contains("title: \"Mismatched loan id.\"", file.Content, StringComparison.Ordinal);
        Assert.Contains("The loan id '{loanId}' does not match the loan ref '{request.LoanRef}'.", file.Content, StringComparison.Ordinal);
        Assert.Contains("statusCode: StatusCodes.Status400BadRequest);", file.Content, StringComparison.Ordinal);
    }

    /// <summary>An input stating equals for no input renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EqualsUnknownInputSkipsWithReason()
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
            - name: loanId
              meaning: The loan the copy retires under.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              binding: route
              equals: no-such-input
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        Assert.Contains("names no input", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>An input stating equals for the tenant key with no implied tenant key renders no endpoint and lists the skip with its reason.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void EqualsTenantWithoutImpliedKeySkipsWithReason()
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
            - name: workspace
              meaning: The workspace the copy retires in.
              type: id
              identifies: aggregate/lending.workspace
              required: true
              origin: caller
              binding: route
              equals: tenant
              classification: internal
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        var skip = Assert.Single(plan.Skips, skip => skip.UseCaseId == "use-case/lending.retire-copy");
        Assert.Contains("no implied tenant key", skip.Reason, StringComparison.Ordinal);
    }

    /// <summary>A body id answers 400 naming the member for a value the identifier refuses, before the message runs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RequiredBodyIdGuardsInvalidValue()
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
                [new EndpointsRenderer.BoundInput(
                    "copyId", "CopyId", "copyId", "copyId", "id", "body", true,
                    "The copy the reader borrows.", null, "CopyId", null, "caller")],
                new Dictionary<string, string>(),
                false,
                [],
                "BorrowCopyCommand",
                "Library.Lending.Application.Loans.BorrowCopy",
                "BorrowCopyCommandResult",
                "BorrowCopyResponseModel",
                "Library.Lending.WebApi.Loans.BorrowCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("if (!CopyId.TryParse(request.CopyId.ToString(), null, out var copyId))", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Invalid copy id.\"", text, StringComparison.Ordinal);
        Assert.Contains("The copy id '{request.CopyId}' is not a valid copy id.", text, StringComparison.Ordinal);
        Assert.Contains("CopyId: copyId", text, StringComparison.Ordinal);
        Assert.Contains(".ProducesProblem(StatusCodes.Status400BadRequest)", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".From(", text, StringComparison.Ordinal);
    }

    /// <summary>A required body list answers 400 naming the member when the request carries an explicit null.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RequiredListNullAnswersBadRequest()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.retire-copy",
                "RetireCopy",
                "Retire copy",
                null,
                "command",
                "POST",
                "/api/copies:retire",
                null,
                [new EndpointsRenderer.BoundInput(
                    "splits", "Splits", "splits", "splits", "list", "body", true,
                    "The cost splits of the retirement, one row per cost center.", null, null, null, "caller",
                    RowType: "RetireSplit",
                    Rows:
                    [
                        new EndpointsRenderer.RowField(
                            "costCenter", "CostCenter", "text", true,
                            "The cost center carrying the split.", null, null, null, null, null),
                        new EndpointsRenderer.RowField(
                            "share", "Share", "decimal", true,
                            "The share of the cost the split carries.", null, null, null, null, null),
                    ])],
                new Dictionary<string, string>(),
                false,
                [],
                "RetireCopyCommand",
                "Library.Lending.Application.Copies.RetireCopy",
                "RetireCopyCommandResult",
                "RetireCopyResponseModel",
                "Library.Lending.WebApi.Copies.RetireCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("if (request.Splits is null)", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Missing splits.\"", text, StringComparison.Ordinal);
        Assert.Contains("detail: \"The splits list is required.\"", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("var splitsRows = new List<RetireSplit>(request.Splits.Length);", text, StringComparison.Ordinal);
    }

    /// <summary>A required single object answers 400 naming the member when the request carries an explicit null.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void RequiredObjectNullAnswersBadRequest()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.retire-copy",
                "RetireCopy",
                "Retire copy",
                null,
                "command",
                "POST",
                "/api/copies:retire",
                null,
                [new EndpointsRenderer.BoundInput(
                    "shelfMark", "ShelfMark", "shelfMark", "shelfMark", "object", "body", true,
                    "The shelf mark the retirement files the copy under.", null, null, null, "caller",
                    RowType: "ShelfMark",
                    Rows:
                    [
                        new EndpointsRenderer.RowField(
                            "shelf", "Shelf", "text", true,
                            "The shelf.", null, null, null, null, null),
                        new EndpointsRenderer.RowField(
                            "slot", "Slot", "integer", true,
                            "The slot on the shelf.", null, null, null, null, null),
                    ])],
                new Dictionary<string, string>(),
                false,
                [],
                "RetireCopyCommand",
                "Library.Lending.Application.Copies.RetireCopy",
                "RetireCopyCommandResult",
                "RetireCopyResponseModel",
                "Library.Lending.WebApi.Copies.RetireCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("if (request.ShelfMark is null)", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Missing shelf mark.\"", text, StringComparison.Ordinal);
        Assert.Contains("detail: \"The shelf mark is required.\"", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Each value of a many-valued body id answers 400 naming the member when the identifier refuses it, and a null array answers 400 as a missing list.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ManyBodyIdGuardsEachValue()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.retire-copy",
                "RetireCopy",
                "Retire copy",
                null,
                "command",
                "POST",
                "/api/copies:retire",
                null,
                [new EndpointsRenderer.BoundInput(
                    "copies", "Copies", "copies", "copies", "id", "body", true,
                    "The copies the retirement files.", null, "CopyId", null, "caller", Many: true)],
                new Dictionary<string, string>(),
                false,
                [],
                "RetireCopyCommand",
                "Library.Lending.Application.Copies.RetireCopy",
                "RetireCopyCommandResult",
                "RetireCopyResponseModel",
                "Library.Lending.WebApi.Copies.RetireCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("if (request.Copies is null)", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Missing copies.\"", text, StringComparison.Ordinal);
        Assert.Contains("var copiesItems = new List<CopyId>(request.Copies.Length);", text, StringComparison.Ordinal);
        Assert.Contains("foreach (var copiesValue in request.Copies)", text, StringComparison.Ordinal);
        Assert.Contains("if (!CopyId.TryParse(copiesValue.ToString(), null, out var copiesMember))", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Invalid copies.\"", text, StringComparison.Ordinal);
        Assert.Contains("copiesItems.Add(copiesMember);", text, StringComparison.Ordinal);
        Assert.Contains("Copies: copiesItems", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".From(", text, StringComparison.Ordinal);
    }

    /// <summary>Each value of a single-field id list answers 400 naming the row field when the identifier refuses it.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void SingleFieldIdListGuardsEachValue()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.retire-copy",
                "RetireCopy",
                "Retire copy",
                null,
                "command",
                "POST",
                "/api/copies:retire",
                null,
                [new EndpointsRenderer.BoundInput(
                    "holds", "Holds", "holds", "holds", "list", "body", true,
                    "The holds the retirement releases, one per hold.", null, null, null, "caller",
                    Rows:
                    [
                        new EndpointsRenderer.RowField(
                            "hold", "Hold", "id", true,
                            "One released hold.", null, "HoldId", null, null, null),
                    ])],
                new Dictionary<string, string>(),
                false,
                [],
                "RetireCopyCommand",
                "Library.Lending.Application.Copies.RetireCopy",
                "RetireCopyCommandResult",
                "RetireCopyResponseModel",
                "Library.Lending.WebApi.Copies.RetireCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("Guid[] Holds", text, StringComparison.Ordinal);
        Assert.Contains("if (request.Holds is null)", text, StringComparison.Ordinal);
        Assert.Contains("var holdsItems = new List<HoldId>(request.Holds.Length);", text, StringComparison.Ordinal);
        Assert.Contains("if (!HoldId.TryParse(holdsValue.ToString(), null, out var holdsMember))", text, StringComparison.Ordinal);
        Assert.Contains("title: \"Invalid hold.\"", text, StringComparison.Ordinal);
        Assert.Contains("Holds: holdsItems", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".From(", text, StringComparison.Ordinal);
    }

    /// <summary>An optional row identifier maps an absent value to null and a carried one through its non-throwing parse.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void OptionalRowIdGuardsWhenCarried()
    {
        var text = EndpointsRenderer.RenderDotnet(
            "lending",
            [new EndpointsRenderer.EndpointData(
                "use-case/lending.retire-copy",
                "RetireCopy",
                "Retire copy",
                null,
                "command",
                "POST",
                "/api/copies:retire",
                null,
                [new EndpointsRenderer.BoundInput(
                    "fines", "Fines", "fines", "fines", "list", "body", true,
                    "The fines cleared by the retirement, one row per fine.", null, null, null, "caller",
                    RowType: "ClearedFine",
                    Rows:
                    [
                        new EndpointsRenderer.RowField(
                            "copy", "Copy", "id", false,
                            "The copy the fine was charged on.", null, "CopyId", null, null, null),
                        new EndpointsRenderer.RowField(
                            "waived", "Waived", "boolean", true,
                            "Whether the retirement waives the fine.", null, null, null, null, null),
                    ])],
                new Dictionary<string, string>(),
                false,
                [],
                "RetireCopyCommand",
                "Library.Lending.Application.Copies.RetireCopy",
                "RetireCopyCommandResult",
                "RetireCopyResponseModel",
                "Library.Lending.WebApi.Copies.RetireCopy",
                MarkStyle.Xml)],
            "Library.Lending.WebApi",
            "Library.Lending.WebApi.ActorClaims.ReadActorId",
            []);

        Assert.Contains("CopyId? finesCopy = null;", text, StringComparison.Ordinal);
        Assert.Contains("if (row.Copy is not null)", text, StringComparison.Ordinal);
        Assert.Contains("if (!CopyId.TryParse(row.Copy.Value.ToString(), null, out var finesCopyParsed))", text, StringComparison.Ordinal);
        Assert.Contains("Copy: finesCopy,", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".From(", text, StringComparison.Ordinal);
    }

    /// <summary>Writes the read-loan query with one optional output group, for the absent-details test.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteReadLoan(string root)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "read-loan");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "read-loan.yaml"),
            """
            type: use-case
            id: use-case/lending.read-loan
            title: Read loan
            kind: query
            access:
              grant: permission/read-loans
              tenant: own
            reachedThrough:
            - entry-point/library-app
            http:
              method: GET
              route: /api/loans/{loanId}
            input:
            - name: loanId
              meaning: The loan to read.
              type: id
              identifies: aggregate/lending.loan
              required: true
              origin: caller
              binding: route
              classification: internal
            output:
              include:
                loan:
                  fields:
                  - name: loanId
                    meaning: The read loan.
                    type: id
                    identifies: aggregate/lending.loan
                  - name: dueDate
                    meaning: The day the loan is due.
                    type: date
                  classification: internal
                  reads:
                  - aggregate/lending.loan
                  freshness:
                    mode: immediate
                  required: false
            """);
    }

    /// <summary>Declares the renewal part on the loan aggregate and writes its declaring type with the renewal time and note, so the part members resolve.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void WriteRenewalPart(string root)
    {
        File.AppendAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"),
            "\nparts:\n- name: renewal\n  identity: false\n");
        var parts = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Domain", "Loans", "Parts");
        Directory.CreateDirectory(parts);
        File.WriteAllText(
            Path.Combine(parts, "Renewal.cs"),
            """
            namespace Library.Lending.Domain.Loans.Parts;

            internal sealed record Renewal(DateTimeOffset RenewedAt, string? Note);
            """);
    }

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
