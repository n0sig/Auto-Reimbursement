using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoReimbursement.Data;

public class Invoice
{
    public int Id { get; set; }
    
    public int ReimbursementPlanId { get; set; }
    
    [StringLength(100)]
    public string? Serial { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime? Date { get; set; }
    
    [Required]
    public InvoiceType Type { get; set; }
    
    [StringLength(500)]
    public string? PdfFilePath { get; set; }
    
    public string? PayerId { get; set; }
    
    // Navigation properties
    public ReimbursementPlan ReimbursementPlan { get; set; } = null!;
    public ApplicationUser? Payer { get; set; }
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
