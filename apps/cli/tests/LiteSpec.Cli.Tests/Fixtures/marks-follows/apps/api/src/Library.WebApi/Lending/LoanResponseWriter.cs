namespace Library.WebApi.Lending;

/// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>
internal sealed class LoanResponseWriter
{
    public string Write(Guid loanId) => loanId.ToString();
}
