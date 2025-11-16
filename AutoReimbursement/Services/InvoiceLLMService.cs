namespace AutoReimbursement.Services;

/// <summary>
/// Placeholder implementation for LLM-based invoice extraction.
/// This should be replaced with actual LLM integration (e.g., OpenAI, Azure AI, etc.)
/// </summary>
public class InvoiceLLMService : IInvoiceLLMService
{
    private readonly ILogger<InvoiceLLMService> _logger;

    public InvoiceLLMService(ILogger<InvoiceLLMService> logger)
    {
        _logger = logger;
    }

    public async Task<ExtractedInvoiceData> ExtractInvoiceDataAsync(Stream pdfStream)
    {
        _logger.LogWarning("Using placeholder LLM service. Implement actual LLM integration for production use.");
        
        // This is a placeholder implementation
        // In production, this should:
        // 1. Convert PDF to text/images
        // 2. Call LLM API (e.g., OpenAI GPT-4 Vision, Azure Document Intelligence)
        // 3. Parse and structure the response
        // 4. Return extracted data
        
        await Task.Delay(100); // Simulate async operation
        
        return new ExtractedInvoiceData
        {
            Serial = "PLACEHOLDER-001",
            Date = DateTime.Today,
            Amount = 0,
            Items = new List<ExtractedInvoiceItem>
            {
                new ExtractedInvoiceItem
                {
                    Name = "Sample Item",
                    Specification = "N/A",
                    Unit = "pcs",
                    Quantity = 1,
                    Pretax = 0,
                    Tax = 0
                }
            }
        };
    }
}
