using Inventory.Data.IServices;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Inventory.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(ISupplierService supplierService, ILogger<SupplierController> logger)
        {
            _supplierService = supplierService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAllSuppliers()
        {
            try
            {
                // FIX: Check if service is null
                if (_supplierService == null)
                {
                    _logger.LogError("SupplierService is null!");
                    return Json(new List<object>());
                }

                var allSuppliers = _supplierService.GetAllSuppliers();

                // FIX: Check if result is null
                if (allSuppliers == null)
                {
                    return Json(new List<object>());
                }

                var suppliers = allSuppliers
                    .Where(s => s != null && !s.isDelete)
                    .Select(s => new
                    {
                        s.SupplierId,
                        s.SupplierName,
                        s.ContactNo,
                        s.Email,
                        s.Address,
                        s.isActive
                    })
                    .ToList();

                return Json(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSuppliers");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                if (_supplierService == null)
                {
                    return Json(new { success = false, message = "Service not available" });
                }

                var supplier = _supplierService.GetSupplierById(id);
                if (supplier == null || supplier.isDelete)
                    return Json(new { success = false, message = "Supplier not found" });

                return Json(new
                {
                    success = true,
                    supplierId = supplier.SupplierId,
                    supplierName = supplier.SupplierName,
                    contactNo = supplier.ContactNo,
                    email = supplier.Email,
                    address = supplier.Address,
                    isActive = supplier.isActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Details");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] Supplier supplier)
        {
            try
            {
                _logger.LogInformation($"Create called with: {JsonSerializer.Serialize(supplier)}");

                // FIX: Check service null
                if (_supplierService == null)
                {
                    return Json(new { success = false, message = "Service not available" });
                }

                if (supplier == null)
                    return Json(new { success = false, message = "Invalid supplier data" });

                // Validation
                if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                    return Json(new { success = false, message = "Supplier name is required" });

                if (supplier.SupplierName.Length < 2)
                    return Json(new { success = false, message = "Supplier name must be at least 2 characters" });

                if (string.IsNullOrWhiteSpace(supplier.ContactNo))
                    return Json(new { success = false, message = "Contact number is required" });

                var cleanContact = new string(supplier.ContactNo.Where(c => char.IsDigit(c)).ToArray());
                if (cleanContact.Length < 10)
                    return Json(new { success = false, message = "Contact number must have at least 10 digits" });

                if (!string.IsNullOrWhiteSpace(supplier.Email))
                {
                    if (!IsValidEmail(supplier.Email))
                        return Json(new { success = false, message = "Invalid email format" });
                }

                // FIX: Safe null check for GetAllSuppliers
                var allSuppliers = _supplierService.GetAllSuppliers();
                if (allSuppliers != null)
                {
                    // Check duplicate name
                    var existingByName = allSuppliers
                        .FirstOrDefault(s => s != null && s.SupplierName != null &&
                            s.SupplierName.ToLower() == supplier.SupplierName.ToLower() && !s.isDelete);
                    if (existingByName != null)
                        return Json(new { success = false, message = "Supplier name already exists" });

                    // Check duplicate contact
                    var existingByContact = allSuppliers
                        .FirstOrDefault(s => s != null && s.ContactNo != null &&
                            s.ContactNo.Replace(" ", "").Replace("-", "").Replace("+", "") ==
                            supplier.ContactNo.Replace(" ", "").Replace("-", "").Replace("+", "") && !s.isDelete);
                    if (existingByContact != null)
                        return Json(new { success = false, message = "Contact number already exists" });
                }

                supplier.isActive = true;
                supplier.isDelete = false;
                supplier.CreatedAt = DateTime.Now;
                supplier.UpdatedAt = DateTime.Now;

                var success = _supplierService.CreateSupplier(supplier);

                if (success)
                {
                    return Json(new { success = true, message = "Supplier created successfully!", supplierId = supplier.SupplierId });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create supplier" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create Supplier");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Supplier supplier)
        {
            try
            {
                if (_supplierService == null)
                {
                    return Json(new { success = false, message = "Service not available" });
                }

                if (supplier == null || supplier.SupplierId == 0)
                    return Json(new { success = false, message = "Invalid supplier data" });

                var existingSupplier = _supplierService.GetSupplierById(supplier.SupplierId);
                if (existingSupplier == null || existingSupplier.isDelete)
                    return Json(new { success = false, message = "Supplier not found" });

                if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                    return Json(new { success = false, message = "Supplier name is required" });

                if (supplier.SupplierName.Length < 2)
                    return Json(new { success = false, message = "Supplier name must be at least 2 characters" });

                if (string.IsNullOrWhiteSpace(supplier.ContactNo))
                    return Json(new { success = false, message = "Contact number is required" });

                var cleanContact = new string(supplier.ContactNo.Where(c => char.IsDigit(c)).ToArray());
                if (cleanContact.Length < 10)
                    return Json(new { success = false, message = "Contact number must have at least 10 digits" });

                if (!string.IsNullOrWhiteSpace(supplier.Email) && !IsValidEmail(supplier.Email))
                    return Json(new { success = false, message = "Invalid email format" });

                // FIX: Safe null check for GetAllSuppliers
                var allSuppliers = _supplierService.GetAllSuppliers();
                if (allSuppliers != null)
                {
                    var duplicateByName = allSuppliers
                        .FirstOrDefault(s => s != null && s.SupplierName != null &&
                            s.SupplierName.ToLower() == supplier.SupplierName.ToLower() &&
                            s.SupplierId != supplier.SupplierId && !s.isDelete);
                    if (duplicateByName != null)
                        return Json(new { success = false, message = "Supplier name already exists" });

                    var duplicateByContact = allSuppliers
                        .FirstOrDefault(s => s != null && s.ContactNo != null &&
                            s.ContactNo.Replace(" ", "").Replace("-", "").Replace("+", "") ==
                            supplier.ContactNo.Replace(" ", "").Replace("-", "").Replace("+", "") &&
                            s.SupplierId != supplier.SupplierId && !s.isDelete);
                    if (duplicateByContact != null)
                        return Json(new { success = false, message = "Contact number already exists" });
                }

                existingSupplier.SupplierName = supplier.SupplierName;
                existingSupplier.ContactNo = supplier.ContactNo;
                existingSupplier.Email = string.IsNullOrWhiteSpace(supplier.Email) ? null : supplier.Email;
                existingSupplier.Address = string.IsNullOrWhiteSpace(supplier.Address) ? null : supplier.Address;
                existingSupplier.UpdatedAt = DateTime.Now;

                var success = _supplierService.UpdateSupplier(existingSupplier);

                if (success)
                {
                    return Json(new { success = true, message = "Supplier updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update supplier" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit Supplier");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                if (_supplierService == null)
                {
                    return Json(new { success = false, message = "Service not available" });
                }

                var supplier = _supplierService.GetSupplierById(id);
                if (supplier == null || supplier.isDelete)
                    return Json(new { success = false, message = "Supplier not found" });

                var success = _supplierService.SoftDeleteSupplier(id);

                if (success)
                    return Json(new { success = true, message = "Supplier deleted successfully" });
                else
                    return Json(new { success = false, message = "Failed to delete supplier" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAll()
        {
            try
            {
                if (_supplierService == null)
                {
                    return Json(new { success = false, message = "Service not available" });
                }

                var allSuppliers = _supplierService.GetAllSuppliers();
                if (allSuppliers == null)
                {
                    return Json(new { success = false, message = "No suppliers found" });
                }

                var suppliers = allSuppliers.Where(s => s != null && !s.isDelete).ToList();

                if (!suppliers.Any())
                    return Json(new { success = false, message = "No suppliers to delete" });

                foreach (var supplier in suppliers)
                {
                    _supplierService.SoftDeleteSupplier(supplier.SupplierId);
                }

                return Json(new { success = true, message = "All suppliers deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAll");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetCount()
        {
            try
            {
                if (_supplierService == null)
                {
                    return Json(new { success = false, count = 0 });
                }

                var allSuppliers = _supplierService.GetAllSuppliers();
                if (allSuppliers == null)
                {
                    return Json(new { success = true, count = 0 });
                }

                var count = allSuppliers.Count(s => s != null && !s.isDelete);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCount");
                return Json(new { success = false, count = 0 });
            }
        }

        [HttpGet]
        public IActionResult Search(string searchTerm)
        {
            try
            {
                if (_supplierService == null)
                {
                    return Json(new List<object>());
                }

                if (string.IsNullOrWhiteSpace(searchTerm))
                    return GetAllSuppliers();

                var allSuppliers = _supplierService.GetAllSuppliers();
                if (allSuppliers == null)
                {
                    return Json(new List<object>());
                }

                var suppliers = allSuppliers
                    .Where(s => s != null && !s.isDelete &&
                        (s.SupplierName != null && s.SupplierName.ToLower().Contains(searchTerm.ToLower()) ||
                         s.ContactNo != null && s.ContactNo.Contains(searchTerm) ||
                         (s.Email != null && s.Email.ToLower().Contains(searchTerm.ToLower())) ||
                         (s.Address != null && s.Address.ToLower().Contains(searchTerm.ToLower()))))
                    .Select(s => new
                    {
                        s.SupplierId,
                        s.SupplierName,
                        s.ContactNo,
                        s.Email,
                        s.Address,
                        s.isActive
                    })
                    .ToList();

                return Json(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return Json(new List<object>());
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return true;

                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}