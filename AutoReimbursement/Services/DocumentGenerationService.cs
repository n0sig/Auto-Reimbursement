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
    public async Task<List<GeneratedDocument>> GenerateDocumentsAsync(IEnumerable<int> invoiceIds, IEnumerable<DocumentType> documentTypes)
    {
        var invoiceIdList = invoiceIds?.ToList() ?? new List<int>();
        var documentTypeList = documentTypes?.ToList() ?? new List<DocumentType>();
        
        if (!invoiceIdList.Any())
        {
            throw new ArgumentException("At least one invoice ID must be provided.", nameof(invoiceIds));
        }

        if (!documentTypeList.Any())
        {
            throw new ArgumentException("At least one document type must be provided.", nameof(documentTypes));
        }

        // Fetch all invoices in a single query
        var invoices = await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.Payer)
            .Where(i => invoiceIdList.Contains(i.Id))
            .ToListAsync();

        // Validate all invoices exist
        if (invoices.Count != invoiceIdList.Count)
        {
            var foundIds = invoices.Select(i => i.Id).ToHashSet();
            var missingIds = invoiceIdList.Where(id => !foundIds.Contains(id)).ToList();
            throw new ArgumentException($"Invoices not found: {string.Join(", ", missingIds)}", nameof(invoiceIds));
        }

        // Validate all invoices are Material type
        var nonMaterialInvoices = invoices.Where(i => i.Type != InvoiceType.Material).ToList();
        if (nonMaterialInvoices.Any())
        {
            var invalidIds = nonMaterialInvoices.Select(i => i.Id).ToList();
            throw new ArgumentException($"Document generation is only supported for Material type invoices. Invalid invoices: {string.Join(", ", invalidIds)}", nameof(invoiceIds));
        }

        _logger.LogInformation("Generating {DocumentTypeCount} document type(s) for {InvoiceCount} invoice(s): {InvoiceIds}", 
            documentTypeList.Count, invoices.Count, string.Join(", ", invoiceIdList));

        // Generate each document type
        var documents = new List<GeneratedDocument>();
        foreach (var documentType in documentTypeList)
        {
            var document = GenerateDocumentFromInvoices(invoices, documentType);
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

    private GeneratedDocument GenerateDocumentFromInvoices(List<Invoice> invoices, DocumentType documentType)
    {
        _logger.LogInformation("Generating {DocumentType} document for {Count} invoice(s)", 
            documentType, invoices.Count);

        // TODO: Implement actual document generation logic
        // For now, return a placeholder document indicating the feature is not yet implemented
        var invoiceSerials = string.Join(", ", invoices.Select(i => i.Serial ?? $"ID:{i.Id}"));
        var content = System.Text.Encoding.UTF8.GetBytes(
            $"Document generation for {documentType} is not yet implemented.\n" +
            $"Invoice Count: {invoices.Count}\n" +
            $"Invoice Serials: {invoiceSerials}");

        return new GeneratedDocument
        {
            FileName = GenerateFileName(invoices, documentType),
            Content = content,
            ContentType = GetContentType(documentType),
            DocumentType = documentType
        };
    }

    private static string GenerateFileName(List<Invoice> invoices, DocumentType documentType)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        
        // Single invoice: use invoice serial
        if (invoices.Count == 1)
        {
            var invoice = invoices[0];
            var serial = !string.IsNullOrEmpty(invoice.Serial) ? invoice.Serial : $"Invoice_{invoice.Id}";
            var invoiceDate = invoice.Date?.ToString("yyyyMMdd") ?? date;
            
            return documentType switch
            {
                DocumentType.WarehouseInOut => $"WarehouseInOut_{serial}_{invoiceDate}.xlsx",
                DocumentType.PurchaseRegistration => $"PurchaseRegistration_{serial}_{invoiceDate}.xlsx",
                DocumentType.InvoicePdf => $"Invoice_{serial}_{invoiceDate}.pdf",
                _ => $"Document_{serial}_{invoiceDate}.txt"
            };
        }
        
        // Multiple invoices: use combined naming
        var count = invoices.Count;
        return documentType switch
        {
            DocumentType.WarehouseInOut => $"WarehouseInOut_Combined_{count}invoices_{date}.xlsx",
            DocumentType.PurchaseRegistration => $"PurchaseRegistration_Combined_{count}invoices_{date}.xlsx",
            DocumentType.InvoicePdf => $"Invoices_Combined_{count}invoices_{date}.pdf",
            _ => $"Document_Combined_{count}invoices_{date}.txt"
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
