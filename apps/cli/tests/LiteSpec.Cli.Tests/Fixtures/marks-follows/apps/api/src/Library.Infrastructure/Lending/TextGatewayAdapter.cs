using Library.Application.Lending.Ports;

namespace Library.Infrastructure.Lending;

/// <implements>external-system/text-gateway</implements>
public sealed class TextGatewayAdapter : ITextGateway
{
    public Task SendOverdueNoticeAsync(string phone, string text, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
