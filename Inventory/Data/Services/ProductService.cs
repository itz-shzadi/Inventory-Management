using Inventory.Data;
using Inventory.Data.IServices;
using Inventory.DTOs;
using Inventory.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get all products
        public IEnumerable<ProductDTO> GetAllProducts()
        {
            try
            {
                return _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => !p.isDelete)
                    .Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        SupplierId = p.SupplierId,
                        SupplierName = p.Supplier.SupplierName,
                        PurchasePrice = p.PurchasePrice,
                        SalePrice = p.SalePrice,
                        Quantity = p.Quantity,
                        Unit = p.Unit,
                        Description = p.Description
                    })
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return new List<ProductDTO>();
            }
        }

        // Get product by id
        public ProductDTO GetProductById(int id)
        {
            try
            {
                var product = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefault(p => p.ProductId == id && !p.isDelete);

                if (product == null) return null;

                return new ProductDTO
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category?.Name,
                    SupplierId = product.SupplierId,
                    SupplierName = product.Supplier?.SupplierName,
                    PurchasePrice = product.PurchasePrice,
                    SalePrice = product.SalePrice,
                    Quantity = product.Quantity,
                    Unit = product.Unit,
                    Description = product.Description
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting product by id: {id}");
                return null;
            }
        }

        // Create product
        public ProductDTO CreateProduct(CreateProductDTO productDto)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var product = new Product
                {
                    ProductName = productDto.ProductName,
                    CategoryId = productDto.CategoryId,
                    SupplierId = productDto.SupplierId,
                    PurchasePrice = productDto.PurchasePrice,
                    SalePrice = productDto.SalePrice,
                    Quantity = productDto.Quantity,
                    Unit = productDto.Unit,
                    Description = productDto.Description,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    isActive = true,
                    isDelete = false
                };

                _context.Products.Add(product);
                _context.SaveChanges();
                transaction.Commit();

                return GetProductById(product.ProductId);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        // Update product
        public ProductDTO UpdateProduct(UpdateProductDTO productDto)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var product = _context.Products.Find(productDto.ProductId);
                if (product == null) return null;

                product.ProductName = productDto.ProductName;
                product.CategoryId = productDto.CategoryId;
                product.SupplierId = productDto.SupplierId;
                product.PurchasePrice = productDto.PurchasePrice;
                product.SalePrice = productDto.SalePrice;
                product.Quantity = productDto.Quantity;
                product.Unit = productDto.Unit;
                product.Description = productDto.Description;
                product.UpdatedAt = DateTime.Now;

                _context.SaveChanges();
                transaction.Commit();

                return GetProductById(product.ProductId);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error updating product");
                throw;
            }
        }

        // Delete product
        public bool DeleteProduct(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var product = _context.Products.Find(id);
                if (product == null) return false;

                product.isDelete = true;
                product.UpdatedAt = DateTime.Now;
                _context.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, $"Error deleting product: {id}");
                return false;
            }
        }

        // Check if product name is unique
        public bool IsProductNameUnique(string productName, int? excludeId = null)
        {
            var query = _context.Products.Where(p => p.ProductName == productName && !p.isDelete);
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.ProductId != excludeId.Value);
            }
            return !query.Any();
        }

        // Add stock (Stock In) - UPDATED with documentPath parameter
        public bool AddStock(CreateStockInDTO stockInDto, string? documentPath = null)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var product = _context.Products.Find(stockInDto.ProductId);
                if (product == null) return false;

                // Add stock entry with document path and unit price
                var stockIn = new StockIn
                {
                    ProductId = stockInDto.ProductId,
                    SupplierId = stockInDto.SupplierId,
                    QuantityAdded = stockInDto.QuantityAdded,
                    Remarks = stockInDto.Remarks,
                    Date = stockInDto.Date != DateTime.MinValue ? stockInDto.Date : DateTime.Now,
                    UnitPrice = stockInDto.UnitPrice,
                    DocumentPath = documentPath,
                    CreatedAt = DateTime.Now
                };

                _context.StockIns.Add(stockIn);

                // Update product quantity
                product.Quantity += stockInDto.QuantityAdded;
                product.UpdatedAt = DateTime.Now;

                _context.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error adding stock");
                return false;
            }
        }

        // Remove stock (Stock Out)
        public bool RemoveStock(CreateStockOutDTO stockOutDto)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var product = _context.Products.Find(stockOutDto.ProductId);
                if (product == null)
                {
                    _logger.LogWarning($"Product not found: {stockOutDto.ProductId}");
                    return false;
                }

                _logger.LogInformation($"Current stock for product {product.ProductName}: {product.Quantity}");
                _logger.LogInformation($"Requested to remove: {stockOutDto.QuantityRemoved}");

                // Check if sufficient stock available
                if (product.Quantity < stockOutDto.QuantityRemoved)
                {
                    _logger.LogWarning($"Insufficient stock. Available: {product.Quantity}, Requested: {stockOutDto.QuantityRemoved}");
                    return false;
                }

                // Add stock out entry
                var stockOut = new StockOut
                {
                    ProductId = stockOutDto.ProductId,
                    QuantityRemoved = stockOutDto.QuantityRemoved,
                    Reason = stockOutDto.Reason,
                    Remarks = stockOutDto.Remarks,
                    Date = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _context.StockOuts.Add(stockOut);
                int result1 = _context.SaveChanges();
                _logger.LogInformation($"StockOut saved. Rows affected: {result1}");

                // Update product quantity
                product.Quantity -= stockOutDto.QuantityRemoved;
                product.UpdatedAt = DateTime.Now;

                int result2 = _context.SaveChanges();
                _logger.LogInformation($"Product updated. New quantity: {product.Quantity}, Rows affected: {result2}");

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error removing stock");
                return false;
            }
        }

        // Get stock in history - UPDATED with fromDate and toDate parameters
        public IEnumerable<StockInDTO> GetStockInHistory(int? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.StockIns
                    .Include(s => s.Product)
                    .Include(s => s.Supplier)
                    .AsQueryable();

                if (productId.HasValue && productId.Value > 0)
                {
                    query = query.Where(s => s.ProductId == productId.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(s => s.Date >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(s => s.Date < endDate);
                }

                return query
                    .OrderByDescending(s => s.Date)
                    .Select(s => new StockInDTO
                    {
                        StockInId = s.StockInId,
                        ProductId = s.ProductId,
                        ProductName = s.Product.ProductName,
                        SupplierId = s.SupplierId,
                        SupplierName = s.Supplier.SupplierName,
                        QuantityAdded = s.QuantityAdded,
                        Date = s.Date,
                        UnitPrice = s.UnitPrice,
                        DocumentPath = s.DocumentPath,
                        Remarks = s.Remarks
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock in history");
                return new List<StockInDTO>();
            }
        }

        // Get stock out history
        public IEnumerable<StockOutDTO> GetStockOutHistory(int? productId = null)
        {
            try
            {
                var query = _context.StockOuts
                    .Include(s => s.Product)
                    .AsQueryable();

                if (productId.HasValue && productId.Value > 0)
                {
                    query = query.Where(s => s.ProductId == productId.Value);
                }

                return query
                    .OrderByDescending(s => s.Date)
                    .Select(s => new StockOutDTO
                    {
                        StockOutId = s.StockOutId,
                        ProductId = s.ProductId,
                        ProductName = s.Product.ProductName,
                        QuantityRemoved = s.QuantityRemoved,
                        Date = s.Date,
                        Reason = s.Reason,
                        Remarks = s.Remarks
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock out history");
                return new List<StockOutDTO>();
            }
        }

        // Get low stock products
        public IEnumerable<LowStockReportDTO> GetLowStockProducts(int threshold = 10)
        {
            try
            {
                return _context.Products
                    .Include(p => p.Category)
                    .Where(p => !p.isDelete && p.Quantity <= threshold)
                    .Select(p => new LowStockReportDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        CategoryName = p.Category.Name,
                        CurrentStock = p.Quantity,
                        Unit = p.Unit,
                        Threshold = threshold
                    })
                    .OrderBy(p => p.CurrentStock)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                return new List<LowStockReportDTO>();
            }
        }

        // Get stock summary
        public StockSummaryDTO GetStockSummary()
        {
            try
            {
                var products = _context.Products.Where(p => !p.isDelete);

                return new StockSummaryDTO
                {
                    TotalProducts = products.Count(),
                    LowStockCount = products.Count(p => p.Quantity <= 10),
                    OutOfStockCount = products.Count(p => p.Quantity == 0),
                    TotalStockValue = products.Sum(p => p.Quantity * p.PurchasePrice)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock summary");
                return new StockSummaryDTO();
            }
        }

        // Get stock history (combined in/out)
        public IEnumerable<StockHistoryDTO> GetStockHistory(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var stockIns = _context.StockIns
                    .Include(s => s.Product)
                    .Select(s => new StockHistoryDTO
                    {
                        Date = s.Date,
                        Type = "Stock In",
                        ProductName = s.Product.ProductName,
                        Quantity = s.QuantityAdded,
                        Reference = s.Supplier.SupplierName
                    });

                var stockOuts = _context.StockOuts
                    .Include(s => s.Product)
                    .Select(s => new StockHistoryDTO
                    {
                        Date = s.Date,
                        Type = "Stock Out",
                        ProductName = s.Product.ProductName,
                        Quantity = s.QuantityRemoved,
                        Reference = s.Reason ?? "Sale"
                    });

                var combined = stockIns.Concat(stockOuts);

                if (fromDate.HasValue)
                    combined = combined.Where(h => h.Date >= fromDate.Value);
                if (toDate.HasValue)
                    combined = combined.Where(h => h.Date <= toDate.Value);

                return combined.OrderByDescending(h => h.Date).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock history");
                return new List<StockHistoryDTO>();
            }
        }

        // Search products
        public IEnumerable<ProductDTO> SearchProducts(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return GetAllProducts();

                searchTerm = searchTerm.ToLower();
                return _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => !p.isDelete && (
                        p.ProductName.ToLower().Contains(searchTerm) ||
                        p.Category.Name.ToLower().Contains(searchTerm) ||
                        p.Supplier.SupplierName.ToLower().Contains(searchTerm)
                    ))
                    .Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        SupplierId = p.SupplierId,
                        SupplierName = p.Supplier.SupplierName,
                        PurchasePrice = p.PurchasePrice,
                        SalePrice = p.SalePrice,
                        Quantity = p.Quantity,
                        Unit = p.Unit,
                        Description = p.Description
                    })
                    .OrderBy(p => p.ProductName)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return new List<ProductDTO>();
            }
        }

        // Filter products
        public IEnumerable<ProductDTO> FilterProducts(int? categoryId = null, int? supplierId = null)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => !p.isDelete);

                if (categoryId.HasValue && categoryId.Value > 0)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                if (supplierId.HasValue && supplierId.Value > 0)
                    query = query.Where(p => p.SupplierId == supplierId.Value);

                return query.Select(p => new ProductDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.SupplierName,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    Description = p.Description
                })
                .OrderBy(p => p.ProductName)
                .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products");
                return new List<ProductDTO>();
            }
        }
    }
}