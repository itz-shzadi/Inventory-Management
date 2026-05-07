using System;
using System.ComponentModel.DataAnnotations;

namespace Inventory.DTOs
{
    // Base DTO - for basic supplier info
    public class SupplierDto
    {
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 100 characters")]
        public string SupplierName { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Contact number must be between 10 and 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string ContactNo { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }
    }

    // Create Supplier DTO
    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 100 characters")]
        public string SupplierName { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Contact number must be between 10 and 20 characters")]
        [RegularExpression(@"^[0-9+\-\s]+$", ErrorMessage = "Contact number can only contain digits, spaces, +, and -")]
        public string ContactNo { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Update Supplier DTO
    public class UpdateSupplierDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 100 characters")]
        public string SupplierName { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Contact number must be between 10 and 20 characters")]
        [RegularExpression(@"^[0-9+\-\s]+$", ErrorMessage = "Contact number can only contain digits, spaces, +, and -")]
        public string ContactNo { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        public bool IsActive { get; set; }
    }

    // Response Supplier DTO (for API responses)
    public class SupplierResponseDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContactNo { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ProductCount { get; set; } // Optional: count of products from this supplier
    }

    // Supplier List DTO (for table display)
    public class SupplierListDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContactNo { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string StatusBadgeClass => IsActive ? "badge-success" : "badge-danger";
        public string FormattedContact => ContactNo;
        public string TruncatedAddress => Address?.Length > 40 ? Address.Substring(0, 40) + "..." : Address;
    }

    // Supplier Statistics DTO
    public class SupplierStatisticsDto
    {
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public int DeletedSuppliers { get; set; }
    }

    // Supplier Contact Info DTO (for dropdowns/select lists)
    public class SupplierContactInfoDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContactNo { get; set; }
        public string DisplayText => $"{SupplierName} - {ContactNo}";
    }
}