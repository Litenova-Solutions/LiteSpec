namespace Library.Orders.Alpha;

/// <implements>use-case/alpha.get-order</implements>
public sealed class OrderHandler(ICommandMediator commands) : HandlerBase
{
    public Task Handle(AlphaQuery query, CancellationToken cancellationToken) =>
        OrderEffects.RunAsync(query.Id, commands, cancellationToken);
}
