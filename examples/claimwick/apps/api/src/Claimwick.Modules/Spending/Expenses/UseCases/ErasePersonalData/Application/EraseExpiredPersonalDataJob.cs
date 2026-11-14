using Claimwick.Spending.Application;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Expenses.UseCases.ErasePersonalData.Application;

/// <implements>job/spending.erase-expired-personal-data</implements>
/// <follows>litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job</follows>
internal sealed class EraseExpiredPersonalDataJob(
    ICommandMediator mediator,
    IExpiredExpenseSelector selector) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var expenseId in await selector.SelectAsync(stoppingToken))
        {
            await mediator.SendAsync(new ErasePersonalDataCommand(expenseId), stoppingToken);
        }
    }
}
