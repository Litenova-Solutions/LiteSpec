using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ErasePersonalData;

internal sealed record ErasePersonalDataCommand(ExpenseId ExpenseId) : ICommand<ErasePersonalDataCommandResult>;
