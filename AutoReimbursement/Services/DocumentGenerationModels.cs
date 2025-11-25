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
/// Request parameters for document generation
/// </summary>
public class DocumentGenerationRequest
{
    /// <summary>
    /// The invoice ID to generate documents for
    /// </summary>
    public int InvoiceId { get; set; }
    
    /// <summary>
    /// The types of documents to generate
    /// </summary>
    public List<DocumentType> DocumentTypes { get; set; } = new();
}
