namespace Library.WebApi.Lending.Loans.BorrowCopy;

/// <implements>rule/loan.copy-available</implements>
public static class BorrowCopyEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/borrow-copy", Handle).WithName("BorrowCopy");
    }

    private static void Handle()
    {
    }
}
