namespace Library.Lending.Application.Loans.BorrowCopy;

/// <follows>litespec/core/backend-persistence#commit-each-command-pipeline-once</follows>
internal sealed class LoanResponseWriter
{
    public string Write(Guid loanId) => loanId.ToString();
}
