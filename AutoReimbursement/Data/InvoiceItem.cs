using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoReimbursement.Data;

public class InvoiceItem
{
    public int Id { get; set; }
    
    public int InvoiceId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Specification { get; set; }
    
    [StringLength(50)]
    public string? Unit { get; set; }
    
    public decimal? Amount { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Pretax { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; set; }
    
    // Navigation property
    public Invoice Invoice { get; set; } = null!;
}
