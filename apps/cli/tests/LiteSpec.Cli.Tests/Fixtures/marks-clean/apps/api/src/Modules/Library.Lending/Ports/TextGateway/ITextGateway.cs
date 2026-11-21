namespace Library.Lending.Ports.TextGateway;

/// <implements>external-system/text-gateway</implements>
public interface ITextGateway
{
    Task SendOverdueNoticeAsync(string phone, string text, CancellationToken cancellationToken);
}
