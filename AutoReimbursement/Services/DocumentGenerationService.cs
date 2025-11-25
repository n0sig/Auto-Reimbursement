using AutoReimbursement.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoReimbursement.Services;

/// <summary>
/// Stub implementation of document generation service.
/// The actual document generation logic is NOT implemented yet.
/// This service provides the API structure and validation only.
/// </summary>
public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DocumentGenerationService> _logger;

    public DocumentGenerationService(ApplicationDbContext dbContext, ILogger<DocumentGenerationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeneratedDocument> GenerateDocumentAsync(int invoiceId, DocumentType documentType)
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.Payer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            throw new ArgumentException($"Invoice with ID {invoiceId} not found.", nameof(invoiceId));
        }

        if (invoice.Type != InvoiceType.Material)
        {
            throw new ArgumentException($"Document generation is only supported for Material type invoices. Invoice {invoiceId} is of type {invoice.Type}.", nameof(invoiceId));
        }

        _logger.LogInformation("Generating {DocumentType} document for invoice {InvoiceId}", documentType, invoiceId);

        // TODO: Implement actual document generation logic
        // For now, return a placeholder document indicating the feature is not yet implemented
        var content = System.Text.Encoding.UTF8.GetBytes($"Document generation for {documentType} is not yet implemented.\nInvoice ID: {invoiceId}\nInvoice Serial: {invoice.Serial ?? "N/A"}");

        return new GeneratedDocument
        {
            FileName = GenerateFileName(invoice, documentType),
            Content = content,
            ContentType = GetContentType(documentType),
            DocumentType = documentType
        };
    }

    /// <inheritdoc />
    public async Task<List<GeneratedDocument>> GenerateDocumentsAsync(int invoiceId, IEnumerable<DocumentType> documentTypes)
    {
        var documents = new List<GeneratedDocument>();
        
        foreach (var documentType in documentTypes)
        {
            var document = await GenerateDocumentAsync(invoiceId, documentType);
            documents.Add(document);
        }

        return documents;
    }

    /// <inheritdoc />
    public async Task<List<DocumentType>> GetSupportedDocumentTypesAsync(int invoiceId)
    {
        var invoice = await _dbContext.Invoices.FindAsync(invoiceId);
        
        if (invoice == null || invoice.Type != InvoiceType.Material)
        {
            return new List<DocumentType>();
        }

        return new List<DocumentType>
        {
            DocumentType.WarehouseInOut,
            DocumentType.PurchaseRegistration
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentGenerationSupportedAsync(int invoiceId)
    {
        var invoice = await _dbContext.Invoices.FindAsync(invoiceId);
        return invoice != null && invoice.Type == InvoiceType.Material;
    }

    private static string GenerateFileName(Invoice invoice, DocumentType documentType)
    {
        var serial = !string.IsNullOrEmpty(invoice.Serial) ? invoice.Serial : $"Invoice_{invoice.Id}";
        var date = invoice.Date?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd");
        
        return documentType switch
        {
            DocumentType.WarehouseInOut => $"WarehouseInOut_{serial}_{date}.xlsx",
            DocumentType.PurchaseRegistration => $"PurchaseRegistration_{serial}_{date}.xlsx",
            DocumentType.InvoicePdf => $"Invoice_{serial}_{date}.pdf",
            _ => $"Document_{serial}_{date}.txt"
        };
    }

    private static string GetContentType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.WarehouseInOut => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            DocumentType.PurchaseRegistration => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            DocumentType.InvoicePdf => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
