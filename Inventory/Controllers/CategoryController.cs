using Inventory.Data.IServices;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Inventory.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(IUnitOfWork unitOfWork, ILogger<CategoryController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Category/GetAllCategories
        [HttpGet]
        public IActionResult GetAllCategories()
        {
            try
            {
                var categories = _unitOfWork.CategoryService.GetAllCategories()
                    .Where(c => !c.isDelete)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Status
                    })
                    .ToList();

                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllCategories");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // GET: Category/GetByStatus
        [HttpGet]
        public IActionResult GetByStatus(string status)
        {
            try
            {
                if (string.IsNullOrEmpty(status))
                {
                    return GetAllCategories();
                }

                var categories = _unitOfWork.CategoryService.GetAllCategories()
                    .Where(c => !c.isDelete && c.Status == status)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Status
                    })
                    .ToList();

                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByStatus");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // GET: Category/Search
        [HttpGet]
        public IActionResult Search(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return GetAllCategories();
                }

                var categories = _unitOfWork.CategoryService.GetAllCategories()
                    .Where(c => !c.isDelete && 
                        (c.Name.Contains(searchTerm) || 
                         (c.Description != null && c.Description.Contains(searchTerm))))
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Status
                    })
                    .ToList();

                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // GET: Category/Details/5
        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                var category = _unitOfWork.CategoryService.GetCategoryById(id);
                if (category == null || category.isDelete)
                    return Json(new { success = false, message = "Category not found" });

                return Json(new
                {
                    success = true,
                    id = category.Id,
                    name = category.Name,
                    description = category.Description,
                    status = category.Status
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
        public IActionResult Create([FromBody] Category category)
        {
            try
            {
                _logger.LogInformation($"Create called with: {JsonSerializer.Serialize(category)}");

                if (category == null)
                {
                    _logger.LogWarning("Category is null");
                    return Json(new { success = false, message = "Invalid category data" });
                }

                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    return Json(new { success = false, message = "Category name is required" });
                }

                // Check duplicate
                if (!_unitOfWork.CategoryService.IsCategoryNameUnique(category.Name))
                {
                    return Json(new { success = false, message = "Category name already exists" });
                }

                // Rest of your code...

                var success = _unitOfWork.CategoryService.CreateCategory(category);

                if (success)
                {
                    _logger.LogInformation($"Category saved successfully with ID: {category.Id}");
                    // Make sure you're returning JSON with success=true
                    return Json(new { success = true, message = "Category created successfully!" });
                }
                else
                {
                    _logger.LogError("CreateCategory returned false");
                    return Json(new { success = false, message = "Failed to create category" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create Category");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        // POST: Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Category category)
        {
            try
            {
                _logger.LogInformation($"Edit called with: {JsonSerializer.Serialize(category)}");

                // Validation
                if (category == null || category.Id == 0)
                {
                    return Json(new { success = false, message = "Invalid category data" });
                }

                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    return Json(new { success = false, message = "Category name is required" });
                }

                if (category.Name.Length < 2)
                {
                    return Json(new { success = false, message = "Category name must be at least 2 characters" });
                }

                // Get existing category
                var existingCategory = _unitOfWork.CategoryService.GetCategoryById(category.Id);
                if (existingCategory == null || existingCategory.isDelete)
                {
                    return Json(new { success = false, message = "Category not found" });
                }

                // Check duplicate name (excluding current category)
                var duplicateCheck = _unitOfWork.CategoryService.GetAllCategories()
                    .Any(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id && !c.isDelete);

                if (duplicateCheck)
                {
                    return Json(new { success = false, message = "Category name already exists" });
                }

                // Update properties
                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.Status = category.Status;
                existingCategory.UpdatedAt = DateTime.Now;

                var success = _unitOfWork.CategoryService.UpdateCategory(existingCategory);

                if (success)
                {
                    _logger.LogInformation($"Category updated successfully with ID: {existingCategory.Id}");
                    return Json(new { success = true, message = "Category updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update category" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit Category");
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        // POST: Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var category = _unitOfWork.CategoryService.GetCategoryById(id);
                if (category == null || category.isDelete)
                    return Json(new { success = false, message = "Category not found" });

                category.isActive = false;
                category.isDelete = true;
                category.UpdatedAt = DateTime.Now;

                var success = _unitOfWork.CategoryService.UpdateCategory(category);

                if (success)
                    return Json(new { success = true, message = "Category deleted successfully" });
                else
                    return Json(new { success = false, message = "Failed to delete category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Category/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAll()
        {
            try
            {
                var categories = _unitOfWork.CategoryService.GetAllCategories()
                    .Where(c => !c.isDelete)
                    .ToList();

                if (!categories.Any())
                    return Json(new { success = false, message = "No categories to delete" });

                foreach (var category in categories)
                {
                    category.isActive = false;
                    category.isDelete = true;
                    category.UpdatedAt = DateTime.Now;
                    _unitOfWork.CategoryService.UpdateCategory(category);
                }

                return Json(new { success = true, message = "All categories deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAll");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Category/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restore(int id)
        {
            try
            {
                var category = _unitOfWork.CategoryService.GetCategoryById(id);
                if (category == null)
                    return Json(new { success = false, message = "Category not found" });

                category.isActive = true;
                category.isDelete = false;
                category.UpdatedAt = DateTime.Now;

                var success = _unitOfWork.CategoryService.UpdateCategory(category);

                if (success)
                    return Json(new { success = true, message = "Category restored successfully" });
                else
                    return Json(new { success = false, message = "Failed to restore category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Restore");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Category/PermanentDelete/5 - FIXED: Passing ID instead of Category object
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PermanentDelete(int id)
        {
            try
            {
                var category = _unitOfWork.CategoryService.GetCategoryById(id);
                if (category == null)
                    return Json(new { success = false, message = "Category not found" });

                // FIXED: Pass the ID (int) instead of the category object
                var success = _unitOfWork.CategoryService.DeleteCategory(category.Id);

                if (success)
                    return Json(new { success = true, message = "Category permanently deleted" });
                else
                    return Json(new { success = false, message = "Failed to permanently delete category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PermanentDelete");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Category/GetCount
        [HttpGet]
        public IActionResult GetCount()
        {
            try
            {
                var count = _unitOfWork.CategoryService.GetCategoryCount();
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCount");
                return Json(new { success = false, count = 0 });
            }
        }

        // GET: Category/GetDeletedCategories
        [HttpGet]
        public IActionResult GetDeletedCategories()
        {
            try
            {
                var categories = _unitOfWork.CategoryService.GetAllCategories()
                    .Where(c => c.isDelete)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Status
                    })
                    .ToList();

                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDeletedCategories");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}