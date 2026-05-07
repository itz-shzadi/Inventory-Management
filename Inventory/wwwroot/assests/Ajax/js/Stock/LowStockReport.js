// LowStockReport.js - COMPLETE FIXED VERSION with Product Dropdown
var currentThreshold = 10;

$(document).ready(function () {
    console.log("LowStockReport.js loaded");
    loadLowStockReport();
    loadSuppliers();
    loadProducts(); // New function to load products

    $('#searchBtn').on('click', function () {
        searchLowStock();
    });

    $('#resetBtn').on('click', function () {
        $('#searchInput').val('');
        loadLowStockReport();
    });

    $('#applyThreshold').on('click', function () {
        currentThreshold = parseInt($('#thresholdFilter').val());
        loadLowStockReport();
    });

    // When product is selected, show current stock
    $('#stockProductId').on('change', function () {
        var selectedOption = $('#stockProductId option:selected');
        var currentStock = selectedOption.data('stock') || 0;
        $('#stockCurrentStock').val(currentStock + ' units');
    });

    $('#stockInAllBtn').on('click', function () {
        $('#stockModal').show();
        $('#stockProductId').val('');
        $('#stockCurrentStock').val('');
        $('#stockQuantity').val('');
        $('#stockRemarks').val('');
        $('#stockSupplierId').val('');
    });

    $('#closeModalBtn, #cancelStockBtn, .close').on('click', function () {
        $('#stockModal').hide();
    });

    $('#stockForm').on('submit', function (e) {
        e.preventDefault();
        addStock();
    });
});

function showToast(message, type) {
    var toast = $('#toast');
    toast.removeClass('success error').addClass(type).text(message);
    toast.fadeIn(300);
    setTimeout(function () { toast.fadeOut(300); }, 3000);
}

function getToken() {
    return $('input[name="__RequestVerificationToken"]').val();
}

// Load products for dropdown
function loadProducts() {
    $.ajax({
        url: '/Stock/GetAllProducts',
        type: 'GET',
        success: function (products) {
            var options = '<option value="">Select Product</option>';
            if (products && products.length) {
                for (var i = 0; i < products.length; i++) {
                    var stock = products[i].quantity || 0;
                    var warning = '';
                    if (stock <= 10 && stock > 0) {
                        warning = ' ⚠️ Low Stock';
                    } else if (stock === 0) {
                        warning = ' 🔴 Out of Stock';
                    }
                    options += '<option value="' + products[i].productId + '" data-stock="' + stock + '">' +
                        escapeHtml(products[i].productName) + ' (Stock: ' + stock + ')' + warning + '</option>';
                }
            }
            $('#stockProductId').html(options);
        },
        error: function (xhr) {
            console.error('Error loading products:', xhr);
            showToast('Error loading products', 'error');
        }
    });
}

function loadSuppliers() {
    $.ajax({
        url: '/Stock/GetAllSuppliers',
        type: 'GET',
        success: function (suppliers) {
            var options = '<option value="">Select Supplier</option>';
            if (suppliers && suppliers.length) {
                for (var i = 0; i < suppliers.length; i++) {
                    options += '<option value="' + suppliers[i].supplierId + '">' +
                        escapeHtml(suppliers[i].supplierName) + '</option>';
                }
            }
            $('#stockSupplierId').html(options);
        },
        error: function (xhr) {
            console.error('Error loading suppliers:', xhr);
            showToast('Error loading suppliers', 'error');
        }
    });
}

function loadLowStockReport() {
    var threshold = currentThreshold;

    $('#lowStockTableBody').html('<tr><td colspan="7" style="text-align:center;">Loading...<\/td><\/tr>');

    $.ajax({
        url: '/Stock/GetLowStockProducts',
        type: 'GET',
        data: { threshold: threshold },
        success: function (data) {
            console.log("Low stock data received:", data);
            displayLowStockReport(data);
            updateCounts(data);
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            $('#lowStockTableBody').html('<tr><td colspan="7" style="text-align:center;color:red;">Error loading data<\/td><\/tr>');
            showToast('Error loading low stock report', 'error');
        }
    });
}

function displayLowStockReport(products) {
    if (!products || products.length === 0) {
        $('#lowStockTableBody').html('<tr><td colspan="7" style="text-align:center;">No low stock items found<\/td><\/tr>');
        $('#showingCount').text('0');
        return;
    }

    var html = '';
    for (var i = 0; i < products.length; i++) {
        var p = products[i];
        var statusClass = '';
        var statusText = '';

        if (p.quantity <= 0) {
            statusClass = 'badge-danger';
            statusText = 'Out of Stock';
        } else if (p.quantity <= 5) {
            statusClass = 'badge-danger';
            statusText = 'Critical';
        } else {
            statusClass = 'badge-warning';
            statusText = 'Low Stock';
        }

        html += '<tr>';
        html += '<td><strong>' + escapeHtml(p.productName) + '</strong></td>';
        html += '<td>' + escapeHtml(p.categoryName || '-') + '</td>';
        html += '<td><span class="' + (p.quantity <= 5 ? 'badge-danger' : 'badge-warning') + '">' + p.quantity + '</span></td>';
        html += '<td>' + escapeHtml(p.unit || '-') + '</td>';
        html += '<td>' + currentThreshold + '</td>';
        html += '<td><span class="' + statusClass + '">' + statusText + '</span></td>';
        html += '<td><button class="btn btn-primary btn-sm" onclick="openStockModal(' + p.productId + ', \'' + escapeHtml(p.productName) + '\', ' + p.quantity + ')">';
        html += '<i class="fas fa-plus"></i> Restock</button></td>';
        html += '</tr>';
    }

    $('#lowStockTableBody').html(html);
    $('#showingCount').text(products.length);
}

function updateCounts(products) {
    if (!products || products.length === 0) {
        $('#lowStockCount').text('0');
        $('#outOfStockCount').text('0');
        return;
    }

    var lowStock = products.filter(function (p) {
        return p.quantity > 0 && p.quantity <= currentThreshold;
    }).length;

    var outOfStock = products.filter(function (p) {
        return p.quantity === 0;
    }).length;

    $('#lowStockCount').text(lowStock);
    $('#outOfStockCount').text(outOfStock);
}

function searchLowStock() {
    var searchTerm = $('#searchInput').val().trim().toLowerCase();

    if (!searchTerm) {
        loadLowStockReport();
        return;
    }

    $('#lowStockTableBody').html('<tr><td colspan="7" style="text-align:center;">Searching...<\/td><\/tr>');

    $.ajax({
        url: '/Stock/GetLowStockProducts',
        type: 'GET',
        data: { threshold: currentThreshold },
        success: function (data) {
            var filtered = data.filter(function (item) {
                return item.productName.toLowerCase().includes(searchTerm);
            });
            displayLowStockReport(filtered);
            updateCounts(filtered);
        },
        error: function () {
            showToast('Error searching', 'error');
        }
    });
}

function openStockModal(productId, productName, currentStock) {
    $('#stockProductId').val(productId);
    $('#stockCurrentStock').val(currentStock + ' units');
    $('#stockQuantity').val('');
    $('#stockRemarks').val('');
    $('#stockSupplierId').val('');
    $('#stockModal').show();

    // Trigger change to show current stock
    $('#stockProductId').trigger('change');
}

function addStock() {
    var token = getToken();
    if (!token) {
        showToast('Security token missing. Please refresh the page.', 'error');
        return;
    }

    var productId = parseInt($('#stockProductId').val());
    var supplierId = parseInt($('#stockSupplierId').val());
    var quantity = parseInt($('#stockQuantity').val());
    var remarks = $('#stockRemarks').val();

    if (!productId) {
        showToast('Please select a product', 'error');
        return;
    }

    if (!supplierId) {
        showToast('Please select a supplier', 'error');
        return;
    }

    if (!quantity || quantity <= 0) {
        showToast('Please enter valid quantity', 'error');
        return;
    }

    var formData = new FormData();
    formData.append('ProductId', productId);
    formData.append('SupplierId', supplierId);
    formData.append('QuantityAdded', quantity);
    formData.append('Remarks', remarks);
    formData.append('Date', new Date().toISOString().split('T')[0]);

    var submitBtn = $('#stockForm button[type="submit"]');
    var originalHtml = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Adding...');

    $.ajax({
        url: '/Stock/AddStockIn',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            console.log("Add stock response:", response);
            if (response.success) {
                showToast(response.message || 'Stock added successfully!', 'success');
                $('#stockModal').hide();
                loadLowStockReport();
                loadProducts(); // Refresh products dropdown
                $('#stockForm')[0].reset();
                $('#stockProductId').val('');
                $('#stockCurrentStock').val('');
            } else {
                showToast(response.message || 'Failed to add stock', 'error');
            }
        },
        error: function (xhr) {
            console.error("Error:", xhr);
            var errorMsg = 'Error adding stock';
            try {
                var response = JSON.parse(xhr.responseText);
                errorMsg = response.message || errorMsg;
            } catch (e) {
                errorMsg = xhr.status + ': ' + xhr.statusText;
            }
            showToast(errorMsg, 'error');
        },
        complete: function () {
            submitBtn.prop('disabled', false).html(originalHtml);
        }
    });
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str).replace(/[&<>]/g, function (m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}