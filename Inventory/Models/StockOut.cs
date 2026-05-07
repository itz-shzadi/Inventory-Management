using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Models
{
    public class StockOut
    {
        [Key]
        public int StockOutId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityRemoved { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Reason { get; set; }  // Made nullable

        [StringLength(500)]
        public string? Remarks { get; set; }  // Made nullable

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Product? Product { get; set; }
    }
}

// ============ REQUEST MODEL - Add this at the end of file ============
public class StockOutRequestModel
{
    public int ProductId { get; set; }
    public int QuantityRemoved { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
}