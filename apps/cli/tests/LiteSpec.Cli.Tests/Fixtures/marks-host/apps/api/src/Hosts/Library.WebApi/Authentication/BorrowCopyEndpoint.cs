namespace Library.WebApi.Authentication;

/// <implements>use-case/lending.borrow-copy</implements>
internal sealed class BorrowCopyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/lending/borrow-copy", HandleAsync).WithName("BorrowCopy");
    }

    private static void HandleAsync()
    {
    }
}
