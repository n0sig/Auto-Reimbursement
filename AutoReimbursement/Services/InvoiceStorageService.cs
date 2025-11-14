namespace AutoReimbursement.Services;

public class InvoiceStorageService : IInvoiceStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InvoiceStorageService> _logger;
    private const string UploadFolder = "uploads/invoices";

    public InvoiceStorageService(IWebHostEnvironment environment, ILogger<InvoiceStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> StorePdfAsync(Stream fileStream, string fileName)
    {
        // Ensure the upload directory exists
        var uploadPath = Path.Combine(_environment.WebRootPath, UploadFolder);
        Directory.CreateDirectory(uploadPath);

        // Generate a unique filename to avoid conflicts
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(uploadPath, uniqueFileName);
        var relativePath = Path.Combine(UploadFolder, uniqueFileName);

        // Save the file
        using (var fileStreamOutput = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        _logger.LogInformation("PDF stored at {Path}", relativePath);
        return relativePath;
    }

    public string GetPdfFullPath(string relativePath)
    {
        return Path.Combine(_environment.WebRootPath, relativePath);
    }

    public bool PdfExists(string relativePath)
    {
        var fullPath = GetPdfFullPath(relativePath);
        return File.Exists(fullPath);
    }

    public async Task DeletePdfAsync(string relativePath)
    {
        var fullPath = GetPdfFullPath(relativePath);
        if (File.Exists(fullPath))
        {
            await Task.Run(() => File.Delete(fullPath));
            _logger.LogInformation("PDF deleted from {Path}", relativePath);
        }
    }

    public async Task<Stream> GetPdfStreamAsync(string relativePath)
    {
        var fullPath = GetPdfFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"PDF file not found: {relativePath}");
        }

        return await Task.Run(() => (Stream)new FileStream(fullPath, FileMode.Open, FileAccess.Read));
    }
}
