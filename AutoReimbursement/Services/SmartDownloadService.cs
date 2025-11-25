using Microsoft.JSInterop;

namespace AutoReimbursement.Services;

/// <summary>
/// Service for downloading files using the Smart Download Manager.
/// Implements smart download logic with auto-compression for multiple files.
/// </summary>
public interface ISmartDownloadService
{
    /// <summary>
    /// Downloads files using smart logic (3+ files = ZIP, otherwise independent downloads)
    /// </summary>
    /// <param name="files">List of files to download</param>
    /// <param name="archiveName">Name of the ZIP archive if compression is used</param>
    /// <param name="compressionThreshold">Number of files threshold for compression (default: 3)</param>
    Task SmartDownloadAsync(List<DownloadFileInfo> files, string archiveName = "documents.zip", int compressionThreshold = 3);
    
    /// <summary>
    /// Downloads a single file from base64 content
    /// </summary>
    /// <param name="base64Content">Base64 encoded file content</param>
    /// <param name="fileName">Name of the file to download</param>
    /// <param name="contentType">MIME type of the file</param>
    Task DownloadFromBase64Async(string base64Content, string fileName, string contentType);
    
    /// <summary>
    /// Downloads generated documents using smart download logic
    /// </summary>
    /// <param name="documents">List of generated documents to download</param>
    /// <param name="archiveName">Name of the ZIP archive if compression is used</param>
    Task DownloadDocumentsAsync(List<GeneratedDocument> documents, string archiveName = "documents.zip");
}

/// <summary>
/// File information for download
/// </summary>
public class DownloadFileInfo
{
    /// <summary>
    /// The file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The file content as bytes
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// The MIME content type
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Implementation of Smart Download Service using JavaScript interop
/// </summary>
public class SmartDownloadService : ISmartDownloadService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<SmartDownloadService> _logger;
    private const int DefaultCompressionThreshold = 3;

    public SmartDownloadService(IJSRuntime jsRuntime, ILogger<SmartDownloadService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SmartDownloadAsync(List<DownloadFileInfo> files, string archiveName = "documents.zip", int compressionThreshold = DefaultCompressionThreshold)
    {
        if (files == null || files.Count == 0)
        {
            _logger.LogWarning("No files provided for download");
            return;
        }

        try
        {
            // Convert files to JS-compatible format (base64 encoded)
            var jsFiles = files.Select(f => new
            {
                fileName = f.FileName,
                content = Convert.ToBase64String(f.Content),
                contentType = f.ContentType
            }).ToArray();

            if (files.Count >= compressionThreshold)
            {
                _logger.LogInformation("Downloading {Count} files as ZIP archive: {ArchiveName}", files.Count, archiveName);
                await _jsRuntime.InvokeVoidAsync("smartDownloadFromBase64", jsFiles, archiveName, compressionThreshold);
            }
            else
            {
                _logger.LogInformation("Downloading {Count} files independently", files.Count);
                foreach (var file in jsFiles)
                {
                    await _jsRuntime.InvokeVoidAsync("downloadFromBase64", file.content, file.fileName, file.contentType);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during smart download");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DownloadFromBase64Async(string base64Content, string fileName, string contentType)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("downloadFromBase64", base64Content, fileName, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileName}", fileName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DownloadDocumentsAsync(List<GeneratedDocument> documents, string archiveName = "documents.zip")
    {
        if (documents == null || documents.Count == 0)
        {
            _logger.LogWarning("No documents provided for download");
            return;
        }

        var files = documents.Select(d => new DownloadFileInfo
        {
            FileName = d.FileName,
            Content = d.Content,
            ContentType = d.ContentType
        }).ToList();

        await SmartDownloadAsync(files, archiveName);
    }
}
