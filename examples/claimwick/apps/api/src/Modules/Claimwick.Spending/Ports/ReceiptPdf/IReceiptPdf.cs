namespace Claimwick.Spending.Ports.ReceiptPdf;

/// <summary>Renders one receipt as a PDF file for the export batch.</summary>
internal interface IReceiptPdf
{
    /// <summary>Renders the receipt text as PDF bytes.</summary>
    byte[] Render(string text);
}
