using LiteBus.Messaging.Abstractions;

namespace Claimwick.Spending.Application.ErasePersonalData;

/// <summary>Declares the erase-personal-data command exempt from auditing.</summary>
internal sealed class ErasePersonalDataCommandAuditDefinition : IAuditDefinition<ErasePersonalDataCommand>
{
    public AuditDeclaration Audit { get; } = AuditDeclaration.Exempt("Erasure keeps the business facts for the tax file and trails nothing personal.");
}
