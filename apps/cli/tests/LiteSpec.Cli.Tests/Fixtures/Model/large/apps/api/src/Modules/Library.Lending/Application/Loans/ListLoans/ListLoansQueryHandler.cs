namespace Library.Lending.Application.Loans.ListLoans;

/// <implements>use-case/lending.list-loans</implements>
public sealed class ListLoansQueryHandler : IQueryHandler<ListLoansQuery>
{
    public Task Handle(ListLoansQuery query) => Task.CompletedTask;
}
