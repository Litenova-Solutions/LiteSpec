namespace Library.Application.Lending.Ports;

/// <implements>external-system/text-gateway</implements>
/// <follows>decision/0002-store-loans-as-documents</follows>
public interface ITextGateway
{
    Task SendOverdueNoticeAsync(string phone, string text, CancellationToken cancellationToken);
}
