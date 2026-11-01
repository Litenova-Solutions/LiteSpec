namespace Library.WebApi.Lending.Holds.PlaceHold;

/// <implements>use-case/lending.place-hold</implements>
public static class PlaceHoldEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/holds", Handle).WithName("PlaceHold");
    }

    private static void Handle()
    {
    }
}
