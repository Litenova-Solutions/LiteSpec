namespace Library.Application.Beta;

/// <implements>use-case/beta.place-order</implements>
public sealed class OrderHandler(ICommandMediator commands) : HandlerBase
{
    public Task Handle(BetaCommand command, CancellationToken cancellationToken) =>
        OrderEffects.RunAsync(command.Id, commands, cancellationToken);
}
