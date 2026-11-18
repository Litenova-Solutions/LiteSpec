namespace Library.Lending.Application.Ports;

/// <implements>external-system/text-gateway</implements>
public interface ITextGateway
{
    Task SendOverdueNoticeAsync(Guid readerId, CancellationToken cancellationToken);

    Task<string> ReceiveDeliveryReceiptAsync(CancellationToken cancellationToken);
}
