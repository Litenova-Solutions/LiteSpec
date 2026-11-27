using Claimwick.Kernel.Values;
using Claimwick.Spending.Domain.Policies;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Policies.ChangeReceiptThreshold;

/// <implements>use-case/spending.change-receipt-threshold</implements>
internal sealed class ChangeReceiptThresholdCommandHandler : ICommandHandler<ChangeReceiptThresholdCommand, ChangeReceiptThresholdCommandResult>
{
    private readonly IPolicyRepository _policies;

    public ChangeReceiptThresholdCommandHandler(IPolicyRepository policies)
    {
        _policies = policies;
    }

    public async Task<ChangeReceiptThresholdCommandResult> HandleAsync(
        ChangeReceiptThresholdCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await _policies.GetActiveAsync(cancellationToken);
        policy.SetThreshold(Money.Create(command.Threshold, command.Currency));
        _policies.Store(policy);
        return new ChangeReceiptThresholdCommandResult(command.Threshold, command.Currency);
    }
}
