namespace Library.WebApi.Lending.Loans.ReturnCopy;

/// <implements>use-case/lending.return-copy</implements>
public static class ReturnCopyEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/loans/{loanId}/return", Handle).WithName("ReturnCopy");
    }

    private static void Handle()
    {
    }
}
