using Inventory.Models;
using System.Collections.Generic;

namespace Inventory.Data.IServices
{
    public interface ICategoryService
    {
        IEnumerable<Category> GetAllCategories();
        Category GetCategoryById(int id);
        bool IsCategoryNameUnique(string name, int? excludeId = null);
        bool CreateCategory(Category category);
        bool UpdateCategory(Category category);
        bool DeleteCategory(int id);
        IEnumerable<Category> SearchCategories(string searchTerm);
        IEnumerable<Category> GetCategoriesByStatus(string status);
        int GetCategoryCount();
    }
}