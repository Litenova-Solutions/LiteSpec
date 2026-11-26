namespace Claimwick.Spending.WebApi.Expenses.ErasePersonalData;

/// <summary>The answer to an erase request: the erased expense's identity.</summary>
internal sealed record ErasePersonalDataResponseModel(Guid ExpenseId);
