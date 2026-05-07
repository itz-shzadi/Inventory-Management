using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Models
{
    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        [Column("SupplierId")]
        public int SupplierId { get; set; }

        [Required]
        [Column("SupplierName")]
        [StringLength(200)]
        public string SupplierName { get; set; } = string.Empty;

        [Column("ContactNo")]
        [StringLength(20)]
        public string? ContactNo { get; set; }

        [Column("Email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("Address")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Column("isActive")]
        public bool isActive { get; set; } = true;

        [Column("isDelete")]
        public bool isDelete { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property - Ye hona chahiye
        public virtual ICollection<Product>? Products { get; set; }
    }
}