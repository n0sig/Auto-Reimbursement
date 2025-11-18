using System.Text;
using System.Text.Json;

namespace AutoReimbursement.Services;

public class InvoiceLLMService : IInvoiceLLMService
{
    private readonly ILogger<InvoiceLLMService> _logger;
    private readonly IInvoiceStorageService _invoiceStorageService;

    public InvoiceLLMService(ILogger<InvoiceLLMService> logger, IInvoiceStorageService invoiceStorageService)
    {
        _logger = logger;
        _invoiceStorageService = invoiceStorageService;
    }

    public async Task<ExtractedInvoiceData> ExtractInvoiceDataAsync(string pdfPath)
    {
        _logger.LogInformation("Extracting invoice data from PDF: {PdfPath}", pdfPath);
        
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "https://generativelanguage.googleapis.com";
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("GOOGLE_API_KEY environment variable is not set.");
        }

        var fileUri = await UploadPdfAsync(pdfPath, "example_invoice", baseUrl, apiKey);

        var response = await GenerateContentAsync(fileUri, apiKey);
        if (response != null)
        {
            _logger.LogInformation("LLM Response: {Response}", response);
            try
            {
                var extractedData = JsonSerializer.Deserialize<ExtractedInvoiceData>(response);
                if (extractedData != null)
                {
                    return extractedData;
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize LLM response into ExtractedInvoiceData.");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error for LLM response.");
            }
        }
        else
        {
            _logger.LogWarning("No response received from LLM.");
        }
        
        return new ExtractedInvoiceData();
    }

    private static readonly HttpClient HttpClient = new();
    private async Task<string> UploadPdfAsync(string pdfPath, string displayName, string baseUrl, string apiKey)
    {
        var fileName = $"{displayName}.pdf";

        // Download the PDF
        var pdfStream = await _invoiceStorageService.GetPdfStreamAsync(pdfPath);

        var mimeType = "application/pdf";
        var numBytes = pdfStream.Length;

        Console.WriteLine($"MIME_TYPE: {mimeType}");
        Console.WriteLine($"NUM_BYTES: {numBytes}");

        // Initial resumable request
        var uploadUrl = $"{baseUrl}/upload/v1beta/files?key={apiKey}";
        
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        startRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.Add("X-Goog-Upload-Command", "start");
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", numBytes.ToString());
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);
        
        var startPayload = new { file = new { display_name = displayName } };
        startRequest.Content = new StringContent(
            JsonSerializer.Serialize(startPayload),
            Encoding.UTF8,
            "application/json"
        );

        var startResponse = await HttpClient.SendAsync(startRequest);
        startResponse.EnsureSuccessStatusCode();

        // Extract upload URL from response headers
        var resumableUploadUrl = startResponse.Headers
            .GetValues("X-Goog-Upload-URL")
            .FirstOrDefault();

        if (string.IsNullOrEmpty(resumableUploadUrl))
        {
            throw new Exception("Failed to get upload URL from response headers");
        }

        // Upload the PDF
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, resumableUploadUrl);
        uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
        uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Content = new StreamContent(pdfStream);
        uploadRequest.Content.Headers.ContentLength = numBytes;

        var uploadResponse = await HttpClient.SendAsync(uploadRequest);
        uploadResponse.EnsureSuccessStatusCode();

        var responseJson = await uploadResponse.Content.ReadAsStringAsync();
        var fileInfoFileName = $"file_info_{displayName}.json";
        await File.WriteAllTextAsync(fileInfoFileName, responseJson);

        // Parse the file URI from response
        var jsonDoc = JsonDocument.Parse(responseJson);
        var fileUri = jsonDoc.RootElement
            .GetProperty("file")
            .GetProperty("uri")
            .GetString();

        Console.WriteLine($"file_uri for {displayName}: {fileUri}");
        
        return fileUri ?? throw new Exception("Failed to extract file URI from response");
    }
    
    static async Task<string?> GenerateContentAsync(string fileUri, string apiKey)
    {
        var generateUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { file_data = new { mime_type = "application/pdf", file_uri = fileUri } },
                        new { text = @"读这张发票，提取并整理其中的信息，以紧凑的JSON格式返回，去掉所有换行和空格，思考过程避免使用JSON。

JSON结构体中应包含如下内容，它们在发票中的对应项为：
 - Serial: 发票号码
 - Date: 开票日期，请转换为yyyy-mm-dd格式
 - Amount: 价税合计，请转换为数字格式
 - Items: 商品信息数组，有关商品信息的详细描述见后文。

每个商品信息对象应包含如下内容，它们在发票中的对应项为：
 - Name: 项目名称
 - Specification: 规格型号
 - Unit: 单位
 - Quantity: 数量
 - Pretax: 金额
 - Tax: 税额

注意以下几点：
 - 一张发票可能会有多页，读到“价税合计”时，代表当前发票结束。
 - 如果某一行的“金额”为空，则代表上一行的“项目名称”或“规格型号”溢出到了本行，请将两行合并；溢出的行可能会跨页，请注意合并；本条注意点先于下一条执行。
 - 如果某一行的“项目名称”与上一行相同且“金额”为负，则代表它是对上一行的补充。请将两行合并，金额进行相应的运算；跨页规则同样适用于这一条注意点。" }
                    }
                }
            },
            generationConfig = new
            {
                thinkingConfig = new
                {
                    thinkingBudget = 1024,
                },
                temperature = 0,
            }
        };

        var response = await HttpClient.PostAsJsonAsync(generateUrl, requestBody);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync("response.json", responseJson);

        // Extract and display text from candidates
        try
        {
            var jsonDoc = JsonDocument.Parse(responseJson);
            var candidates = jsonDoc.RootElement.GetProperty("candidates");
            
            foreach (var candidate in candidates.EnumerateArray())
            {
                var parts = candidate.GetProperty("content").GetProperty("parts");
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textElement))
                    {
                        var text = textElement.GetString();
                        if (text != null)
                        {
                            if (text.StartsWith("{") && text.EndsWith("}"))
                            {
                                return text.Trim();
                            }
                            var regex = new System.Text.RegularExpressions.Regex("```json(.*?)```",
                                System.Text.RegularExpressions.RegexOptions.Singleline);
                            var match = regex.Match(text);
                            if (match.Success)
                            {
                                return match.Groups[1].Value.Trim();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not parse response text: {ex.Message}");
        }
        
        return null;
    }
}
