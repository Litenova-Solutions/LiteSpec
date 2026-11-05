using Claimwick.Domain;
using Claimwick.Spending.Policies.Domain;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Policies.UseCases.ChangeReceiptThreshold;

/// <implements>use-case/spending.change-receipt-threshold</implements>
internal sealed class ChangeReceiptThresholdCommandHandler(
    IPolicyRepository policies) : ICommandHandler<ChangeReceiptThresholdCommand, ChangeReceiptThresholdCommandResult>
{
    public async Task<ChangeReceiptThresholdCommandResult> HandleAsync(
        ChangeReceiptThresholdCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await policies.GetActiveAsync(cancellationToken);
        policy.SetThreshold(Money.Create(command.Threshold, command.Currency));
        policies.Store(policy);
        return new ChangeReceiptThresholdCommandResult(command.Threshold, command.Currency);
    }
}
