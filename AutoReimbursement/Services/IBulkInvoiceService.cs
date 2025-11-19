using Microsoft.AspNetCore.Components.Forms;

namespace AutoReimbursement.Services;

public enum BulkUploadStatus
{
    Pending,
    Uploading,
    Extracting,
    Added,
    Failed
}

public class BulkUploadProgress
{
    public string FileName { get; set; } = string.Empty;
    public BulkUploadStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class FileData
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

public interface IBulkInvoiceService
{
    /// <summary>
    /// Process multiple PDFs with automatic invoice recognition and addition
    /// </summary>
    Task ProcessBulkUploadAsync(
        List<FileData> files,
        string payerId,
        int reimbursementPlanId,
        Action<BulkUploadProgress> progressCallback);
}
