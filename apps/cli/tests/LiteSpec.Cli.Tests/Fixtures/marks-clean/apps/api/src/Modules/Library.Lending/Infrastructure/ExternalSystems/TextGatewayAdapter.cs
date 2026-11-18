using Library.Lending.Application.Ports;

namespace Library.Lending.Infrastructure.ExternalSystems;

/// <implements>external-system/text-gateway</implements>
public sealed class TextGatewayAdapter : ITextGateway
{
    public Task SendOverdueNoticeAsync(string phone, string text, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
