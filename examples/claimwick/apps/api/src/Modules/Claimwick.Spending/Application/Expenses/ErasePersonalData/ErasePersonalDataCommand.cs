using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Expenses.ErasePersonalData;

internal sealed record ErasePersonalDataCommand(ExpenseId ExpenseId);
