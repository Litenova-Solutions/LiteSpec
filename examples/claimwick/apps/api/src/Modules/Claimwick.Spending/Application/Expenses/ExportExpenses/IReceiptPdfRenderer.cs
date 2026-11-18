namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

/// <summary>Renders one receipt as a PDF file for the export batch.</summary>
internal interface IReceiptPdfRenderer
{
    /// <summary>Renders the receipt text as PDF bytes.</summary>
    byte[] RenderReceiptPdf(string text);
}
