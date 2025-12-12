namespace AutoReimbursement.Services;

/// <summary>
/// Service interface for generating documents from invoices.
/// Documents are generated on-demand and returned as streams without server storage.
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// Generates documents for one or more invoices.
    /// When multiple invoices are provided, data is combined into each document type.
    /// This is the unified method for both single-invoice and batch document generation.
    /// </summary>
    /// <param name="invoiceIds">The IDs of the invoices to generate documents for</param>
    /// <param name="documentTypes">The types of documents to generate</param>
    /// <returns>List of generated documents with content and metadata</returns>
    /// <exception cref="ArgumentException">Thrown when any invoice is not found or document type is not supported</exception>
    Task<List<GeneratedDocument>> GenerateDocumentsAsync(IEnumerable<int> invoiceIds, IEnumerable<DocumentType> documentTypes);
    
    /// <summary>
    /// Gets the supported document types for the given invoice type.
    /// Currently only Material type invoices support document generation.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice</param>
    /// <returns>List of supported document types for the invoice</returns>
    Task<List<DocumentType>> GetSupportedDocumentTypesAsync(int invoiceId);
    
    /// <summary>
    /// Checks if document generation is supported for the given invoice.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice</param>
    /// <returns>True if document generation is supported, false otherwise</returns>
    Task<bool> IsDocumentGenerationSupportedAsync(int invoiceId);
}
