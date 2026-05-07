using Inventory.Data.IServices;

namespace Inventory.Data.IServices
{
    public interface IUnitOfWork
    {
        IUserService UserService { get; }
        ICategoryService CategoryService { get; }
        ISupplierService SupplierService { get; }
        Task<bool> SaveAsync();
    }
}
