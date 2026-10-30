namespace Claimwick.Acceptance.Tests.Spending.Receipts.AttachReceipt;

public sealed class AttachReceiptScenarios
{
    /// <proves>scenario/attach-receipt.attaching-a-receipt-succeeds</proves>
    [Fact]
    public Task Attaching_a_receipt_succeedsAsync() => Runner.RunScenarioAsync(
        given => new SubmitExpenseSteps().SubmitAnExpenseAsync("employee-1"),
        when => new AttachReceiptSteps().AttachAReceiptAsync("expense-1"),
        then => new AttachReceiptSteps().TheReceiptIsAttachedAsync());

    /// <proves>scenario/attach-receipt.attaching-to-decided-expense-refused</proves>
    [Fact]
    public Task Attaching_to_a_decided_expense_is_refusedAsync() => Runner.RunScenarioAsync(
        given => new ApproveExpenseSteps().ApproveAnExpenseAsync("expense-1"),
        when => new AttachReceiptSteps().AttachAReceiptAsync("expense-1"),
        then => new AttachReceiptSteps().TheReceiptIsRefusedAsExpenseDecidedAsync());
}
