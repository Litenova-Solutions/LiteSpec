using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.Expenses.SubmitExpense;

/// <summary>Authorizes the submit-expense command from its declared permission.</summary>
internal sealed class SubmitExpenseCommandAuthorizationDefinition : IAuthorizationDefinition<SubmitExpenseCommand>
{
    public static Permission Policy { get; } = new(SpendingPermissions.RequestExpense);

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
