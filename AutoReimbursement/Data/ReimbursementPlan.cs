using System.ComponentModel.DataAnnotations;

namespace AutoReimbursement.Data;

public class ReimbursementPlan
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; }
    
    // Navigation properties
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
