using Claimwick.Application.Spending.Expenses.ErasePersonalData;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Application.Spending.Expenses.EraseExpiredPersonalData;

/// <implements>automation/spending.erase-expired-personal-data</implements>
internal sealed class EraseExpiredPersonalDataJob(ICommandMediator mediator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await mediator.SendAsync(new ErasePersonalDataCommand(Guid.Empty), stoppingToken);
    }
}
