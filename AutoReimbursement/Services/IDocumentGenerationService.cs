namespace AutoReimbursement.Services;

/// <summary>
/// Service interface for generating documents from invoices.
/// Documents are generated on-demand and returned as streams without server storage.
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// Generates a document of the specified type for the given invoice.
    /// The document is generated on-demand and returned as a stream without storing on the server.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice to generate the document for</param>
    /// <param name="documentType">The type of document to generate</param>
    /// <returns>The generated document with content and metadata</returns>
    /// <exception cref="ArgumentException">Thrown when the invoice is not found or document type is not supported for the invoice type</exception>
    Task<GeneratedDocument> GenerateDocumentAsync(int invoiceId, DocumentType documentType);
    
    /// <summary>
    /// Generates multiple documents for the given invoice.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice to generate documents for</param>
    /// <param name="documentTypes">The types of documents to generate</param>
    /// <returns>List of generated documents with content and metadata</returns>
    Task<List<GeneratedDocument>> GenerateDocumentsAsync(int invoiceId, IEnumerable<DocumentType> documentTypes);
    
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
