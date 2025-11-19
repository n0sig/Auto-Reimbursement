using AutoReimbursement.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace AutoReimbursement.Services;

public class BulkInvoiceService : IBulkInvoiceService
{
    private readonly ILogger<BulkInvoiceService> _logger;
    private readonly IInvoiceStorageService _storageService;
    private readonly IInvoiceLLMService _llmService;
    private readonly ApplicationDbContext _dbContext;

    public BulkInvoiceService(
        ILogger<BulkInvoiceService> logger,
        IInvoiceStorageService storageService,
        IInvoiceLLMService llmService,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _storageService = storageService;
        _llmService = llmService;
        _dbContext = dbContext;
    }

    public async Task ProcessBulkUploadAsync(
        List<IBrowserFile> files,
        string payerId,
        int reimbursementPlanId,
        Action<BulkUploadProgress> progressCallback)
    {
        // First, read all files into memory to avoid Blazor file reference issues
        var fileDataList = new List<(string FileName, byte[] Data)>();
        
        foreach (var file in files)
        {
            var progress = new BulkUploadProgress
            {
                FileName = file.Name,
                Status = BulkUploadStatus.Uploading,
                Message = "Reading file..."
            };
            progressCallback(progress);

            try
            {
                await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                fileDataList.Add((file.Name, memoryStream.ToArray()));
            }
            catch (Exception ex)
            {
                progress.Status = BulkUploadStatus.Failed;
                progress.Message = $"Failed to read file: {ex.Message}";
                progressCallback(progress);
                _logger.LogError(ex, "Error reading file {FileName}", file.Name);
            }
        }

        // Now process each file from memory
        foreach (var (fileName, fileData) in fileDataList)
        {
            var progress = new BulkUploadProgress
            {
                FileName = fileName,
                Status = BulkUploadStatus.Uploading,
                Message = "Uploading PDF..."
            };
            progressCallback(progress);

            try
            {
                // Step 1: Upload PDF
                await using var stream = new MemoryStream(fileData);
                var pdfPath = await _storageService.StorePdfAsync(stream, fileName);

                // Step 2: Extract data using LLM
                progress.Status = BulkUploadStatus.Extracting;
                progress.Message = "Extracting invoice data...";
                progressCallback(progress);

                var extractedData = await _llmService.ExtractInvoiceDataAsync(pdfPath);

                if (extractedData == null)
                {
                    progress.Status = BulkUploadStatus.Failed;
                    progress.Message = "Failed to extract invoice data";
                    progressCallback(progress);
                    
                    // Clean up the uploaded PDF
                    await _storageService.DeletePdfAsync(pdfPath);
                    continue;
                }

                // Step 3: Validate and add invoice
                var invoiceType = ParseInvoiceType(extractedData.Type);

                // Validation for Material invoices
                if (invoiceType == InvoiceType.Material)
                {
                    var itemsTotal = extractedData.Items.Sum(i => i.Pretax + i.Tax);
                    var tolerance = 0.01m; // Allow small rounding differences

                    if (Math.Abs(itemsTotal - extractedData.Amount) > tolerance)
                    {
                        progress.Status = BulkUploadStatus.Failed;
                        progress.Message = $"Validation failed: Items total ({itemsTotal:N2}) does not match invoice amount ({extractedData.Amount:N2})";
                        progressCallback(progress);
                        
                        // Clean up the uploaded PDF
                        await _storageService.DeletePdfAsync(pdfPath);
                        continue;
                    }
                }

                // Create and add invoice
                var invoice = new Invoice
                {
                    ReimbursementPlanId = reimbursementPlanId,
                    Serial = extractedData.Serial,
                    Amount = extractedData.Amount,
                    Date = extractedData.Date,
                    Type = invoiceType,
                    PdfFilePath = pdfPath,
                    PayerId = payerId
                };

                // Add invoice items
                foreach (var itemData in extractedData.Items)
                {
                    invoice.InvoiceItems.Add(new InvoiceItem
                    {
                        Name = itemData.Name,
                        Specification = itemData.Specification,
                        Unit = itemData.Unit,
                        Amount = itemData.Quantity,
                        Pretax = itemData.Pretax,
                        Tax = itemData.Tax
                    });
                }

                _dbContext.Invoices.Add(invoice);
                await _dbContext.SaveChangesAsync();

                progress.Status = BulkUploadStatus.Added;
                progress.Message = $"Successfully added (Type: {invoiceType}, Amount: ${extractedData.Amount:N2})";
                progressCallback(progress);

                _logger.LogInformation("Invoice from {FileName} added successfully to plan {PlanId}", 
                    fileName, reimbursementPlanId);
            }
            catch (Exception ex)
            {
                progress.Status = BulkUploadStatus.Failed;
                progress.Message = $"Error: {ex.Message}";
                progressCallback(progress);
                
                _logger.LogError(ex, "Error processing bulk upload for file {FileName}", fileName);
            }
        }
    }

    private InvoiceType ParseInvoiceType(string type)
    {
        return type?.ToLower() switch
        {
            "material" => InvoiceType.Material,
            "travel" => InvoiceType.Travel,
            _ => InvoiceType.Others
        };
    }
}
