using Claimwick.Platform.Access;
using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.UseCases.ReimburseExpense.Application;

/// <summary>Authorizes the reimburse-expense command from its declared permission.</summary>
internal sealed class ReimburseExpenseCommandAuthorizationDefinition : IAuthorizationDefinition<ReimburseExpenseCommand>
{
    public static Permission Policy { get; } = new("permission/post-reimbursement");

    public AuthorizationDeclaration Authorization => AuthorizationDeclaration.Authorized(Policy.Id);
}
