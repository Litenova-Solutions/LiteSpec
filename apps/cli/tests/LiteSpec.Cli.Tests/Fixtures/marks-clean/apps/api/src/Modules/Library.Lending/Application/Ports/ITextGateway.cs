namespace Library.Lending.Application.Ports;

/// <implements>external-system/text-gateway</implements>
public interface ITextGateway
{
    Task SendOverdueNoticeAsync(string phone, string text, CancellationToken cancellationToken);
}
