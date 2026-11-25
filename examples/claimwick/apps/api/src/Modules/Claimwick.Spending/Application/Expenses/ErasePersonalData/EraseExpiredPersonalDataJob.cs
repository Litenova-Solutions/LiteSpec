using Claimwick.Spending.Domain.Expenses;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Claimwick.Spending.Application.Expenses.ErasePersonalData;

/// <implements>job/spending.erase-expired-personal-data</implements>
/// <follows>litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job</follows>
internal sealed class EraseExpiredPersonalDataJob : BackgroundService
{
    private readonly ICommandMediator _mediator;
    private readonly IExpenseRepository _expenses;

    public EraseExpiredPersonalDataJob(ICommandMediator mediator, IExpenseRepository expenses)
    {
        _mediator = mediator;
        _expenses = expenses;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var expenseId in await _expenses.SelectExpiredAsync(stoppingToken))
        {
            await _mediator.SendAsync(new ErasePersonalDataCommand(expenseId), stoppingToken);
        }
    }
}
