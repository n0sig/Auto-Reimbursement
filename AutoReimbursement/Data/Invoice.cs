using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoReimbursement.Data;

public class Invoice
{
    public int Id { get; set; }
    
    public int ReimbursementPlanId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime Date { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    // Navigation property
    public ReimbursementPlan ReimbursementPlan { get; set; } = null!;
}
