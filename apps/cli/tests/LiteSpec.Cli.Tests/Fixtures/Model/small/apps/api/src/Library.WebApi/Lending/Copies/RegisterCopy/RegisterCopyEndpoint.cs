namespace Library.WebApi.Lending.Copies.RegisterCopy;

/// <implements>use-case/lending.register-copy</implements>
public static class RegisterCopyEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/lending/copies", Handle).WithName("RegisterCopy");
    }

    private static void Handle()
    {
    }
}
