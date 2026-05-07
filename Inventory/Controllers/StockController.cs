using Inventory.Data;
using Inventory.DTOs;
using Inventory.Models;
using Inventory.Services; // Add this for notification service
using InventoryMS.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.Controllers
{
    public class StockController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAppNotificationService _notificationService; // Use renamed interface

        public StockController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IAppNotificationService notificationService) // Use renamed interface
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
        }
        // ============ ALL VIEWS ============

        [HttpGet]
        public IActionResult StockIn()
        {
            return View();
        }

        [HttpGet]
        public IActionResult StockOut()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LowStock()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LowStockReport()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CurrentStock()
        {
            return View();
        }

        // ============ COMMON APIs ============

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.isDelete == false && p.isActive == true)
                    .Select(p => new {
                        p.ProductId,
                        p.ProductName,
                        p.Quantity
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.isDelete == false && s.isActive == true)
                    .Select(s => new {
                        s.SupplierId,
                        SupplierName = s.SupplierName
                    })
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductStock(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.ProductId == productId && p.isDelete == false)
                    .Select(p => new { p.ProductId, p.ProductName, p.Quantity })
                    .FirstOrDefaultAsync();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============ STOCK IN APIs ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStockIn([FromForm] CreateStockInDTO model, IFormFile? Document)
        {
            try
            {
                if (model.ProductId <= 0)
                    return BadRequest(new { success = false, message = "Please select a valid product" });

                if (model.SupplierId <= 0)
                    return BadRequest(new { success = false, message = "Please select a valid supplier" });

                if (model.QuantityAdded <= 0)
                    return BadRequest(new { success = false, message = "Quantity must be greater than 0" });

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == model.ProductId && p.isDelete == false);

                if (product == null)
                    return BadRequest(new { success = false, message = "Product not found" });

                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.SupplierId == model.SupplierId && s.isDelete == false);

                if (supplier == null)
                    return BadRequest(new { success = false, message = "Supplier not found" });

                // Store old quantity for comparison
                int oldQuantity = product.Quantity;

                var stockIn = new StockIn
                {
                    ProductId = model.ProductId,
                    SupplierId = model.SupplierId,
                    QuantityAdded = model.QuantityAdded,
                    Date = model.Date != DateTime.MinValue ? model.Date : DateTime.Now,
                    UnitPrice = model.UnitPrice,
                    Remarks = model.Remarks ?? string.Empty,
                    CreatedAt = DateTime.Now
                };

                // Handle file upload
                if (Document != null && Document.Length > 0)
                {
                    try
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Document.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await Document.CopyToAsync(fileStream);
                        }

                        stockIn.DocumentPath = "/uploads/documents/" + uniqueFileName;
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"File upload error: {fileEx.Message}");
                    }
                }

                _context.StockIns.Add(stockIn);
                product.Quantity += model.QuantityAdded;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Get user name for notification
                var userName = HttpContext.Session.GetString("UserName") ?? "System";

                // ============ NOTIFICATION 3: Stock In Notification ============
                _notificationService.StockInNotification(
                    product.ProductName,
                    product.ProductId,
                    model.QuantityAdded,
                    userName
                );

                // ============ Check stock status after addition ============
                await CheckAndSendStockStatusNotifications(product, oldQuantity);

                // ============ NOTIFICATION 7: User Activity Notification ============
                _notificationService.UserActivityNotification(
                    "Stock In",
                    userName,
                    $"Added {model.QuantityAdded} units of '{product.ProductName}' from supplier '{supplier.SupplierName}'"
                );

                return Ok(new { success = true, message = $"Stock added successfully! New quantity: {product.Quantity}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockInHistory(DateTime? fromDate, DateTime? toDate, string? searchTerm)
        {
            try
            {
                var query = _context.StockIns
                    .Include(s => s.Product)
                    .Include(s => s.Supplier)
                    .Where(s => s.Product != null && s.Product.isDelete == false)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(s => s.Date.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(s => s.Date.Date <= toDate.Value.Date);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    query = query.Where(s => (s.Product != null && s.Product.ProductName.Contains(searchTerm)) ||
                                             (s.Supplier != null && s.Supplier.SupplierName.Contains(searchTerm)));
                }

                var result = await query
                    .OrderByDescending(s => s.Date)
                    .Select(s => new StockInDTO
                    {
                        StockInId = s.StockInId,
                        ProductId = s.ProductId,
                        ProductName = s.Product != null ? s.Product.ProductName : "N/A",
                        SupplierId = s.SupplierId,
                        SupplierName = s.Supplier != null ? s.Supplier.SupplierName : "N/A",
                        QuantityAdded = s.QuantityAdded,
                        Date = s.Date,
                        UnitPrice = s.UnitPrice,
                        DocumentPath = s.DocumentPath,
                        Remarks = s.Remarks ?? string.Empty
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============ STOCK OUT APIs ============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStock(int productId, int quantityRemoved, string reason, string remarks)
        {
            try
            {
                // Log the request
                System.Diagnostics.Debug.WriteLine($"RemoveStock called: productId={productId}, quantity={quantityRemoved}, reason={reason}");

                // Validation
                if (productId <= 0)
                {
                    return BadRequest(new { success = false, message = "Product ID is required" });
                }

                if (quantityRemoved <= 0)
                {
                    return BadRequest(new { success = false, message = "Quantity must be greater than 0" });
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(new { success = false, message = "Reason is required" });
                }

                // Find product
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId && p.isDelete == false);

                if (product == null)
                {
                    return BadRequest(new { success = false, message = "Product not found" });
                }

                var userName = HttpContext.Session.GetString("UserName") ?? "System";

                // ============ NOTIFICATION 5: Invalid Stock Alert ============
                // Check stock availability
                if (product.Quantity < quantityRemoved)
                {
                    _notificationService.InvalidStockAlert(
                        product.ProductName,
                        product.ProductId,
                        quantityRemoved,
                        product.Quantity,
                        userName
                    );

                    return BadRequest(new { success = false, message = $"Only {product.Quantity} units available" });
                }

                // Store old quantity for comparison
                int oldQuantity = product.Quantity;

                // Update product stock
                product.Quantity -= quantityRemoved;
                product.UpdatedAt = DateTime.Now;

                // Create stock out record
                var stockOut = new StockOut
                {
                    ProductId = productId,
                    QuantityRemoved = quantityRemoved,
                    Reason = reason,
                    Remarks = remarks ?? "",
                    Date = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _context.StockOuts.Add(stockOut);
                await _context.SaveChangesAsync();

                // ============ NOTIFICATION 4: Stock Out Notification ============
                _notificationService.StockOutNotification(
                    product.ProductName,
                    product.ProductId,
                    quantityRemoved,
                    userName,
                    reason
                );

                // ============ Check stock status after removal ============
                await CheckAndSendStockStatusNotifications(product, oldQuantity);

                // ============ NOTIFICATION 7: User Activity Notification ============
                _notificationService.UserActivityNotification(
                    "Stock Out",
                    userName,
                    $"Removed {quantityRemoved} units of '{product.ProductName}' (Reason: {reason})"
                );

                return Ok(new { success = true, message = $"Stock removed successfully! Remaining: {product.Quantity}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockOutHistory(DateTime? fromDate, DateTime? toDate, string? searchTerm)
        {
            try
            {
                var query = _context.StockOuts
                    .Include(s => s.Product)
                    .Where(s => s.Product != null && s.Product.isDelete == false)
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(s => s.Date.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(s => s.Date.Date <= toDate.Value.Date);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    query = query.Where(s => s.Product != null && s.Product.ProductName.Contains(searchTerm));
                }

                var result = await query
                    .OrderByDescending(s => s.Date)
                    .Select(s => new
                    {
                        s.StockOutId,
                        s.ProductId,
                        ProductName = s.Product != null ? s.Product.ProductName : "N/A",
                        s.QuantityRemoved,
                        s.Date,
                        s.Reason,
                        s.Remarks
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTotalStockOut()
        {
            try
            {
                int total = await _context.StockOuts.SumAsync(s => s.QuantityRemoved);
                return Ok(total);
            }
            catch (Exception)
            {
                return Ok(0);
            }
        }

        // ============ LOW STOCK APIs ============

        [HttpGet]
        public async Task<IActionResult> GetLowStockProducts(int threshold = 10)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.isDelete == false && p.isActive == true && p.Quantity <= threshold)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Quantity,
                        p.Unit,
                        Status = p.Quantity <= 0 ? "Out of Stock" : (p.Quantity <= 5 ? "Critical" : "Low Stock")
                    })
                    .OrderBy(p => p.Quantity)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLowStockCount(int threshold = 10)
        {
            try
            {
                int count = await _context.Products
                    .CountAsync(p => p.isDelete == false && p.isActive == true && p.Quantity <= threshold);

                return Ok(count);
            }
            catch (Exception)
            {
                return Ok(0);
            }
        }

        // ============ DASHBOARD APIs ============

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                int totalProducts = await _context.Products.CountAsync(p => p.isDelete == false && p.isActive == true);
                int totalSuppliers = await _context.Suppliers.CountAsync(s => s.isDelete == false && s.isActive == true);
                int lowStockCount = await _context.Products.CountAsync(p => p.isDelete == false && p.isActive == true && p.Quantity <= 10 && p.Quantity > 0);
                int outOfStockCount = await _context.Products.CountAsync(p => p.isDelete == false && p.isActive == true && p.Quantity == 0);

                decimal totalStockValue = await _context.Products
                    .Where(p => p.isDelete == false && p.isActive == true)
                    .SumAsync(p => p.Quantity * p.PurchasePrice);

                DateTime today = DateTime.Today;
                int todayStockIn = await _context.StockIns
                    .Where(s => s.Date.Date == today)
                    .SumAsync(s => s.QuantityAdded);

                int todayStockOut = await _context.StockOuts
                    .Where(s => s.Date.Date == today)
                    .SumAsync(s => s.QuantityRemoved);

                var recentStockIns = await _context.StockIns
                    .Include(s => s.Product)
                    .OrderByDescending(s => s.Date)
                    .Take(5)
                    .Select(s => new
                    {
                        s.Date,
                        ProductName = s.Product != null ? s.Product.ProductName : "N/A",
                        s.QuantityAdded,
                        s.UnitPrice
                    })
                    .ToListAsync();

                var recentStockOuts = await _context.StockOuts
                    .Include(s => s.Product)
                    .OrderByDescending(s => s.Date)
                    .Take(5)
                    .Select(s => new
                    {
                        s.Date,
                        ProductName = s.Product != null ? s.Product.ProductName : "N/A",
                        s.QuantityRemoved,
                        s.Reason
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalProducts,
                    totalSuppliers,
                    lowStockCount,
                    outOfStockCount,
                    totalStockValue,
                    todayStockIn,
                    todayStockOut,
                    recentStockIns,
                    recentStockOuts
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============ STOCK HISTORY for Reports ============

        [HttpGet]
        public async Task<IActionResult> GetStockHistory(DateTime? fromDate, DateTime? toDate, string? type)
        {
            try
            {
                var stockIns = _context.StockIns
                    .Include(s => s.Product)
                    .Include(s => s.Supplier)
                    .Select(s => new
                    {
                        s.Date,
                        Type = "Stock In",
                        ProductName = s.Product != null ? s.Product.ProductName : "N/A",
                        Quantity = s.QuantityAdded,
                        Reference = s.Supplier != null ? s.Supplier.SupplierName : "N/A"
                    });

                var stockOuts = _context.StockOuts
                    .Include(s => s.Product)
                    .Select(s => new
                    {
                        s.Date,
                        Type = "Stock Out",
                        ProductName = s.Product != null ? s.Product.ProductName : "N/A",
                        Quantity = s.QuantityRemoved,
                        Reference = s.Reason ?? "Sale"
                    });

                var query = stockIns.Concat(stockOuts).AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(s => s.Date.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(s => s.Date.Date <= toDate.Value.Date);

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(s => s.Type == type);

                var result = query
                    .OrderByDescending(s => s.Date)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============ CURRENT STOCK REPORT APIs ============

        [HttpGet]
        public async Task<IActionResult> GetCurrentStock(string? searchTerm, string? category, int? minQuantity, int? maxQuantity)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.isDelete == false && p.isActive == true)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.Trim();
                    query = query.Where(p => p.ProductName.Contains(searchTerm));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category != null && p.Category.Name.Contains(category));
                }

                if (minQuantity.HasValue)
                {
                    query = query.Where(p => p.Quantity >= minQuantity.Value);
                }

                if (maxQuantity.HasValue)
                {
                    query = query.Where(p => p.Quantity <= maxQuantity.Value);
                }

                var products = await query
                    .OrderBy(p => p.ProductName)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Quantity,
                        p.Unit,
                        p.PurchasePrice,
                        p.SalePrice,
                        TotalValue = p.Quantity * p.PurchasePrice,
                        CategoryName = p.Category != null ? p.Category.Name : "N/A",
                        SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                        Status = p.Quantity <= 0 ? "Out of Stock" : (p.Quantity <= 10 ? "Low Stock" : "In Stock")
                    })
                    .ToListAsync();

                var summary = new
                {
                    TotalProducts = products.Count,
                    TotalQuantity = products.Sum(p => p.Quantity),
                    TotalValue = products.Sum(p => p.TotalValue),
                    LowStockCount = products.Count(p => p.Status == "Low Stock"),
                    OutOfStockCount = products.Count(p => p.Status == "Out of Stock")
                };

                return Ok(new { products, summary });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentStockSummary()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.isDelete == false && p.isActive == true)
                    .ToListAsync();

                var summary = new
                {
                    TotalProducts = products.Count,
                    TotalQuantity = products.Sum(p => p.Quantity),
                    TotalValue = products.Sum(p => p.Quantity * p.PurchasePrice),
                    LowStockCount = products.Count(p => p.Quantity <= 10 && p.Quantity > 0),
                    OutOfStockCount = products.Count(p => p.Quantity == 0),
                    CriticalStockCount = products.Count(p => p.Quantity <= 5 && p.Quantity > 0)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockValueByCategory()
        {
            try
            {
                var stockByCategory = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.isDelete == false && p.isActive == true && p.Category != null)
                    .GroupBy(p => p.Category.Name)
                    .Select(g => new
                    {
                        Category = g.Key ?? "Uncategorized",
                        TotalQuantity = g.Sum(p => p.Quantity),
                        TotalValue = g.Sum(p => p.Quantity * p.PurchasePrice),
                        ProductCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalValue)
                    .ToListAsync();

                return Ok(stockByCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportCurrentStockToCsv()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.isDelete == false && p.isActive == true)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Quantity,
                        p.Unit,
                        p.PurchasePrice,
                        p.SalePrice,
                        TotalValue = p.Quantity * p.PurchasePrice,
                        CategoryName = p.Category != null ? p.Category.Name : "",
                        SupplierName = p.Supplier != null ? p.Supplier.SupplierName : ""
                    })
                    .ToListAsync();

                var csv = "ProductId,ProductName,Quantity,Unit,PurchasePrice,SalePrice,TotalValue,Category,Supplier\n";
                foreach (var p in products)
                {
                    csv += $"{p.ProductId},{p.ProductName},{p.Quantity},{p.Unit},{p.PurchasePrice},{p.SalePrice},{p.TotalValue},{p.CategoryName},{p.SupplierName}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"CurrentStock_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStockSummary()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync(p => p.isDelete == false && p.isActive == true);
                var lowStockCount = await _context.Products.CountAsync(p => p.isDelete == false && p.isActive == true && p.Quantity <= 10 && p.Quantity > 0);
                var outOfStockCount = await _context.Products.CountAsync(p => p.isDelete == false && p.isActive == true && p.Quantity == 0);
                var totalStockValue = await _context.Products
                    .Where(p => p.isDelete == false && p.isActive == true)
                    .SumAsync(p => p.Quantity * p.PurchasePrice);

                return Ok(new { totalProducts, lowStockCount, outOfStockCount, totalStockValue });
            }
            catch (Exception ex)
            {
                return Ok(new { totalProducts = 0, lowStockCount = 0, outOfStockCount = 0, totalStockValue = 0 });
            }
        }

        // ============ HELPER METHOD: Check and Send Stock Status Notifications ============

        /// <summary>
        /// Checks product stock status and sends Low Stock / Out of Stock notifications
        /// </summary>
        private async Task CheckAndSendStockStatusNotifications(Product product, int oldQuantity)
        {
            int threshold = 10; // Default threshold, can be made configurable

            // ============ NOTIFICATION 2: Out of Stock Alert ============
            if (product.Quantity == 0 && oldQuantity > 0)
            {
                _notificationService.OutOfStockAlert(
                    product.ProductName,
                    product.ProductId
                );
            }
            // ============ NOTIFICATION 1: Low Stock Alert ============
            else if (product.Quantity <= threshold && product.Quantity > 0 && oldQuantity > threshold)
            {
                _notificationService.LowStockAlert(
                    product.ProductName,
                    product.ProductId,
                    product.Quantity,
                    threshold
                );
            }
            // Also check if stock is still low but was already low (don't send duplicate)
            else if (product.Quantity <= threshold && product.Quantity > 0 && oldQuantity <= threshold)
            {
                // Stock is still low, but don't send duplicate notification
                // You can implement a cooldown mechanism if needed
            }

            await Task.CompletedTask;
        }
    }
}