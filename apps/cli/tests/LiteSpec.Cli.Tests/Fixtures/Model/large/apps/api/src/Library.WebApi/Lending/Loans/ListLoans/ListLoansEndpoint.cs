namespace Library.WebApi.Lending.Loans.ListLoans;

/// <implements>use-case/lending.list-loans</implements>
public static class ListLoansEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/lending/loans", Handle).WithName("ListLoans");
    }

    private static void Handle()
    {
    }
}
