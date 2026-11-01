namespace Library.WebApi.Lending.Loans.ExtendLoan;

/// <implements>use-case/lending.extend-loan</implements>
public static class ExtendLoanEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/loans/{loanId}/extend", Handle).WithName("ExtendLoan");
    }

    private static void Handle()
    {
    }
}
