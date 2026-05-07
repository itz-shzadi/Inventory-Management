using System.Collections.Generic;
using Inventory.Models;

namespace Inventory.Data.IServices
{
    public interface ISupplierService
    {
        // Basic CRUD
        IEnumerable<Supplier> GetAllSuppliers();
        Supplier GetSupplierById(int id);
        bool CreateSupplier(Supplier supplier);
        bool UpdateSupplier(Supplier supplier);
        bool DeleteSupplier(int id); // Permanent delete

        // Additional methods
        bool IsSupplierNameUnique(string supplierName, int? excludeId = null);
        bool IsContactNoUnique(string contactNo, int? excludeId = null);
        int GetSupplierCount();
        IEnumerable<Supplier> GetActiveSuppliers();
        IEnumerable<Supplier> GetDeletedSuppliers();
        bool SoftDeleteSupplier(int id);
        bool RestoreSupplier(int id);
    }
}