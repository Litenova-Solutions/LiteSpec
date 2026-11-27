using Library.Lending.Ports.TextGateway;

namespace Library.Lending.Infrastructure.TextGateway;

/// <implements>external-system/text-gateway</implements>
public sealed class TextGatewayAdapter : ITextGateway
{
    public Task SendOverdueNoticeAsync(Guid readerId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<string> ReceiveDeliveryReceiptAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult("");
    }
}
