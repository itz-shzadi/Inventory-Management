// Product Management Module
var ProductManagement = (function () {
    var isEditMode = false;
    var currentSearchTerm = '';

    // Show Toast
    function showToast(message, type) {
        $('#customToast').remove();
        var toast = $('<div id="customToast">' + message + '</div>');
        $('body').append(toast);
        toast.css({
            'position': 'fixed',
            'bottom': '20px',
            'right': '20px',
            'padding': '12px 20px',
            'backgroundColor': type === 'success' ? '#28a745' : (type === 'error' ? '#dc3545' : '#17a2b8'),
            'color': 'white',
            'borderRadius': '5px',
            'zIndex': '99999',
            'fontSize': '14px',
            'boxShadow': '0 2px 5px rgba(0,0,0,0.2)'
        });
        toast.fadeIn(300);
        setTimeout(function () { toast.fadeOut(300, function () { $(this).remove(); }); }, 3000);
    }

    function getToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // Load Categories for main form
    function loadCategories() {
        $.ajax({
            url: '/Product/GetCategories',
            type: 'GET',
            success: function (categories) {
                var options = '<option value="">Select Category</option>';
                for (var i = 0; i < categories.length; i++) {
                    options += '<option value="' + categories[i].id + '">' + categories[i].name + '</option>';
                }
                $('#categoryId').html(options);
            },
            error: function (xhr, status, error) {
                console.error('Error loading categories:', error);
            }
        });
    }

    // Load Suppliers for main form
    function loadSuppliers() {
        console.log('Loading suppliers for main form...');
        $.ajax({
            url: '/Product/GetSuppliers',
            type: 'GET',
            success: function (suppliers) {
                console.log('Suppliers loaded:', suppliers);
                var options = '<option value="">Select Supplier</option>';
                for (var i = 0; i < suppliers.length; i++) {
                    options += '<option value="' + suppliers[i].id + '">' + suppliers[i].name + '</option>';
                }
                $('#supplierId').html(options);
            },
            error: function (xhr, status, error) {
                console.error('Error loading suppliers:', error);
            }
        });
    }

    // Load Suppliers for Stock Modal
    function loadSuppliersForStock() {
        console.log('Loading suppliers for stock modal...');
        $.ajax({
            url: '/Product/GetSuppliers',
            type: 'GET',
            success: function (suppliers) {
                console.log('Suppliers for stock:', suppliers);
                var options = '<option value="">Select Supplier</option>';
                for (var i = 0; i < suppliers.length; i++) {
                    options += '<option value="' + suppliers[i].id + '">' + suppliers[i].name + '</option>';
                }
                $('#stockSupplierId').html(options);
            },
            error: function (xhr, status, error) {
                console.error('Error loading suppliers for stock:', error);
                $('#stockSupplierId').html('<option value="">Error loading suppliers</option>');
            }
        });
    }

    // Load Products for Stock Select
    function loadProductsForStock() {
        $.ajax({
            url: '/Product/GetAllProducts',
            type: 'GET',
            success: function (products) {
                var options = '<option value="">Select Product</option>';
                for (var i = 0; i < products.length; i++) {
                    options += '<option value="' + products[i].productId + '">' + products[i].productName + ' (Current Stock: ' + products[i].quantity + ')</option>';
                }
                $('#stockProductSelect').html(options);
            },
            error: function (xhr, status, error) {
                console.error('Error loading products:', error);
            }
        });
    }

    // Load Products
    function loadProducts() {
        var url = currentSearchTerm ? '/Product/SearchProducts' : '/Product/GetAllProducts';
        var data = currentSearchTerm ? { searchTerm: currentSearchTerm } : {};

        $.ajax({
            url: url,
            type: 'GET',
            data: data,
            success: function (products) {
                displayProducts(products);
                $('#totalProducts').text(products.length);
                checkLowStock(products);
            },
            error: function (xhr, status, error) {
                console.error('Error loading products:', error);
                $('#productsTableBody').html('<tr><td colspan="7" style="text-align:center;color:red;">Error loading products</td></tr>');
            }
        });
    }

    // Check Low Stock
    function checkLowStock(products) {
        var lowStock = products.filter(function (p) { return p.quantity <= 10; });
        $('#lowStockCount').text(lowStock.length);
    }

    // Escape HTML
    function escapeHtml(str) {
        if (!str) return '';
        return $('<div>').text(str).html();
    }

    // Display Products - No Highlight, Icons in One Line
    function displayProducts(products) {
        if (!products || products.length === 0) {
            $('#productsTableBody').html('<tr><td colspan="7" style="text-align:center;">No products found</td></tr>');
            $('#showingCount').text('0');
            return;
        }

        var html = '';
        for (var i = 0; i < products.length; i++) {
            var p = products[i];
            // Remove low-stock class - no highlight
            html += '<tr data-id="' + p.productId + '">';
            html += '<td><strong>' + escapeHtml(p.productName) + '</strong></td>';
            html += '<td>' + escapeHtml(p.categoryName) + '</td>';
            html += '<td>' + escapeHtml(p.supplierName) + '</td>';
            html += '<td>$' + parseFloat(p.salePrice).toFixed(2) + '</td>';
            html += '<td>' + p.quantity + '</td>';
            html += '<td>' + (p.unit || '-') + '</td>';
            html += '<td class="action-buttons">';
            html += '<button class="edit-btn" onclick="ProductManagement.editProduct(' + p.productId + ')"><i class="fas fa-edit"></i> Edit</button>';
            html += '<button class="delete-btn" onclick="ProductManagement.deleteProduct(' + p.productId + ')"><i class="fas fa-trash"></i> Delete</button>';
            html += '<button class="stock-btn" onclick="ProductManagement.openStockModal(' + p.productId + ', \'' + escapeHtml(p.productName) + '\')"><i class="fas fa-plus"></i> Stock</button>';
            html += '</td>';
            html += '</tr>';
        }
        $('#productsTableBody').html(html);
        $('#showingCount').text(products.length);
    }
    // Create Product
    function createProduct(productData) {
        $.ajax({
            url: '/Product/Create',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(productData),
            headers: { 'RequestVerificationToken': getToken() },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Saving...');
            },
            success: function (response) {
                console.log('Create response:', response);
                if (response.success) {
                    showToast(response.message, 'success');
                    resetForm();
                    loadProducts();
                } else {
                    showToast(response.message, 'error');
                }
            },
            error: function (xhr) {
                console.error('Create error:', xhr);
                var msg = 'Error creating product';
                try {
                    var response = JSON.parse(xhr.responseText);
                    msg = response.message || msg;
                } catch (e) { }
                showToast(msg, 'error');
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Product');
            }
        });
    }

    // Update Product
    function updateProduct(productData) {
        $.ajax({
            url: '/Product/Edit',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(productData),
            headers: { 'RequestVerificationToken': getToken() },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Updating...');
            },
            success: function (response) {
                console.log('Update response:', response);
                if (response.success) {
                    showToast(response.message, 'success');
                    resetForm();
                    loadProducts();
                } else {
                    showToast(response.message, 'error');
                }
            },
            error: function (xhr) {
                console.error('Update error:', xhr);
                var msg = 'Error updating product';
                try {
                    var response = JSON.parse(xhr.responseText);
                    msg = response.message || msg;
                } catch (e) { }
                showToast(msg, 'error');
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Product');
            }
        });
    }

    // Delete Product
    function deleteProduct(id) {
        if (confirm('Are you sure you want to delete this product?')) {
            $.ajax({
                url: '/Product/Delete/' + id,
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                success: function (response) {
                    if (response.success) {
                        showToast(response.message, 'success');
                        loadProducts();
                    } else {
                        showToast(response.message, 'error');
                    }
                },
                error: function (xhr) {
                    console.error('Delete error:', xhr);
                    showToast('Error deleting product', 'error');
                }
            });
        }
    }

    // Add this variable at the top with other variables
    var originalQuantity = 0;

    // Edit Product - updated to store original quantity
    function editProduct(id) {
        $.ajax({
            url: '/Product/GetProductById/' + id,
            type: 'GET',
            success: function (response) {
                console.log('Edit response:', response);
                if (response.success) {
                    isEditMode = true;
                    originalQuantity = response.quantity; // Store original quantity
                    $('#productId').val(response.productId);
                    $('#productName').val(response.productName);
                    $('#categoryId').val(response.categoryId);
                    $('#supplierId').val(response.supplierId);
                    $('#purchasePrice').val(response.purchasePrice);
                    $('#salePrice').val(response.salePrice);
                    $('#quantity').val(response.quantity);
                    $('#unit').val(response.unit);
                    $('#description').val(response.description);
                    $('#formTitle').text('Edit Product');
                    $('#submitBtn').html('<i class="fas fa-pen"></i> Update Product');
                    $('#cancelBtn').show();
                    $('html, body').animate({ scrollTop: 0 }, 300);
                } else {
                    showToast(response.message || 'Error loading product', 'error');
                }
            },
            error: function (xhr) {
                console.error('Edit load error:', xhr);
                showToast('Error loading product', 'error');
            }
        });
    }

    // Open Stock Modal
    function openStockModal(productId, productName) {
        console.log('Opening stock modal for product:', productId, productName);
        $('#stockProductId').val(productId);
        $('#stockQuantity').val('');
        $('#stockRemarks').val('');
        $('#stockModal').show();

        loadProductsForStock();
        loadSuppliersForStock();

        setTimeout(function () {
            $('#stockProductSelect').val(productId);
        }, 500);
    }

    // Add Stock
    function addStock() {
        var productId = $('#stockProductSelect').val();
        var supplierId = $('#stockSupplierId').val();
        var quantity = $('#stockQuantity').val();
        var remarks = $('#stockRemarks').val();

        console.log('Selected Product ID:', productId);
        console.log('Selected Supplier ID:', supplierId);
        console.log('Quantity:', quantity);

        var stockData = {
            ProductId: parseInt(productId),
            SupplierId: parseInt(supplierId),
            QuantityAdded: parseInt(quantity),
            Remarks: remarks
        };

        if (!stockData.ProductId || stockData.ProductId <= 0) {
            showToast('Please select a product', 'error');
            return;
        }

        if (!stockData.SupplierId || stockData.SupplierId <= 0) {
            showToast('Please select a supplier', 'error');
            return;
        }

        if (!stockData.QuantityAdded || stockData.QuantityAdded <= 0) {
            showToast('Please enter valid quantity', 'error');
            return;
        }

        console.log('Sending stock data:', stockData);

        $.ajax({
            url: '/Product/AddStock',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(stockData),
            headers: { 'RequestVerificationToken': getToken() },
            success: function (response) {
                console.log('Stock response:', response);
                if (response.success) {
                    showToast(response.message, 'success');
                    $('#stockModal').hide();
                    loadProducts();
                    $('#stockProductSelect').val('');
                    $('#stockSupplierId').val('');
                    $('#stockQuantity').val('');
                    $('#stockRemarks').val('');
                } else {
                    showToast(response.message, 'error');
                }
            },
            error: function (xhr) {
                console.error('Stock error:', xhr);
                showToast('Error adding stock: ' + (xhr.responseJSON?.message || xhr.statusText), 'error');
            }
        });
    }

    // Reset Form
    function resetForm() {
        isEditMode = false;
        $('#productForm')[0].reset();
        $('#productId').val('0');
        $('#formTitle').text('Add New Product');
        $('#submitBtn').html('<i class="fas fa-save"></i> Save Product');
        $('#cancelBtn').hide();
        $('.error').text('');
    }

    // Search Products
    function searchProducts() {
        currentSearchTerm = $('#searchInput').val().trim();
        loadProducts();
    }

    function resetSearch() {
        $('#searchInput').val('');
        currentSearchTerm = '';
        loadProducts();
    }

    // Validate and Submit
    function validateAndSubmit(e) {
        e.preventDefault();

        $('.error').text('');

        var productName = $('#productName').val().trim();
        var categoryId = $('#categoryId').val();
        var supplierId = $('#supplierId').val();
        var purchasePrice = parseFloat($('#purchasePrice').val());
        var salePrice = parseFloat($('#salePrice').val());
        var quantity = parseInt($('#quantity').val()) || 0;

        var isValid = true;

        if (!productName) {
            $('#nameError').text('Product name is required');
            isValid = false;
        } else if (productName.length < 2) {
            $('#nameError').text('Product name must be at least 2 characters');
            isValid = false;
        }

        if (!categoryId) {
            $('#categoryError').text('Category is required');
            isValid = false;
        }

        if (!supplierId) {
            $('#supplierError').text('Supplier is required');
            isValid = false;
        }

        if (!purchasePrice || purchasePrice <= 0) {
            $('#purchasePriceError').text('Purchase price must be greater than 0');
            isValid = false;
        }

        if (!salePrice || salePrice <= 0) {
            $('#salePriceError').text('Sale price must be greater than 0');
            isValid = false;
        }

        if (salePrice < purchasePrice) {
            $('#salePriceError').text('Sale price cannot be less than purchase price');
            isValid = false;
        }

        // FIXED: Quantity validation - prevent negative values
        if (isNaN(quantity) || quantity < 0) {
            $('#quantityError').text('Quantity cannot be negative');
            isValid = false;
        }

        if (!isValid) {
            showToast('Please fix validation errors', 'error');
            return false;
        }

        var productData = {
            productId: parseInt($('#productId').val()) || 0,
            productName: productName,
            categoryId: parseInt(categoryId),
            supplierId: parseInt(supplierId),
            purchasePrice: purchasePrice,
            salePrice: salePrice,
            quantity: quantity,
            unit: $('#unit').val().trim(),
            description: $('#description').val().trim()
        };

        console.log('Sending product data:', productData);

        if (isEditMode) {
            updateProduct(productData);
        } else {
            createProduct(productData);
        }

        return false;
    }

    // Init - MAIN INITIALIZATION FUNCTION
    function init() {
        console.log('ProductManagement initializing...');

        // Load all dropdowns
        loadCategories();
        loadSuppliers();  // ← YEH FUNCTION AB DEFINED HAI

        // Load products
        loadProducts();

        // Event handlers
        $('#productForm').on('submit', validateAndSubmit);
        $('#cancelBtn').on('click', resetForm);
        $('#searchBtn').on('click', searchProducts);
        $('#resetSearchBtn').on('click', resetSearch);

        // Stock In button
        $('#stockInBtn').on('click', function () {
            console.log('Stock In button clicked');
            loadProductsForStock();
            loadSuppliersForStock();
            $('#stockModal').show();
        });

        // Stock form submit
        $('#stockForm').on('submit', function (e) {
            e.preventDefault();
            addStock();
            return false;
        });

        // Close modal buttons
        $('.close, #closeModalBtn').on('click', function () {
            $('#stockModal').hide();
        });

        // Click outside to close
        $(window).on('click', function (e) {
            if ($(e.target).is('#stockModal')) {
                $('#stockModal').hide();
            }
        });

        console.log('ProductManagement initialized successfully');
    }

    // Public API
    return {
        init: init,
        editProduct: editProduct,
        deleteProduct: deleteProduct,
        openStockModal: openStockModal
    };
})();

// Document ready
$(document).ready(function () {
    ProductManagement.init();
});
// Add these functions to your existing JavaScript

// Load Categories for Filter
function loadCategoriesForFilter() {
    $.ajax({
        url: '/Product/GetCategories',
        type: 'GET',
        success: function (categories) {
            var options = '<option value="">All Categories</option>';
            for (var i = 0; i < categories.length; i++) {
                options += '<option value="' + categories[i].id + '">' + categories[i].name + '</option>';
            }
            $('#categoryFilter').html(options);
        }
    });
}

// Load Suppliers for Filter
function loadSuppliersForFilter() {
    $.ajax({
        url: '/Product/GetSuppliers',
        type: 'GET',
        success: function (suppliers) {
            var options = '<option value="">All Suppliers</option>';
            for (var i = 0; i < suppliers.length; i++) {
                options += '<option value="' + suppliers[i].id + '">' + suppliers[i].name + '</option>';
            }
            $('#supplierFilter').html(options);
        }
    });
}

// Filter Products
function filterProducts() {
    var categoryId = $('#categoryFilter').val();
    var supplierId = $('#supplierFilter').val();
    var searchTerm = $('#searchInput').val().trim();

    $.ajax({
        url: '/Product/FilterProducts',
        type: 'GET',
        data: {
            categoryId: categoryId,
            supplierId: supplierId,
            searchTerm: searchTerm
        },
        success: function (products) {
            displayProducts(products);
            $('#totalProducts').text(products.length);
            $('#showingCount').text(products.length);
            checkLowStock(products);
        }
    });
}

// Update init function
function init() {
    console.log('ProductManagement initializing...');

    loadCategories();
    loadSuppliers();
    loadCategoriesForFilter();
    loadSuppliersForFilter();
    loadProducts();

    $('#productForm').on('submit', validateAndSubmit);
    $('#cancelBtn').on('click', resetForm);
    $('#searchInput').on('keyup', function (e) {
        if (e.key === 'Enter') {
            filterProducts();
        }
    });
    $('#filterBtn').on('click', filterProducts);
    $('#resetSearchBtn').on('click', function () {
        $('#searchInput').val('');
        $('#categoryFilter').val('');
        $('#supplierFilter').val('');
        filterProducts();
    });
    $('#stockInBtn').on('click', function () {
        loadProductsForStock();
        loadSuppliersForStock();
        $('#stockModal').show();
    });

    $('#stockForm').on('submit', function (e) {
        e.preventDefault();
        addStock();
        return false;
    });

    $('.close, #closeModalBtn').on('click', function () {
        $('#stockModal').hide();
    });

    $(window).on('click', function (e) {
        if ($(e.target).is('#stockModal')) {
            $('#stockModal').hide();
        }
    });

    console.log('ProductManagement initialized successfully');
}