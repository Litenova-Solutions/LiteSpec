using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.ErasePersonalData;

/// <implements>job/spending.erase-expired-personal-data</implements>
/// <follows>litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job</follows>
internal sealed class EraseExpiredPersonalDataJob : BackgroundService
{
    private readonly ICommandMediator _mediator;
    private readonly IExpiredExpenseSelector _selector;

    public EraseExpiredPersonalDataJob(ICommandMediator mediator, IExpiredExpenseSelector selector)
    {
        _mediator = mediator;
        _selector = selector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var expenseId in await _selector.SelectAsync(stoppingToken))
        {
            await _mediator.SendAsync(new ErasePersonalDataCommand(expenseId), stoppingToken);
        }
    }
}
