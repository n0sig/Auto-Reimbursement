using AutoReimbursement.Data;

namespace AutoReimbursement.Services;

/// <summary>
/// Represents extracted invoice data from LLM processing
/// </summary>
public class ExtractedInvoiceData
{
    public string? SerialNumber { get; set; }
    public DateTime? Date { get; set; }
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ExtractedInvoiceItem> Items { get; set; } = new();
}

public class ExtractedInvoiceItem
{
    public string Name { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public string? Unit { get; set; }
    public decimal? Amount { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal TotalPriceWithoutTax { get; set; }
    public decimal Tax { get; set; }
}

public interface IInvoiceLLMService
{
    /// <summary>
    /// Extracts invoice data from a PDF file using LLM
    /// </summary>
    Task<ExtractedInvoiceData> ExtractInvoiceDataAsync(Stream pdfStream);
}
