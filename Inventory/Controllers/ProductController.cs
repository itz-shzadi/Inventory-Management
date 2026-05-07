using Inventory.Data.IServices;
using Inventory.DTOs;
using Inventory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Inventory.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ISupplierService _supplierService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            ICategoryService categoryService,
            ISupplierService supplierService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _supplierService = supplierService;
            _logger = logger;
        }

        // Main view
        public IActionResult Index()
        {
            return View();
        }

        // Get all products
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            try
            {
                var products = _productService.GetAllProducts();
                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllProducts");
                return Json(new List<ProductDTO>());
            }
        }

        // Get product by id
        [HttpGet]
        public IActionResult GetProductById(int id)
        {
            try
            {
                var product = _productService.GetProductById(id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                return Json(new { success = true, product });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProductById");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Create product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] CreateProductDTO productDto)
        {
            try
            {
                if (productDto == null)
                    return Json(new { success = false, message = "Invalid product data" });

                // Validation
                if (string.IsNullOrWhiteSpace(productDto.ProductName))
                    return Json(new { success = false, message = "Product name is required" });

                if (productDto.ProductName.Length < 2)
                    return Json(new { success = false, message = "Product name must be at least 2 characters" });

                if (productDto.CategoryId <= 0)
                    return Json(new { success = false, message = "Category is required" });

                if (productDto.SupplierId <= 0)
                    return Json(new { success = false, message = "Supplier is required" });

                if (productDto.PurchasePrice <= 0)
                    return Json(new { success = false, message = "Purchase price must be greater than 0" });

                if (productDto.SalePrice <= 0)
                    return Json(new { success = false, message = "Sale price must be greater than 0" });

                if (productDto.SalePrice < productDto.PurchasePrice)
                    return Json(new { success = false, message = "Sale price cannot be less than purchase price" });

                if (!_productService.IsProductNameUnique(productDto.ProductName))
                    return Json(new { success = false, message = "Product name already exists" });

                var product = _productService.CreateProduct(productDto);
                return Json(new { success = true, message = "Product created successfully!", product });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create Product");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Update product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] UpdateProductDTO productDto)
        {
            try
            {
                if (productDto == null || productDto.ProductId == 0)
                    return Json(new { success = false, message = "Invalid product data" });

                var existingProduct = _productService.GetProductById(productDto.ProductId);
                if (existingProduct == null)
                    return Json(new { success = false, message = "Product not found" });

                // Validation
                if (string.IsNullOrWhiteSpace(productDto.ProductName))
                    return Json(new { success = false, message = "Product name is required" });

                if (productDto.ProductName.Length < 2)
                    return Json(new { success = false, message = "Product name must be at least 2 characters" });

                if (productDto.PurchasePrice <= 0)
                    return Json(new { success = false, message = "Purchase price must be greater than 0" });

                if (productDto.SalePrice <= 0)
                    return Json(new { success = false, message = "Sale price must be greater than 0" });

                if (productDto.SalePrice < productDto.PurchasePrice)
                    return Json(new { success = false, message = "Sale price cannot be less than purchase price" });

                if (!_productService.IsProductNameUnique(productDto.ProductName, productDto.ProductId))
                    return Json(new { success = false, message = "Product name already exists" });

                var product = _productService.UpdateProduct(productDto);
                return Json(new { success = true, message = "Product updated successfully!", product });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit Product");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Delete product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var success = _productService.DeleteProduct(id);
                if (success)
                    return Json(new { success = true, message = "Product deleted successfully" });
                else
                    return Json(new { success = false, message = "Failed to delete product" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Stock In
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStock([FromBody] CreateStockInDTO stockInDto)
        {
            try
            {
                if (stockInDto == null)
                    return Json(new { success = false, message = "Invalid stock data" });

                if (stockInDto.ProductId <= 0)
                    return Json(new { success = false, message = "Product is required" });

                if (stockInDto.SupplierId <= 0)
                    return Json(new { success = false, message = "Supplier is required" });

                if (stockInDto.QuantityAdded <= 0)
                    return Json(new { success = false, message = "Quantity must be greater than 0" });

                var success = _productService.AddStock(stockInDto);
                if (success)
                    return Json(new { success = true, message = "Stock added successfully!" });
                else
                    return Json(new { success = false, message = "Failed to add stock" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddStock");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Stock Out
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveStock([FromBody] CreateStockOutDTO stockOutDto)
        {
            try
            {
                if (stockOutDto == null)
                    return Json(new { success = false, message = "Invalid stock data" });

                if (stockOutDto.ProductId <= 0)
                    return Json(new { success = false, message = "Product is required" });

                if (stockOutDto.QuantityRemoved <= 0)
                    return Json(new { success = false, message = "Quantity must be greater than 0" });

                var success = _productService.RemoveStock(stockOutDto);
                if (success)
                    return Json(new { success = true, message = "Stock removed successfully!" });
                else
                    return Json(new { success = false, message = "Insufficient stock available!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveStock");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Get categories
        [HttpGet]
        public IActionResult GetCategories()
        {
            try
            {
                var categories = _categoryService.GetAllCategories()
                    .Where(c => c.isActive && !c.isDelete)
                    .Select(c => new { id = c.Id, name = c.Name })
                    .ToList();
                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategories");
                return Json(new List<object>());
            }
        }

        // Get suppliers
        [HttpGet]
        public IActionResult GetSuppliers()
        {
            try
            {
                var suppliers = _supplierService.GetAllSuppliers()
                    .Where(s => s.isActive && !s.isDelete)
                    .Select(s => new { id = s.SupplierId, name = s.SupplierName })
                    .ToList();
                return Json(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSuppliers");
                return Json(new List<object>());
            }
        }

        // Search products
        [HttpGet]
        public IActionResult SearchProducts(string searchTerm)
        {
            try
            {
                var products = _productService.SearchProducts(searchTerm);
                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchProducts");
                return Json(new List<ProductDTO>());
            }
        }

        // Filter products
        [HttpGet]
        public IActionResult FilterProducts(int? categoryId = null, int? supplierId = null)
        {
            try
            {
                var products = _productService.FilterProducts(categoryId, supplierId);
                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FilterProducts");
                return Json(new List<ProductDTO>());
            }
        }

        // Get low stock report
        [HttpGet]
        public IActionResult GetLowStockReport(int threshold = 10)
        {
            try
            {
                var lowStock = _productService.GetLowStockProducts(threshold);
                return Json(lowStock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLowStockReport");
                return Json(new List<object>());
            }
        }

        // Get stock summary
        [HttpGet]
        public IActionResult GetStockSummary()
        {
            try
            {
                var summary = _productService.GetStockSummary();
                return Json(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStockSummary");
                return Json(new { });
            }
        }

        // Get stock history
        [HttpGet]
        public IActionResult GetStockHistory(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var history = _productService.GetStockHistory(fromDate, toDate);
                return Json(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStockHistory");
                return Json(new List<object>());
            }
        }
    }
}