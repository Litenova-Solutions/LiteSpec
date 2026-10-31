namespace Library.Application.Alpha;

internal static class OrderEffects
{
    public static async Task RunAsync(Guid id, ICommandMediator commands, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new ChargeCardCommand(), cancellationToken);
    }
}

public sealed record ChargeCardCommand;
