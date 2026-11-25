using Claimwick.Kernel.Values;

namespace Claimwick.Spending.Contracts.Expenses;

/// <summary>One category subtotal of an expense: the kind of cost with its amount. The read-expense subtotals share it, so it lives in Contracts.</summary>
public sealed record CategoryTotal(string Category, Money Amount);
