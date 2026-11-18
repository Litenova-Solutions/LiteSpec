namespace Library.Lending.Application.Ports;

internal sealed partial class SplitStep
{
    public Task StoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
