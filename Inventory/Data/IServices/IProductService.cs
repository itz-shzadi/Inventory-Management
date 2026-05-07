using Inventory.DTOs;
using System;
using System.Collections.Generic;

namespace Inventory.Data.IServices
{
    public interface IProductService
    {
        // Product CRUD
        IEnumerable<ProductDTO> GetAllProducts();
        ProductDTO? GetProductById(int id);
        ProductDTO CreateProduct(CreateProductDTO productDto);
        ProductDTO? UpdateProduct(UpdateProductDTO productDto);
        bool DeleteProduct(int id);
        bool IsProductNameUnique(string productName, int? excludeId = null);

        // Stock Management
        bool AddStock(CreateStockInDTO stockInDto, string? documentPath = null);
        bool RemoveStock(CreateStockOutDTO stockOutDto);

        // History
        IEnumerable<StockInDTO> GetStockInHistory(int? productId = null, DateTime? fromDate = null, DateTime? toDate = null);
        IEnumerable<StockOutDTO> GetStockOutHistory(int? productId = null);
        IEnumerable<StockHistoryDTO> GetStockHistory(DateTime? fromDate = null, DateTime? toDate = null);

        // Reports
        IEnumerable<LowStockReportDTO> GetLowStockProducts(int threshold = 10);
        StockSummaryDTO GetStockSummary();

        // Search/Filter
        IEnumerable<ProductDTO> SearchProducts(string searchTerm);
        IEnumerable<ProductDTO> FilterProducts(int? categoryId = null, int? supplierId = null);
    }
}