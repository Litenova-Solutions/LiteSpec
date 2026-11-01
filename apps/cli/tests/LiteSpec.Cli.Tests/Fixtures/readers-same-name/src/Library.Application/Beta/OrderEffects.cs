namespace Library.Application.Beta;

internal static class OrderEffects
{
    public static async Task RunAsync(Guid id, ICommandMediator commands, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new RefundCardCommand(), cancellationToken);
    }
}

public sealed record RefundCardCommand;
