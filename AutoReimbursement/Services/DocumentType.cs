namespace AutoReimbursement.Services;

/// <summary>
/// Supported document types for generation
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Warehouse In/Out Document (入库单/出库单)
    /// </summary>
    WarehouseInOut,
    
    /// <summary>
    /// Purchase Registration Document (采购登记表)
    /// </summary>
    PurchaseRegistration,
    
    /// <summary>
    /// Invoice PDF (Future support)
    /// </summary>
    InvoicePdf
}
