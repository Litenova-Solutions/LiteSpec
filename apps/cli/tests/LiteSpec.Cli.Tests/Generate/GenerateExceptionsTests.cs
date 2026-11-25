using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the exceptions generator writes one refusal exception per aggregate-declared refusal.</summary>
public sealed class GenerateExceptionsTests
{
    /// <summary>Each aggregate refusal renders its golden exception file in its aggregate folder.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void ExceptionsDotnetMatchesGoldens()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Exceptions
              namespace: Library.{Module}.Domain.{Aggregate}.Exceptions
              baseType: Library.Kernel.Domain.DomainException
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Empty(plan.ConfigErrors);
        Assert.Equal(
            [
                "apps/api/src/Modules/Library.Lending/Domain/Copies/Exceptions/CopyCopyAlreadyRegisteredException.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Copies/Exceptions/CopyCopyOnLoanException.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Holds/Exceptions/HoldHoldAlreadyWaitingException.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Holds/Exceptions/HoldHoldNotWaitingException.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Loans/Exceptions/LoanCopyNotAvailableException.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Loans/Exceptions/LoanExtensionRefusedException.generated.cs",
                "apps/api/src/Modules/Library.Lending/Domain/Loans/Exceptions/LoanLoanNotActiveException.generated.cs",
            ],
            plan.Files
                .Select(file => Path.GetRelativePath(temp.Path, file.AbsolutePath).Replace('\\', '/'))
                .Order(StringComparer.Ordinal));

        AssertGolden(plan, "CopyCopyAlreadyRegisteredException.generated.cs", "exceptions-dotnet-copies-copy-already-registered.txt");
        AssertGolden(plan, "CopyCopyOnLoanException.generated.cs", "exceptions-dotnet-copies-copy-on-loan.txt");
        AssertGolden(plan, "HoldHoldAlreadyWaitingException.generated.cs", "exceptions-dotnet-holds-hold-already-waiting.txt");
        AssertGolden(plan, "HoldHoldNotWaitingException.generated.cs", "exceptions-dotnet-holds-hold-not-waiting.txt");
        AssertGolden(plan, "LoanCopyNotAvailableException.generated.cs", "exceptions-dotnet-loans-copy-not-available.txt");
        AssertGolden(plan, "LoanExtensionRefusedException.generated.cs", "exceptions-dotnet-loans-extension-refused.txt");
        AssertGolden(plan, "LoanLoanNotActiveException.generated.cs", "exceptions-dotnet-loans-loan-not-active.txt");
    }

    /// <summary>A refusal with parameters renders a constructor taking each value and interpolating the message.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    /// <proves>litespec/core/model#prove-every-generator-with-golden-tests</proves>
    [Fact]
    public void ParameterisedRefusalMatchesGolden()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Exceptions
              namespace: Library.{Module}.Domain.{Aggregate}.Exceptions
              baseType: Library.Kernel.Domain.DomainException
            """);
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "refusals.yaml"),
            """
            type: refusal-set
            summary: The refusals the loan aggregate raises.
            records:
            - id: refusal/lending.copy-not-available
              title: Copy Not Available
              meaning: The copy is on loan or on hold for another reader.
            - id: refusal/lending.extension-refused
              title: Extension Refused
              meaning: The loan is not due within the {window} extension window.
              parameters:
              - name: window
                meaning: The extension window in days.
            - id: refusal/lending.loan-not-active
              title: Loan Not Active
              meaning: The loan is already returned.
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        AssertGolden(plan, "LoanExtensionRefusedException.generated.cs", "exceptions-dotnet-loans-extension-refused-parameterised.txt");
    }

    /// <summary>A module-level refusal renders no exception file, while aggregate refusals still render theirs.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ModuleLevelRefusalRendersNoFile()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Exceptions
              namespace: Library.{Module}.Domain.{Aggregate}.Exceptions
              baseType: Library.Kernel.Domain.DomainException
            """);
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "refusals.yaml"),
            """
            type: refusal-set
            summary: The refusals no aggregate raises.
            records:
            - id: refusal/lending.import-rejected
              title: Import Rejected
              meaning: The import file holds no readable rows.
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Errors);
        Assert.Equal(7, plan.Files.Count);
        Assert.DoesNotContain(plan.Files, file => file.AbsolutePath.Contains("ImportRejected", StringComparison.Ordinal));
    }

    /// <summary>An exceptions target without a base type reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ExceptionsTargetWithoutBaseTypeReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Exceptions
              namespace: Library.{Module}.Domain.{Aggregate}.Exceptions
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("baseType", StringComparison.Ordinal));
    }

    /// <summary>An exceptions target without an aggregate placeholder in its path reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ExceptionsTargetWithoutAggregatePathReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/Exceptions
              namespace: Library.{Module}.Domain.{Aggregate}.Exceptions
              baseType: Library.Kernel.Domain.DomainException
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("{Aggregate}", StringComparison.Ordinal));
    }

    /// <summary>An exceptions target without an aggregate placeholder in its namespace reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ExceptionsTargetWithoutAggregateNamespaceReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Exceptions
              namespace: Library.{Module}.Domain.Exceptions
              baseType: Library.Kernel.Domain.DomainException
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains("{Aggregate}", StringComparison.Ordinal));
    }

    /// <summary>An exceptions target for a TypeScript stack reports and renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void ExceptionsTargetForTypeScriptReports()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: svelte
              path: apps/web/src/generated
              baseType: DomainException
            """);

        var plan = GeneratePlan.Build(ModelBuilder.Build(ConsumerConfig.Load(temp.Path)));

        Assert.Empty(plan.Files);
        Assert.Contains(plan.Errors, error => error.Contains(".NET", StringComparison.Ordinal));
    }

    /// <summary>A placeholder the parameters list does not declare is invalid, and the refusal renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UndeclaredPlaceholderIsInvalid()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLoansRefusals(temp.Path, """
            - id: refusal/lending.extension-refused
              title: Extension Refused
              meaning: The loan is not due within the {window} extension window.
            """);

        var problems = new List<Finding>();
        var entries = RefusalSetReader.Read(ConsumerConfig.Load(temp.Path), problems);

        Assert.Contains(problems, problem => problem.Code == "record-invalid" && problem.Message.Contains("{window}", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, entry => entry.Code == "lending.extension-refused");
    }

    /// <summary>A declared parameter the meaning never names is invalid, and the refusal renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void UnnamedParameterIsInvalid()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLoansRefusals(temp.Path, """
            - id: refusal/lending.extension-refused
              title: Extension Refused
              meaning: The loan is not due within the extension window.
              parameters:
              - name: window
                meaning: The extension window in days.
            """);

        var problems = new List<Finding>();
        var entries = RefusalSetReader.Read(ConsumerConfig.Load(temp.Path), problems);

        Assert.Contains(problems, problem => problem.Code == "record-invalid" && problem.Message.Contains("window", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, entry => entry.Code == "lending.extension-refused");
    }

    /// <summary>A parameter declared twice is invalid, and the refusal renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void DuplicateParameterIsInvalid()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLoansRefusals(temp.Path, """
            - id: refusal/lending.extension-refused
              title: Extension Refused
              meaning: The loan is not due within the {window} extension window.
              parameters:
              - name: window
                meaning: The extension window in days.
              - name: window
                meaning: The extension window in days.
            """);

        var problems = new List<Finding>();
        var entries = RefusalSetReader.Read(ConsumerConfig.Load(temp.Path), problems);

        Assert.Contains(problems, problem => problem.Code == "record-invalid" && problem.Message.Contains("twice", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, entry => entry.Code == "lending.extension-refused");
    }

    /// <summary>A parameter name that is no identifier is invalid, and the refusal renders nothing.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void NonIdentifierParameterIsInvalid()
    {
        using var temp = ModelFixture.Prepare("small");
        WriteLoansRefusals(temp.Path, """
            - id: refusal/lending.extension-refused
              title: Extension Refused
              meaning: The loan is not due within the extension window.
              parameters:
              - name: extension window
                meaning: The extension window in days.
            """);

        var problems = new List<Finding>();
        var entries = RefusalSetReader.Read(ConsumerConfig.Load(temp.Path), problems);

        Assert.Contains(problems, problem => problem.Code == "record-invalid" && problem.Message.Contains("no identifier", StringComparison.Ordinal));
        Assert.DoesNotContain(entries, entry => entry.Code == "lending.extension-refused");
    }

    /// <summary>A project with attribute marks renders the rule mark as an attribute.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void AttributeMarksRenderAsAttributes()
    {
        var text = ExceptionsRenderer.RenderDotnet(new ExceptionsRenderer.ExceptionData(
            "lending",
            "loan",
            new RefusalEntry("lending", "lending.loan-not-active", "The loan is already returned.", "refusals.yaml", "loans"),
            "rule/loan.returned-while-active",
            "Library.Kernel.Domain.DomainException",
            "Library.Lending.Domain.Loans.Exceptions",
            MarkStyle.Attribute));

        Assert.Contains("[Implements(\"rule/loan.returned-while-active\")]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("<implements>", text, StringComparison.Ordinal);
    }

    /// <summary>Quotes and literal braces escape in the interpolated message, while placeholders stay single.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void InterpolatedMessageEscapesLiterals()
    {
        var entry = new RefusalEntry("lending", "lending.extension-refused", "Say \"hi\": a {{literal}} {window} note.", "refusals.yaml", "loans")
        {
            Parameters = [new RefusalParameter("window", "The extension window in days.")],
        };
        var text = ExceptionsRenderer.RenderDotnet(new ExceptionsRenderer.ExceptionData(
            "lending", "loan", entry, "rule/loan.extended-before-due",
            "Library.Kernel.Domain.DomainException", "Library.Lending.Domain.Loans.Exceptions", MarkStyle.Xml));

        Assert.Contains(": base($\"Say \\\"hi\\\": a {{literal}} {window} note.\")", text, StringComparison.Ordinal);
        Assert.Contains("<summary>Say \"hi\": a {literal} <paramref name=\"window\"/> note.</summary>", text, StringComparison.Ordinal);
    }

    /// <summary>A simple base type writes no import, and neither does a base type already in scope.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void BaseTypeImportReadsOnlyWhenNeeded()
    {
        var entry = new RefusalEntry("lending", "lending.loan-not-active", "The loan is already returned.", "refusals.yaml", "loans");

        var simple = ExceptionsRenderer.RenderDotnet(new ExceptionsRenderer.ExceptionData(
            "lending", "loan", entry, null, "DomainException",
            "Library.Lending.Domain.Loans.Exceptions", MarkStyle.Xml));

        Assert.DoesNotContain("using ", simple, StringComparison.Ordinal);
        Assert.Contains("class LoanLoanNotActiveException : DomainException", simple, StringComparison.Ordinal);

        var scoped = ExceptionsRenderer.RenderDotnet(new ExceptionsRenderer.ExceptionData(
            "lending", "loan", entry, null, "Library.Lending.Domain.Loans.Exceptions.DomainException",
            "Library.Lending.Domain.Loans.Exceptions", MarkStyle.Xml));

        Assert.DoesNotContain("using ", scoped, StringComparison.Ordinal);
    }

    /// <summary>A refusal with no aggregate record names its exception from the module.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void TypeNameFallsBackToModule()
    {
        Assert.Equal("LoanLoanNotActiveException", ExceptionsRenderer.TypeName("lending", "loan", "lending.loan-not-active"));
        Assert.Equal("LendingLoanNotActiveException", ExceptionsRenderer.TypeName("lending", "", "lending.loan-not-active"));
    }

    /// <summary>Generate writes the exception files, and a second check finds them current.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GenerateWritesExceptionFiles()
    {
        using var temp = ModelFixture.Prepare("small");
        AppendGenerate(temp.Path, """
            generate:
            - kind: exceptions
              stack: dotnet
              path: apps/api/src/Modules/Library.{Module}/Domain/{Aggregate}/Exceptions
              namespace: Library.{Module}.Domain.{Aggregate}.Exceptions
              baseType: Library.Kernel.Domain.DomainException
            """);

        var result = TestCli.Run("generate", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Domain", "Loans", "Exceptions", "LoanLoanNotActiveException.generated.cs");
        Assert.True(File.Exists(file), "exception file");
        Assert.Contains("internal sealed partial class LoanLoanNotActiveException", File.ReadAllText(file), StringComparison.Ordinal);
        Assert.Equal(0, TestCli.Run("generate", "--root", temp.Path, "--check").Status);
    }

    /// <summary>Asserts the one rendered file with the name matches its golden file byte for byte.</summary>
    /// <param name="plan">The rendered plan holding the file.</param>
    /// <param name="fileName">The generated file name.</param>
    /// <param name="golden">The golden file name under <c>Generate/Goldens/</c>.</param>
    static void AssertGolden(GeneratePlan.Plan plan, string fileName, string golden) =>
        GenerateGoldens.AssertEqual(golden, Assert.Single(plan.Files, file => file.AbsolutePath.EndsWith(fileName, StringComparison.Ordinal)).Content);

    /// <summary>Rewrites the loans refusal set with one entry, keeping the set readable.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="entry">The one refusal entry YAML.</param>
    static void WriteLoansRefusals(string root, string entry) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "refusals.yaml"),
            "type: refusal-set\nsummary: The refusals the loan aggregate raises.\nrecords:\n" + entry + "\n");

    /// <summary>Appends a generate block to the fixture project file.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="yaml">The generate block YAML.</param>
    static void AppendGenerate(string root, string yaml) =>
        File.AppendAllText(Path.Combine(root, ".litespec", "project.yaml"), "\n" + yaml + "\n");
}
