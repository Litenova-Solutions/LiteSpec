using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2014, the find or create rule.</summary>
public sealed class FindOrCreateAnalyzerTests
{
    [Fact(DisplayName = "LS2014 reports a static factory where the find came back empty")]
    public Task ReportsAStaticFactoryAfterAFind() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler(IReaderRepository readerRepository)
            {
                public void Handle(RegisterReaderCommand command)
                {
                    var reader = readerRepository.FindByCard(command.CardId);
                    if (reader is null)
                    {
                        reader = Reader.{|LS2014:Register|}(command.CardId);
                    }
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 reports a constructor as the fallback of an async find")]
    public Task ReportsAConstructorAfterAnAsyncFind() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;
            using System.Threading.Tasks;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Task<Reader?> FindByCardAsync(Guid cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler(IReaderRepository readerRepository)
            {
                public async Task HandleAsync(RegisterReaderCommand command)
                {
                    var reader = await readerRepository.FindByCardAsync(command.CardId) ?? new {|LS2014:Reader|}(command.CardId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 reports a creation after a branch that leaves when the record was found")]
    public Task ReportsACreationAfterALeavingFoundBranch() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public Guid Id { get; } = Guid.NewGuid();

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler(IReaderRepository readerRepository)
            {
                public Guid Handle(RegisterReaderCommand command)
                {
                    var existing = readerRepository.FindByCard(command.CardId);
                    if (existing is not null)
                    {
                        return existing.Id;
                    }

                    return Reader.{|LS2014:Register|}(command.CardId).Id;
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 reports a coalescing assignment of a found record")]
    public Task ReportsACoalescingAssignment() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler(IReaderRepository readerRepository)
            {
                public void Handle(RegisterReaderCommand command)
                {
                    var reader = readerRepository.FindByCard(command.CardId);
                    reader ??= Reader.{|LS2014:Register|}(command.CardId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a creation after refusing a missing record")]
    public Task AcceptsACreationAfterRefusingAMissingRecord() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Notice
            {
                public Notice(Guid loanId) { }

                public bool Sent { get; }

                public static Notice FollowUp(Notice first) => new Notice(Guid.Empty);
            }

            public interface INoticeRepository
            {
                Notice? FindFirstForLoan(Guid loanId);
            }

            public sealed class LoanNotFoundException : Exception { }

            public sealed record SendFollowUpNoticeCommand(Guid LoanId);

            internal sealed class SendFollowUpNoticeCommandHandler(INoticeRepository noticeRepository)
            {
                public void Handle(SendFollowUpNoticeCommand command)
                {
                    var first = noticeRepository.FindFirstForLoan(command.LoanId);
                    if (first is null || !first.Sent)
                    {
                        throw new LoanNotFoundException();
                    }

                    var followUp = Notice.FollowUp(first);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a creation that reads another record of its type")]
    public Task AcceptsACreationReadingAnotherRecordOfItsType() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class FineStatement
            {
                public FineStatement(DateOnly day, FineStatement? previous) { }
            }

            public interface IFineStatementRepository
            {
                FineStatement? FindPrevious(DateOnly before);
            }

            public sealed record IssueFineStatementCommand(DateOnly Day);

            internal sealed class IssueFineStatementCommandHandler(IFineStatementRepository fineStatementRepository)
            {
                public void Handle(IssueFineStatementCommand command)
                {
                    var previous = fineStatementRepository.FindPrevious(command.Day);
                    var statement = new FineStatement(command.Day, previous);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a handler that only finds")]
    public Task AcceptsAHandlerThatOnlyFinds() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record RecordReaderSignInCommand(Guid CardId);

            internal sealed class RecordReaderSignInCommandHandler(IReaderRepository readerRepository)
            {
                public void Handle(RecordReaderSignInCommand command)
                {
                    var existing = readerRepository.FindByCard(command.CardId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a handler that only creates")]
    public Task AcceptsAHandlerThatOnlyCreates() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler
            {
                public void Handle(RegisterReaderCommand command)
                {
                    var reader = Reader.Register(command.CardId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a find and a creation of another type")]
    public Task AcceptsAFindAndACreationOfAnotherType() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }
            }

            public sealed class Hold
            {
                public Hold(Guid bookId) { }
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record PlaceHoldCommand(Guid CardId, Guid BookId);

            internal sealed class PlaceHoldCommandHandler(IReaderRepository readerRepository)
            {
                public void Handle(PlaceHoldCommand command)
                {
                    var existing = readerRepository.FindByCard(command.CardId);
                    var hold = new Hold(command.BookId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a find and a creation in different methods")]
    public Task AcceptsAFindAndACreationInDifferentMethods() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler(IReaderRepository readerRepository)
            {
                public void Handle(RegisterReaderCommand command)
                {
                    var existing = readerRepository.FindByCard(command.CardId);
                }

                public Reader Register(RegisterReaderCommand command) =>
                    Reader.Register(command.CardId);
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 accepts a find or create in a query handler")]
    public Task AcceptsAFindOrCreateInAQueryHandler() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record GetReaderQuery(Guid CardId);

            internal sealed class GetReaderQueryHandler(IReaderRepository readerRepository)
            {
                public void Handle(GetReaderQuery query)
                {
                    var existing = readerRepository.FindByCard(query.CardId);
                    var reader = Reader.Register(query.CardId);
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2014 stays silent outside Application and Infrastructure")]
    public Task StaysSilentOutsideItsLayers() =>
        Verify.AnalyzerAsync<FindOrCreateAnalyzer>("""
            using System;

            namespace Shop.Loans;

            public sealed class Reader
            {
                public Reader(Guid cardId) { }

                public static Reader Register(Guid cardId) => new Reader(cardId);
            }

            public interface IReaderRepository
            {
                Reader? FindByCard(Guid cardId);
            }

            public sealed record RegisterReaderCommand(Guid CardId);

            internal sealed class RegisterReaderCommandHandler(IReaderRepository readerRepository)
            {
                public void Handle(RegisterReaderCommand command)
                {
                    var existing = readerRepository.FindByCard(command.CardId);
                    var reader = Reader.Register(command.CardId);
                }
            }
            """, assemblyName: "Shop.Domain");
}
