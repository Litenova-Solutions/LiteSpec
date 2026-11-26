using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>Proves the generated validators fit the LiteBus validator API: the kind-specific interface, the failure argument order, and the validity answer.</summary>
public sealed class GenerateValidatorCompileTests
{
    /// <summary>Faithful copies of the LiteBus validator API the generated code compiles against: the same namespaces, type and member names, signatures and constraints, with minimal bodies. Copied from <c>LiteBus.Messaging.Abstractions/Handler/Pre/IMessageValidator[TMessage].cs</c>, <c>ValidationFailure.cs</c> and <c>Validity.cs</c>, <c>LiteBus.Commands.Abstractions/ICommandValidator[TCommand].cs</c> and <c>ICommand.cs</c>, and <c>LiteBus.Queries.Abstractions/IQueryValidator[TQuery].cs</c> and <c>IQuery.cs</c>.</summary>
    internal const string LiteBusStubs = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using LiteBus.Messaging.Abstractions;

        namespace LiteBus.Messaging.Abstractions
        {
            public interface IMessagePreStageHandler;

            public interface IMessageValidator<in TMessage> : IMessagePreStageHandler
                where TMessage : notnull
            {
                Task<Validity> ValidateAsync(TMessage message, CancellationToken cancellationToken = default);
            }

            public readonly struct ValidationFailure
            {
                public ValidationFailure(string message, string? member = null, string? code = null)
                {
                    Message = message;
                    Member = member;
                    Code = code;
                }

                public string Message { get; }
                public string? Member { get; }
                public string? Code { get; }
            }

            public readonly struct Validity
            {
                public static Validity Valid => default;

                public static Validity Invalid(IEnumerable<ValidationFailure> failures)
                {
                    ArgumentNullException.ThrowIfNull(failures);

                    return default;
                }
            }
        }

        namespace LiteBus.Commands.Abstractions
        {
            public interface ICommand;

            public interface ICommandValidator<in TCommand> : IMessageValidator<TCommand>
                where TCommand : ICommand;
        }

        namespace LiteBus.Queries.Abstractions
        {
            public interface IQuery;

            public interface IQueryValidator<in TQuery> : IMessageValidator<TQuery>
                where TQuery : IQuery;
        }
        """;

    /// <summary>The renew-loan command the golden validator judges, with the members its rules read.</summary>
    internal const string RenewLoanMessage = """
        using System;

        namespace Library.Lending.Application.Loans.RenewLoan;

        internal sealed record Money(decimal Amount);

        internal abstract record LoanKind;

        internal sealed record RenewLoanCommand(
            Guid LoanId,
            Guid ReaderId,
            string Note,
            string? RenewalCode,
            int ExtensionDays,
            decimal? Fine,
            Money Deposit,
            decimal Fee,
            string? FeeCurrency,
            LoanKind Kind) : LiteBus.Commands.Abstractions.ICommand;
        """;

    /// <summary>The search-loans query the golden validator judges, with the members its rules read.</summary>
    private const string SearchLoansMessage = """
        using System;

        namespace Library.Lending.Application.Loans.SearchLoans;

        internal sealed record SearchLoansQuery(Guid? Reader, string? Filter, int? Limit) : LiteBus.Queries.Abstractions.IQuery;
        """;

    /// <summary>The running .NET's own assemblies, mirroring the analyzers' Verify helper.</summary>
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>The golden command and query validators compile against the LiteBus validator API.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GeneratedValidatorsCompileAgainstTheLiteBusAbstractions()
    {
        AssertCompiles(
        [
            ("LiteBusStubs.cs", LiteBusStubs),
            ("RenewLoan.cs", RenewLoanMessage),
            ("RenewLoanCommandValidator.generated.cs", GenerateGoldens.Read("validators-dotnet-loans-renew-loan.txt")),
            ("SearchLoans.cs", SearchLoansMessage),
            ("SearchLoansQueryValidator.generated.cs", GenerateGoldens.Read("validators-dotnet-loans-search-loans.txt")),
        ]);
    }

    /// <summary>Each generated validator implements the validator interface of its kind.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GeneratedValidatorsImplementKindSpecificInterfaces()
    {
        var command = GenerateGoldens.Read("validators-dotnet-loans-renew-loan.txt");
        var query = GenerateGoldens.Read("validators-dotnet-loans-search-loans.txt");

        Assert.Contains(" : ICommandValidator<RenewLoanCommand>", command, StringComparison.Ordinal);
        Assert.Contains(" : IQueryValidator<SearchLoansQuery>", query, StringComparison.Ordinal);
        Assert.DoesNotContain("IValidator<", command, StringComparison.Ordinal);
        Assert.DoesNotContain("IValidator<", query, StringComparison.Ordinal);
    }

    /// <summary>Each generated finding carries the message, the member, then the code, as the LiteBus failure constructor takes them.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GeneratedFailuresCarryMessageMemberThenCode()
    {
        var command = GenerateGoldens.Read("validators-dotnet-loans-renew-loan.txt");

        Assert.Matches(new Regex("new ValidationFailure\\(\"[^\"]+\", \"Note\", \"required\"\\)"), command);
        Assert.DoesNotContain("new ValidationFailure(\"Note\", \"required\"", command, StringComparison.Ordinal);
    }

    /// <summary>Each generated validator answers valid for no failures and invalid otherwise, without constructing a validity.</summary>
    /// <proves>litespec/core/model#generate-record-determined-code-from-the-records</proves>
    [Fact]
    public void GeneratedValidatorsAnswerValidForEmptyFailures()
    {
        var command = GenerateGoldens.Read("validators-dotnet-loans-renew-loan.txt");

        Assert.Contains("failures.Count == 0 ? Validity.Valid : Validity.Invalid(failures)", command, StringComparison.Ordinal);
        Assert.DoesNotContain("new Validity(", command, StringComparison.Ordinal);
    }

    /// <summary>Asserts the sources compile together with no errors.</summary>
    /// <param name="files">The source files, each with its path and text.</param>
    static void AssertCompiles(IReadOnlyList<(string Path, string Source)> files)
    {
        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(
                SourceText.From(file.Source, Encoding.UTF8),
                new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
                file.Path))
            .ToList();
        var compilation = CSharpCompilation.Create(
            "Validators",
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, "The generated validator does not compile:\n" + string.Join("\n", errors));
    }

    /// <summary>The running .NET's own assemblies, enough to compile any BCL-only test source.</summary>
    /// <returns>The metadata references of the running .NET assemblies.</returns>
    internal static ImmutableArray<MetadataReference> LoadReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return [.. paths
            .Where(path => Path.GetFileName(path) is var name
                && (name.StartsWith("System.", StringComparison.Ordinal) || name is "System.dll" or "netstandard.dll" or "mscorlib.dll"))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }
}
