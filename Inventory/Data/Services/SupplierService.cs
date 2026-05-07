// Alternative: If you don't want to add UpdatedAt property, use this version of SupplierService
using System;
using System.Collections.Generic;
using System.Linq;
using Inventory.Data.IServices;
using Inventory.Models;

namespace Inventory.Data.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Supplier> GetAllSuppliers()
        {
            return _context.Suppliers.ToList();
        }

        public Supplier? GetSupplierById(int id)
        {
            return _context.Suppliers.Find(id);
        }

        public bool CreateSupplier(Supplier supplier)
        {
            try
            {
                supplier.CreatedAt = DateTime.Now;
                _context.Suppliers.Add(supplier);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating supplier: {ex.Message}");
                return false;
            }
        }

        public bool UpdateSupplier(Supplier supplier)
        {
            try
            {
                // Remove the UpdatedAt line if the property doesn't exist
                _context.Suppliers.Update(supplier);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating supplier: {ex.Message}");
                return false;
            }
        }

        public bool DeleteSupplier(int id)
        {
            try
            {
                var supplier = GetSupplierById(id);
                if (supplier != null)
                {
                    _context.Suppliers.Remove(supplier);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting supplier: {ex.Message}");
                return false;
            }
        }

        public bool IsSupplierNameUnique(string supplierName, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(supplierName))
                return false;

            return !_context.Suppliers.Any(s => s.SupplierName.ToLower() == supplierName.ToLower()
                && !s.isDelete
                && (excludeId == null || s.SupplierId != excludeId));
        }

        public bool IsContactNoUnique(string contactNo, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(contactNo))
                return true;

            return !_context.Suppliers.Any(s => s.ContactNo == contactNo
                && !s.isDelete
                && (excludeId == null || s.SupplierId != excludeId));
        }

        public int GetSupplierCount()
        {
            return _context.Suppliers.Count(s => !s.isDelete);
        }

        public IEnumerable<Supplier> GetActiveSuppliers()
        {
            return _context.Suppliers.Where(s => !s.isDelete && s.isActive).ToList();
        }

        public IEnumerable<Supplier> GetDeletedSuppliers()
        {
            return _context.Suppliers.Where(s => s.isDelete).ToList();
        }

        public bool SoftDeleteSupplier(int id)
        {
            try
            {
                var supplier = GetSupplierById(id);
                if (supplier != null && !supplier.isDelete)
                {
                    supplier.isDelete = true;
                    supplier.isActive = false;
                    // Remove this line if UpdatedAt doesn't exist
                    // supplier.UpdatedAt = DateTime.Now;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error soft deleting supplier: {ex.Message}");
                return false;
            }
        }

        public bool RestoreSupplier(int id)
        {
            try
            {
                var supplier = GetSupplierById(id);
                if (supplier != null && supplier.isDelete)
                {
                    supplier.isDelete = false;
                    supplier.isActive = true;
                    // Remove this line if UpdatedAt doesn't exist
                    // supplier.UpdatedAt = DateTime.Now;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring supplier: {ex.Message}");
                return false;
            }
        }
    }
}