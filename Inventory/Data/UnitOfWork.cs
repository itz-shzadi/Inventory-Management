using Inventory.Data.IServices;
using Inventory.Data.Services;

namespace Inventory.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IUserService _userService;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IUserService UserService => _userService ??= new UserServices(_context);
        public ICategoryService CategoryService => new CategoryService(_context);
        public ISupplierService SupplierService => new SupplierService(_context);
        public async Task<bool> SaveAsync()
        {
            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}