namespace Demo.Billing.Invoicing.Domain;

/// <implements>aggregate/billing.invoice</implements>
public sealed class Invoice : AggregateRoot<Guid>
{
    private Invoice(Guid id, decimal amount)
    {
        Id = id;
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Invoice Send(Guid id, decimal amount) => new(id, amount);
}
