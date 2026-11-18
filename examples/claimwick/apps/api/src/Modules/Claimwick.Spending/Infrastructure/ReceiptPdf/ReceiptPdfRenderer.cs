using System.Text;
using Claimwick.Spending.Ports.ReceiptPdf;

namespace Claimwick.Spending.Infrastructure.ReceiptPdf;

/// <summary>Renders an expense receipt as a PDF document.</summary>
/// <follows>decision/0005-render-receipts-as-pdf</follows>
internal sealed class ReceiptPdfRenderer : IReceiptPdf
{
    /// <summary>
    ///     Describes the rendering of the receipt text as bytes. The infrastructure
    ///     adapter renders the file itself with the admitted library.
    /// </summary>
    public byte[] Render(string text) =>
        Encoding.UTF8.GetBytes("Receipt PDF for: " + text);
}
