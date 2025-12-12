namespace AutoReimbursement.Services;

/// <summary>
/// Result of a document generation operation
/// </summary>
public class GeneratedDocument
{
    /// <summary>
    /// The file name of the generated document
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The raw content of the generated document as bytes
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// The MIME content type of the document
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";
    
    /// <summary>
    /// The type of document that was generated
    /// </summary>
    public DocumentType DocumentType { get; set; }
}

/// <summary>
/// Request parameters for document generation.
/// Supports both single and multiple invoices in one unified request model.
/// </summary>
public class DocumentGenerationRequest
{
    /// <summary>
    /// The invoice IDs to generate documents for.
    /// Use single ID for single-invoice generation, or multiple IDs for combined documents.
    /// </summary>
    public List<int> InvoiceIds { get; set; } = new();
    
    /// <summary>
    /// The types of documents to generate.
    /// Each document type will contain data from all specified invoices.
    /// </summary>
    public List<DocumentType> DocumentTypes { get; set; } = new();
}
