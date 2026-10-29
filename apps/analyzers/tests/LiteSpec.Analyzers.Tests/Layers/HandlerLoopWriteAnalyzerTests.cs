using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2015, the loop write rule.</summary>
public sealed class HandlerLoopWriteAnalyzerTests
{
    [Fact(DisplayName = "LS2015 reports a repository store in a foreach body")]
    public Task ReportsAStoreInAForeachBody() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System;
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
                public void Expire(DateTime now) { }
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record ExpireLoansCommand(DateTime Now);

            internal sealed class ExpireLoansCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(ExpireLoansCommand command, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        loan.Expire(command.Now);
                        loanRepository.{|LS2015:Store|}(loan);
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 reports repository writes in a for body")]
    public Task ReportsWritesInAForBody() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
            }

            public interface ILoanRepository
            {
                void Add(Loan loan);
                void Update(Loan loan);
            }

            public sealed record ImportLoansCommand(int Count);

            internal sealed class ImportLoansCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(ImportLoansCommand command, List<Loan> loans)
                {
                    for (var index = 0; index < loans.Count; index++)
                    {
                        loanRepository.{|LS2015:Add|}(loans[index]);
                        loanRepository.{|LS2015:Update|}(loans[index]);
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 reports a repository delete in a while body")]
    public Task ReportsADeleteInAWhileBody() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
            }

            public interface ILoanRepository
            {
                void Delete(Loan loan);
            }

            public sealed record ClearLoansCommand(int Count);

            internal sealed class ClearLoansCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(ClearLoansCommand command, Queue<Loan> loans)
                {
                    while (loans.Count > 0)
                    {
                        loanRepository.{|LS2015:Delete|}(loans.Dequeue());
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 accepts a loop that builds a result list")]
    public Task AcceptsALoopThatBuildsAResultList() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System;
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
                public Guid Id { get; }
            }

            public sealed record ExpireLoansCommand(DateTime Now);

            internal sealed class ExpireLoansCommandHandler
            {
                public List<Guid> Handle(ExpireLoansCommand command, List<Loan> loans)
                {
                    var expired = new List<Guid>();
                    foreach (var loan in loans)
                    {
                        expired.Add(loan.Id);
                    }

                    return expired;
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 accepts a loop that writes through another type")]
    public Task AcceptsALoopThatWritesThroughAnotherType() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
            }

            public interface IObjectCache
            {
                void Store(Loan loan);
            }

            public sealed record WarmLoanCacheCommand(int Count);

            internal sealed class WarmLoanCacheCommandHandler(IObjectCache cache)
            {
                public void Handle(WarmLoanCacheCommand command, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        cache.Store(loan);
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 accepts a repository store outside a loop")]
    public Task AcceptsAStoreOutsideALoop() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Loan
            {
                public void Expire(DateTime now) { }
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record ExpireLoanCommand(DateTime Now);

            internal sealed class ExpireLoanCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(ExpireLoanCommand command, Loan loan)
                {
                    loan.Expire(command.Now);
                    loanRepository.Store(loan);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 accepts a loop with a store in a query handler")]
    public Task AcceptsALoopWithAStoreInAQueryHandler() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record ListLoansQuery(int Page);

            internal sealed class ListLoansQueryHandler(ILoanRepository loanRepository)
            {
                public void Handle(ListLoansQuery query, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        loanRepository.Store(loan);
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 accepts a loop with a store in an event handler")]
    public Task AcceptsALoopWithAStoreInAnEventHandler() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public interface IEventHandler<TEvent> { }

            public sealed class Loan
            {
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record LoansImportedEvent(int Count);

            internal sealed class IndexLoansOnLoansImportedHandler : IEventHandler<LoansImportedEvent>
            {
                private readonly ILoanRepository loanRepository;

                public IndexLoansOnLoansImportedHandler(ILoanRepository loanRepository)
                {
                    this.loanRepository = loanRepository;
                }

                public void Handle(LoansImportedEvent fact, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        loanRepository.Store(loan);
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 accepts a write then return search loop")]
    public Task AcceptsAWriteThenReturnSearchLoop() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
                public bool HasWork { get; }
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record AdvanceCancellationCommand(int Count);

            internal sealed class AdvanceCancellationCommandHandler(ILoanRepository loanRepository)
            {
                public bool Handle(AdvanceCancellationCommand command, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        if (loan.HasWork)
                        {
                            loanRepository.Store(loan);
                            return true;
                        }
                    }

                    return false;
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 reports a write followed by a conditional return")]
    public Task ReportsAWriteFollowedByAConditionalReturn() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record ExpireLoansCommand(bool Done);

            internal sealed class ExpireLoansCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(ExpireLoansCommand command, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        loanRepository.{|LS2015:Store|}(loan);
                        if (command.Done)
                        {
                            return;
                        }
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2015 stays silent outside Application and Infrastructure")]
    public Task StaysSilentOutsideItsLayers() =>
        Verify.AnalyzerAsync<HandlerLoopWriteAnalyzer>("""
            using System.Collections.Generic;

            namespace Shop.Loans;

            public sealed class Loan
            {
            }

            public interface ILoanRepository
            {
                void Store(Loan loan);
            }

            public sealed record ExpireLoansCommand(int Count);

            internal sealed class ExpireLoansCommandHandler(ILoanRepository loanRepository)
            {
                public void Handle(ExpireLoansCommand command, List<Loan> loans)
                {
                    foreach (var loan in loans)
                    {
                        loanRepository.Store(loan);
                    }
                }
            }
            """, assemblyName: "Shop.Domain");
}
