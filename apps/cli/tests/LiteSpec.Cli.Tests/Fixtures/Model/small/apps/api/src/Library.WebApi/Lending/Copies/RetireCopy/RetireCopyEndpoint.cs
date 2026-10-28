namespace Library.WebApi.Lending.Copies.RetireCopy;

/// <implements>use-case/lending.retire-copy</implements>
public static class RetireCopyEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/copies/{copyId}/retire", Handle).WithName("RetireCopy");
    }

    private static void Handle()
    {
    }
}
