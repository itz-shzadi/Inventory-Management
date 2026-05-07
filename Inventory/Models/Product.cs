using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [Column("ProductId")]
        public int ProductId { get; set; }

        [Required]
        [Column("ProductName")]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Column("CategoryId")]
        public int CategoryId { get; set; }

        // Ye property sirf ek baar define karo
        [Column("SupplierId")]
        public int SupplierId { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; } = 0;

        [Column("Unit")]
        public string? Unit { get; set; }

        [Column("SalePrice")]
        public decimal SalePrice { get; set; } = 0;

        [Column("PurchasePrice")]
        public decimal PurchasePrice { get; set; } = 0;

        [Column("Description")]
        public string? Description { get; set; }

        [Column("isActive")]
        public bool isActive { get; set; } = true;

        [Column("isDelete")]
        public bool isDelete { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties - Sirf yahan ek baar define karo
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<StockIn>? StockIns { get; set; }
        public virtual ICollection<StockOut>? StockOuts { get; set; }
    }
}