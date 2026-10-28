namespace Library.WebApi.Lending.Holds.CancelHold;

/// <implements>use-case/lending.cancel-hold</implements>
public static class CancelHoldEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/holds/{holdId}/cancel", Handle).WithName("CancelHold");
    }

    private static void Handle()
    {
    }
}
