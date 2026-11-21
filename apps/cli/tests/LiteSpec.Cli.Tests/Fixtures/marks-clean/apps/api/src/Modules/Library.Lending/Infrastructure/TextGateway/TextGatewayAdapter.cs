using Library.Lending.Ports.TextGateway;

namespace Library.Lending.Infrastructure.TextGateway;

/// <implements>external-system/text-gateway</implements>
public sealed class TextGatewayAdapter : ITextGateway
{
    public Task SendOverdueNoticeAsync(string phone, string text, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
