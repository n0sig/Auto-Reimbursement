namespace AutoReimbursement.Services;

public interface IInvoiceStorageService
{
    /// <summary>
    /// Stores a PDF file and returns the relative path where it was stored
    /// </summary>
    Task<string> StorePdfAsync(Stream fileStream, string fileName);
    
    /// <summary>
    /// Gets the full file path for a stored PDF
    /// </summary>
    string GetPdfFullPath(string relativePath);
    
    /// <summary>
    /// Checks if a PDF file exists
    /// </summary>
    bool PdfExists(string relativePath);
    
    /// <summary>
    /// Deletes a PDF file
    /// </summary>
    Task DeletePdfAsync(string relativePath);
    
    /// <summary>
    /// Gets a read stream for a PDF file
    /// </summary>
    Task<Stream> GetPdfStreamAsync(string relativePath);
}
