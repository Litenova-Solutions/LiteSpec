using Demo.Billing.Invoicing.Domain;

namespace Demo.Billing.Invoicing.SendInvoice;

/// <implements>use-case/billing.send-invoice</implements>
internal sealed class SendInvoiceCommandHandler(IInvoiceRepository invoices)
{
    public async Task<Guid> HandleAsync(SendInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = Invoice.Send(command.InvoiceId, command.Amount);
        invoices.Store(invoice);
        return await Task.FromResult(invoice.Id);
    }
}
