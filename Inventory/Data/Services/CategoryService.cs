using Inventory.Data;
using Inventory.Data.IServices;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Data.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Category> GetAllCategories()
        {
            return _context.Categories
                .OrderByDescending(c => c.Id)
                .ToList();
        }

        public Category GetCategoryById(int id)
        {
            return _context.Categories.Find(id);
        }

        public bool IsCategoryNameUnique(string name, int? excludeId = null)
        {
            var query = _context.Categories.Where(c => c.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            return !query.Any();
        }

        public bool CreateCategory(Category category)
        {
            try
            {
                category.CreatedAt = DateTime.Now;
                category.UpdatedAt = DateTime.Now;
                category.isActive = true;
                category.isDelete = false;

                _context.Categories.Add(category);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateCategory(Category category)
        {
            try
            {
                category.UpdatedAt = DateTime.Now;
                _context.Categories.Update(category);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteCategory(int id)
        {
            try
            {
                var category = _context.Categories.Find(id);
                if (category == null)
                    return false;

                _context.Categories.Remove(category);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<Category> SearchCategories(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllCategories();

            searchTerm = searchTerm.ToLower();
            return _context.Categories
                .Where(c => c.Name.ToLower().Contains(searchTerm) ||
                           (c.Description != null && c.Description.ToLower().Contains(searchTerm)))
                .OrderByDescending(c => c.Id)
                .ToList();
        }

        public IEnumerable<Category> GetCategoriesByStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return GetAllCategories();

            return _context.Categories
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.Id)
                .ToList();
        }

        public int GetCategoryCount()
        {
            return _context.Categories.Count(c => !c.isDelete);
        }
    }
}