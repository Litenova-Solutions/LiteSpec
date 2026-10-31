namespace Library.Application.Beta;

public abstract class HandlerBase : ICommandHandler<BetaCommand>
{
}

public sealed record BetaCommand(Guid Id);
