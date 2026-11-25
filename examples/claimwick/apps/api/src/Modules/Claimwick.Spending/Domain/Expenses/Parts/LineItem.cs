using Claimwick.Kernel.Values;

namespace Claimwick.Spending.Domain.Expenses.Parts;

/// <summary>One line of an expense: the kind of cost with its amount.</summary>
internal sealed record LineItem(string Category, Money Amount);
