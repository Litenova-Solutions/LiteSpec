namespace Library.WebApi.Lending.Loans.PurgeLoans;

/// <implements>use-case/lending.purge-loans</implements>
public static class PurgeLoansEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/purge-loans", Handle).WithName("PurgeLoans");
    }

    private static void Handle()
    {
    }
}
