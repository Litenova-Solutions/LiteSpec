namespace Library.Application.Alpha;

public abstract class HandlerBase : IQueryHandler<AlphaQuery, AlphaResult>
{
}

public sealed record AlphaQuery(Guid Id);

public sealed record AlphaResult;
