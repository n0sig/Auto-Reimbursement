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

    public async Task<ExtractedInvoiceData?> ExtractInvoiceDataAsync(string pdfPath)
    {
        _logger.LogInformation("Extracting invoice data from PDF: {PdfPath}", pdfPath);

        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("GOOGLE_API_KEY environment variable is not set.");

        var pdfStream = await _invoiceStorageService.GetPdfStreamAsync(pdfPath);
        var fileUri = await GeminiApi.UploadPdfAsync(pdfStream, apiKey);
        
        var extractedInvoiceData = await ExtractBasicsAsync(fileUri, apiKey);
        if (extractedInvoiceData == null)
        {
            _logger.LogWarning("Failed to extract basic invoice data.");
            return null;
        }

        if (extractedInvoiceData.Type == "Material")
            extractedInvoiceData.Items = await ExtractItemsAsync(fileUri, apiKey);

        return extractedInvoiceData;
    }
    
    private async Task<ExtractedInvoiceData?> ExtractBasicsAsync(string fileUri, string apiKey)
    {
        var response = await GeminiApi.GenerateContentAsync("gemini-flash-lite-latest", ExtractBasicsPrompt, [fileUri], apiKey);

        if (response == null)
        {
            _logger.LogWarning("No response received from LLM.");
            return null;
        }
        
        _logger.LogInformation("LLM Response: {Response}", response);
        response = response.Trim();
        try
        {
            // Legal JSON response
            if (!response.StartsWith('{') || !response.EndsWith('}'))
            {
                var regex = new System.Text.RegularExpressions.Regex("```json(.*?)```",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                var match = regex.Match(response);
                if (match.Success)
                    response = match.Groups[1].Value.Trim();
            }

            var extractedData = JsonSerializer.Deserialize<ExtractedInvoiceData>(response);
            if (extractedData != null)
            {
                return extractedData;
            }

            _logger.LogWarning("Failed to deserialize LLM response into ExtractedInvoiceData.");
            return null;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error for LLM response.");
            return null;
        }
    }
    
    private async Task<List<ExtractedInvoiceItem>> ExtractItemsAsync(string fileUri, string apiKey)
    {
        var response = await GeminiApi.GenerateContentAsync("gemini-flash-latest", ExtractItemsPrompt, [fileUri], apiKey, 4096);
        if (response == null)
        {
            _logger.LogWarning("No response received from LLM for invoice items.");
            return [];
        }

        _logger.LogInformation("LLM Response for invoice items: {Response}", response);
        response = response.Trim();
        try
        {
            // Legal JSON response
            if (!response.StartsWith('[') || !response.EndsWith(']'))
            {
                var regex = new System.Text.RegularExpressions.Regex("```json(.*?)```",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                var match = regex.Match(response);
                if (match.Success)
                    response = match.Groups[1].Value.Trim();
            }

            var extractedItems = JsonSerializer.Deserialize<List<ExtractedInvoiceItem>>(response);
            if (extractedItems != null)
                return extractedItems;
            
            _logger.LogWarning("Failed to deserialize LLM response into List<ExtractedInvoiceItem>.");
            return [];
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error for LLM response of invoice items.");
            return [];
        }
    }

    private const string ExtractBasicsPrompt = @"读这张发票，提取并整理其中的信息，以紧凑的JSON格式返回，去掉所有换行和空格。

JSON结构体应包含如下内容，其在发票中的对应项分别为：
 - Serial: 发票号码
 - Date: 开票日期，请转换为yyyy-mm-dd格式
 - Amount: 价税合计，请转换为数字格式
 - Type: 发票类型，可能的值有“Material”和“Travel”；请选择最符合本张发票的一个。
";

    private const string ExtractItemsPrompt = @"读这张发票，提取并整理其中的信息，以紧凑的JSON格式返回，去掉所有换行和空格，思考过程避免使用JSON。

JSON结构体应为一个包含有若干项目的数组，每个项目中应包含如下内容，它们在发票中的对应项为：
 - Name: 项目名称
 - Specification: 规格型号
 - Unit: 单位
 - Quantity: 数量，为数字格式或null
 - Pretax: 金额，为数字格式
 - Tax: 税额，为数字格式

注意以下几点：
 - 一张发票可能会有多页，读到“价税合计”时，代表当前发票结束。
 - 如果某一行的“金额”为空，则代表上一行的“项目名称”或“规格型号”溢出到了本行，请将两行合并；溢出的行可能会跨页，请注意合并；本条注意点先于下一条执行。
 - 如果某一行的“项目名称”与上一行相同且“金额”为负，则代表它是对上一行的补充。请将两行合并，金额进行相应的运算；跨页规则同样适用于这一条注意点。";
}

internal static class GeminiApi
{
    private static readonly HttpClient HttpClient = new();

    public static async Task<string> UploadPdfAsync(Stream pdfStream, string apiKey)
    {
        var uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";
        var displayName = Guid.NewGuid().ToString();

        using var startRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        startRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.Add("X-Goog-Upload-Command", "start");
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", pdfStream.Length.ToString());
        startRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", "application/pdf");

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
        uploadRequest.Content.Headers.ContentLength = pdfStream.Length;

        var uploadResponse = await HttpClient.SendAsync(uploadRequest);
        uploadResponse.EnsureSuccessStatusCode();

        // Parse the file URI from response
        var responseJson = await uploadResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseJson);
        var fileUri = jsonDoc.RootElement
            .GetProperty("file")
            .GetProperty("uri")
            .GetString();

        return fileUri ?? throw new Exception("Failed to extract file URI from response");
    }

    public static async Task<string?> GenerateContentAsync(string model, string prompt, List<string> fileUris, string apiKey, int thinkingBudget = 0)
    {
        var generateUrl =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var parts1 = fileUris
            .Select(fileUri => new { file_data = new { mime_type = "application/pdf", file_uri = fileUri } })
            .ToList<object>();
        parts1.Add(new { text = prompt });

        var requestBody = new
        {
            contents = new[] { new { parts = parts1 } },
            generationConfig = new
            {
                thinkingConfig = new
                {
                    thinkingBudget,
                },
                temperature = 0,
            }
        };

        var response = await HttpClient.PostAsJsonAsync(generateUrl, requestBody);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        try
        {
            var jsonDoc = JsonDocument.Parse(responseJson);
            var candidates = jsonDoc.RootElement.GetProperty("candidates");

            foreach (var part in candidates.EnumerateArray().SelectMany(candidate =>
                         candidate.GetProperty("content").GetProperty("parts").EnumerateArray()))
            {
                if (!part.TryGetProperty("text", out var textElement)) continue;
                var text = textElement.GetString();
                if (text == null) 
                    continue;
                return text;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not parse response text: {ex.Message}");
        }

        return null;
    }
}