using LiteSpec.Analyzers.Rules.Layers.Architecture;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2029, contract types named outside their layer, and LS2030, interfaces in Contracts.</summary>
public sealed class ModuleContractsAnalyzerTests
{
    /// <summary>LS2029 accepts a handler naming another module's public query.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a handler naming another module's public query")]
    public Task AcceptsAHandlerNamingAnotherModulesQuery() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/BorrowBookQueryHandler.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class BorrowBookQueryHandler
                    {
                        internal void Handle(ReadBookQuery query)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookQuery.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a handler naming another module's public result.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a handler naming another module's public result")]
    public Task AcceptsAHandlerNamingAnotherModulesResult() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/BorrowBookQueryHandler.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class BorrowBookQueryHandler
                    {
                        internal void Handle(ReadBookResult result)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookResult.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookResult
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports a Domain type naming another module's public query.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports a Domain type naming another module's public query")]
    public Task ReportsADomainTypeNamingAnotherModulesQuery() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Domain/Loan/Loan.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.Domain.Loan;

                    internal sealed class Loan
                    {
                        internal void Attach({|LS2029:ReadBookQuery|} query)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookQuery.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports a port naming another module's public result.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports a port naming another module's public result")]
    public Task ReportsAPortNamingAnotherModulesResult() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Ports/Books/IBooks.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.Ports.Books;

                    internal interface IBooks
                    {
                        {|LS2029:ReadBookResult|} Find();
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookResult.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookResult
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports an endpoint naming another module's public query.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports an endpoint naming another module's public query")]
    public Task ReportsAnEndpointNamingAnotherModulesQuery() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/WebApi/BorrowBook/BorrowBookEndpoint.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.WebApi.BorrowBook;

                    internal sealed class BorrowBookEndpoint
                    {
                        internal void Map({|LS2029:ReadBookQuery|} query)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookQuery.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports an adapter naming another module's public query.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports an adapter naming another module's public query")]
    public Task ReportsAnAdapterNamingAnotherModulesQuery() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Infrastructure/Books/BooksAdapter.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.Infrastructure.Books;

                    internal sealed class BooksAdapter
                    {
                        internal {|LS2029:ReadBookQuery|} Query() => new();
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookQuery.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a Domain type naming another module's identifier.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a Domain type naming another module's identifier")]
    public Task AcceptsADomainTypeNamingAnotherModulesIdentifier() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Domain/Loan/Loan.cs", """
                    using Claimwick.Books.Contracts.Books;

                    namespace Claimwick.Reading.Domain.Loan;

                    internal sealed class Loan
                    {
                        internal void Attach(BookId book)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/BookId.cs", """
                    namespace Claimwick.Books.Contracts.Books;

                    internal readonly record struct BookId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a handler naming its own module's public query.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a handler naming its own module's public query")]
    public Task AcceptsAHandlerNamingItsOwnModulesQuery() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Books/Application/ReadBook/ReadBookQueryHandler.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Books.Application.ReadBook;

                    internal sealed class ReadBookQueryHandler
                    {
                        internal void Handle(ReadBookQuery query)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookQuery.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a handler naming another module's fact.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a handler naming another module's fact")]
    public Task AcceptsAHandlerNamingAnotherModulesFact() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/BorrowBookQueryHandler.cs", """
                    using Claimwick.Books.Contracts.Books.OnBookLent;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class BorrowBookQueryHandler
                    {
                        internal void On(BookLentModuleEvent lent)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/OnBookLent/BookLentModuleEvent.cs", """
                    namespace Claimwick.Books.Contracts.Books.OnBookLent;

                    internal sealed record BookLentModuleEvent(System.Guid Book);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports a Platform file naming a module identifier.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports a Platform file naming a module identifier")]
    public Task ReportsAPlatformFileNamingAModuleIdentifier() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Platform/Access/ActorScope.cs", """
                    using Claimwick.Books.Contracts.Books;

                    namespace Claimwick.Platform.Access;

                    internal sealed class ActorScope
                    {
                        internal void Attach({|LS2029:BookId|} book)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/BookId.cs", """
                    namespace Claimwick.Books.Contracts.Books;

                    internal readonly record struct BookId(System.Guid Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a Platform file naming a Kernel identifier.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a Platform file naming a Kernel identifier")]
    public Task AcceptsAPlatformFileNamingAKernelIdentifier() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Platform/Access/ActorScope.cs", """
                    using Claimwick.Kernel;

                    namespace Claimwick.Platform.Access;

                    internal sealed class ActorScope
                    {
                        internal void Attach(TenantId tenant)
                        {
                        }
                    }
                    """),
                ("Claimwick.Kernel/TenantId.cs", """
                    namespace Claimwick.Kernel;

                    internal readonly record struct TenantId(string Value);
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a module-level public query named by a handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a module-level public query named by a handler")]
    public Task AcceptsAModuleLevelQueryNamedByAHandler() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/BorrowBookQueryHandler.cs", """
                    using Claimwick.Books.Contracts.SearchCatalog;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class BorrowBookQueryHandler
                    {
                        internal void Handle(SearchCatalogQuery query)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/SearchCatalog/SearchCatalogQuery.cs", """
                    namespace Claimwick.Books.Contracts.SearchCatalog;

                    internal sealed class SearchCatalogQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports a handler naming another module's public command.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports a handler naming another module's public command")]
    public Task ReportsAHandlerNamingAnotherModulesCommand() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/BorrowBookCommandHandler.cs", """
                    using Claimwick.Books.Contracts.Books.LendBook;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class BorrowBookCommandHandler
                    {
                        internal void Handle({|LS2029:LendBookCommand|} command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/LendBook/LendBookCommand.cs", """
                    namespace Claimwick.Books.Contracts.Books.LendBook;

                    internal sealed class LendBookCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a reaction naming another module's public command.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a reaction naming another module's public command")]
    public Task AcceptsAReactionNamingAnotherModulesCommand() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/Stubs.cs", """
                    namespace Claimwick.Reading.Application.BorrowBook;

                    public interface IEventHandler<TEvent>
                    {
                    }

                    public sealed record BookReservedEvent;
                    """),
                ("Claimwick.Modules/Reading/Application/BorrowBook/LendBookOnBookReservedHandler.cs", """
                    using Claimwick.Books.Contracts.Books.LendBook;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class LendBookOnBookReservedHandler : IEventHandler<BookReservedEvent>
                    {
                        internal void Handle(LendBookCommand command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/LendBook/LendBookCommand.cs", """
                    namespace Claimwick.Books.Contracts.Books.LendBook;

                    internal sealed class LendBookCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a job naming another module's public command.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a job naming another module's public command")]
    public Task AcceptsAJobNamingAnotherModulesCommand() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/LendOverdueBooksJob.cs", """
                    using Claimwick.Books.Contracts.Books.LendBook;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class LendOverdueBooksJob
                    {
                        internal void Handle(LendBookCommand command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/LendBook/LendBookCommand.cs", """
                    namespace Claimwick.Books.Contracts.Books.LendBook;

                    internal sealed class LendBookCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 accepts a reaction naming another module's public query.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 accepts a reaction naming another module's public query")]
    public Task AcceptsAReactionNamingAnotherModulesQuery() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/Stubs.cs", """
                    namespace Claimwick.Reading.Application.BorrowBook;

                    public interface IEventHandler<TEvent>
                    {
                    }

                    public sealed record BookReservedEvent;
                    """),
                ("Claimwick.Modules/Reading/Application/BorrowBook/NotifyOnBookReservedHandler.cs", """
                    using Claimwick.Books.Contracts.Books.ReadBook;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class NotifyOnBookReservedHandler : IEventHandler<BookReservedEvent>
                    {
                        internal void Handle(ReadBookQuery query)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/ReadBook/ReadBookQuery.cs", """
                    namespace Claimwick.Books.Contracts.Books.ReadBook;

                    internal sealed class ReadBookQuery
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports an adapter naming another module's public command.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports an adapter naming another module's public command")]
    public Task ReportsAnAdapterNamingAnotherModulesCommand() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Infrastructure/Books/BooksAdapter.cs", """
                    using Claimwick.Books.Contracts.Books.LendBook;

                    namespace Claimwick.Reading.Infrastructure.Books;

                    internal sealed class BooksAdapter
                    {
                        internal {|LS2029:LendBookCommand|} Command() => new();
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/LendBook/LendBookCommand.cs", """
                    namespace Claimwick.Books.Contracts.Books.LendBook;

                    internal sealed class LendBookCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2029 reports a module-level public command named by a handler.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</proves>
    [Fact(DisplayName = "LS2029 reports a module-level public command named by a handler")]
    public Task ReportsAModuleLevelCommandNamedByAHandler() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Claimwick.Modules/Reading/Application/BorrowBook/BorrowBookCommandHandler.cs", """
                    using Claimwick.Books.Contracts.RegisterMember;

                    namespace Claimwick.Reading.Application.BorrowBook;

                    internal sealed class BorrowBookCommandHandler
                    {
                        internal void Handle({|LS2029:RegisterMemberCommand|} command)
                        {
                        }
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/RegisterMember/RegisterMemberCommand.cs", """
                    namespace Claimwick.Books.Contracts.RegisterMember;

                    internal sealed class RegisterMemberCommand
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");

    /// <summary>LS2030 reports an interface declared in a Contracts project and accepts data beside it.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</proves>
    [Fact(DisplayName = "LS2030 reports an interface declared in a Contracts project")]
    public Task ReportsAnInterfaceDeclaredInAContractsProject() =>
        Verify.AnalyzerAsync<ModuleContractsAnalyzer>(
            [
                ("Modules/Claimwick.Books.Contracts/Books/IBookReader.cs", """
                    namespace Claimwick.Books.Contracts.Books;

                    internal interface {|LS2030:IBookReader|}
                    {
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/IBookEvents.cs", """
                    namespace Claimwick.Books.Contracts.Books;

                    internal interface {|LS2030:IBookEvents|}
                    {
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/OnBookLent/IBookLent.cs", """
                    namespace Claimwick.Books.Contracts.Books.OnBookLent;

                    internal interface {|LS2030:IBookLent|}
                    {
                    }
                    """),
                ("Claimwick.Modules/Books/Contracts/Books/OnBookLent/BookLentModuleEvent.cs", """
                    namespace Claimwick.Books.Contracts.Books.OnBookLent;

                    internal sealed record BookLentModuleEvent(System.Guid Book);
                    """),
                ("Claimwick.Modules/Reading/Ports/Books/IBooks.cs", """
                    namespace Claimwick.Reading.Ports.Books;

                    internal interface IBooks
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Modules");
}
