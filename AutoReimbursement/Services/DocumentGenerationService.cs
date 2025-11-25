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
    public async Task<GeneratedDocument> GenerateCombinedDocumentAsync(IEnumerable<int> invoiceIds, DocumentType documentType)
    {
        var invoiceIdList = invoiceIds.ToList();
        
        if (!invoiceIdList.Any())
        {
            throw new ArgumentException("At least one invoice ID must be provided.", nameof(invoiceIds));
        }

        var invoices = await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.Payer)
            .Where(i => invoiceIdList.Contains(i.Id))
            .ToListAsync();

        if (invoices.Count != invoiceIdList.Count)
        {
            var foundIds = invoices.Select(i => i.Id).ToHashSet();
            var missingIds = invoiceIdList.Where(id => !foundIds.Contains(id)).ToList();
            throw new ArgumentException($"Invoices not found: {string.Join(", ", missingIds)}", nameof(invoiceIds));
        }

        var nonMaterialInvoices = invoices.Where(i => i.Type != InvoiceType.Material).ToList();
        if (nonMaterialInvoices.Any())
        {
            var invalidIds = nonMaterialInvoices.Select(i => i.Id).ToList();
            throw new ArgumentException($"Document generation is only supported for Material type invoices. Invalid invoices: {string.Join(", ", invalidIds)}", nameof(invoiceIds));
        }

        _logger.LogInformation("Generating combined {DocumentType} document for {Count} invoices: {InvoiceIds}", 
            documentType, invoices.Count, string.Join(", ", invoiceIdList));

        // TODO: Implement actual combined document generation logic
        // For now, return a placeholder document indicating the feature is not yet implemented
        var invoiceSerials = string.Join(", ", invoices.Select(i => i.Serial ?? $"ID:{i.Id}"));
        var content = System.Text.Encoding.UTF8.GetBytes(
            $"Combined document generation for {documentType} is not yet implemented.\n" +
            $"Invoice Count: {invoices.Count}\n" +
            $"Invoice Serials: {invoiceSerials}");

        return new GeneratedDocument
        {
            FileName = GenerateCombinedFileName(invoices, documentType),
            Content = content,
            ContentType = GetContentType(documentType),
            DocumentType = documentType
        };
    }

    /// <inheritdoc />
    public async Task<List<GeneratedDocument>> GenerateCombinedDocumentsAsync(IEnumerable<int> invoiceIds, IEnumerable<DocumentType> documentTypes)
    {
        var documentTypeList = documentTypes?.ToList() ?? new List<DocumentType>();
        if (!documentTypeList.Any())
        {
            throw new ArgumentException("At least one document type must be provided.", nameof(documentTypes));
        }

        var invoiceIdList = invoiceIds.ToList();
        if (!invoiceIdList.Any())
        {
            throw new ArgumentException("At least one invoice ID must be provided.", nameof(invoiceIds));
        }

        // Fetch and validate invoices once to avoid N+1 queries
        var invoices = await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.Payer)
            .Where(i => invoiceIdList.Contains(i.Id))
            .ToListAsync();

        if (invoices.Count != invoiceIdList.Count)
        {
            var foundIds = invoices.Select(i => i.Id).ToHashSet();
            var missingIds = invoiceIdList.Where(id => !foundIds.Contains(id)).ToList();
            throw new ArgumentException($"Invoices not found: {string.Join(", ", missingIds)}", nameof(invoiceIds));
        }

        var nonMaterialInvoices = invoices.Where(i => i.Type != InvoiceType.Material).ToList();
        if (nonMaterialInvoices.Any())
        {
            var invalidIds = nonMaterialInvoices.Select(i => i.Id).ToList();
            throw new ArgumentException($"Document generation is only supported for Material type invoices. Invalid invoices: {string.Join(", ", invalidIds)}", nameof(invoiceIds));
        }

        // Generate documents for each type using pre-fetched invoices
        var documents = new List<GeneratedDocument>();
        foreach (var documentType in documentTypeList)
        {
            var document = GenerateCombinedDocumentFromInvoices(invoices, documentType);
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

    private static string GenerateCombinedFileName(List<Invoice> invoices, DocumentType documentType)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var count = invoices.Count;
        
        return documentType switch
        {
            DocumentType.WarehouseInOut => $"WarehouseInOut_Combined_{count}invoices_{date}.xlsx",
            DocumentType.PurchaseRegistration => $"PurchaseRegistration_Combined_{count}invoices_{date}.xlsx",
            DocumentType.InvoicePdf => $"Invoices_Combined_{count}invoices_{date}.pdf",
            _ => $"Document_Combined_{count}invoices_{date}.txt"
        };
    }

    private GeneratedDocument GenerateCombinedDocumentFromInvoices(List<Invoice> invoices, DocumentType documentType)
    {
        _logger.LogInformation("Generating combined {DocumentType} document for {Count} invoices", 
            documentType, invoices.Count);

        // TODO: Implement actual combined document generation logic
        // For now, return a placeholder document indicating the feature is not yet implemented
        var invoiceSerials = string.Join(", ", invoices.Select(i => i.Serial ?? $"ID:{i.Id}"));
        var content = System.Text.Encoding.UTF8.GetBytes(
            $"Combined document generation for {documentType} is not yet implemented.\n" +
            $"Invoice Count: {invoices.Count}\n" +
            $"Invoice Serials: {invoiceSerials}");

        return new GeneratedDocument
        {
            FileName = GenerateCombinedFileName(invoices, documentType),
            Content = content,
            ContentType = GetContentType(documentType),
            DocumentType = documentType
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
