// StockOut.js - FIXED VERSION

$(document).ready(function () {
    console.log("StockOut.js loaded");

    loadProducts();
    loadStockOutHistory();
    loadTodayStats(); // Added to load header stats

    $('#productId').on('change', function () {
        loadCurrentStock();
    });

    $('#stockOutForm').on('submit', function (e) {
        e.preventDefault();
        removeStock();
    });

    $('#searchBtn').on('click', function () {
        searchStockOut();
    });

    $('#resetBtn').on('click', function () {
        $('#searchInput').val('');
        loadStockOutHistory();
    });
});

function showToast(message, type) {
    var toast = $('#toast');
    toast.removeClass('success error').addClass(type).text(message);
    toast.fadeIn(300);
    setTimeout(function () {
        toast.fadeOut(300);
    }, 3000);
}

function getToken() {
    return $('input[name="__RequestVerificationToken"]').val();
}

function loadProducts() {
    console.log("Loading products...");
    $.ajax({
        url: '/Stock/GetAllProducts',
        type: 'GET',
        success: function (products) {
            console.log("Products loaded:", products);
            var options = '<option value="">Select Product</option>';
            if (products && products.length) {
                for (var i = 0; i < products.length; i++) {
                    var stock = products[i].quantity || 0;
                    var warning = '';
                    var style = '';
                    if (stock <= 10 && stock > 0) {
                        style = 'style="color:orange;"';
                        warning = ' ⚠️ Low Stock';
                    } else if (stock === 0) {
                        style = 'style="color:red;"';
                        warning = ' 🔴 OUT OF STOCK';
                    }
                    options += '<option value="' + products[i].productId + '" data-stock="' + stock + '" ' + style + '>' +
                        escapeHtml(products[i].productName) + ' (Stock: ' + stock + ')' + warning + '</option>';
                }
            }
            $('#productId').html(options);
        },
        error: function (xhr) {
            console.error("Error loading products:", xhr);
            showToast('Error loading products', 'error');
        }
    });
}

function loadCurrentStock() {
    var selected = $('#productId option:selected');
    var stock = parseInt(selected.data('stock')) || 0;

    if (stock === 0) {
        $('#currentStock').val('OUT OF STOCK - ' + stock + ' units');
        $('#currentStock').css('color', 'red').css('font-weight', 'bold');
    } else if (stock <= 10) {
        $('#currentStock').val(stock + ' units (Low Stock!)');
        $('#currentStock').css('color', 'orange').css('font-weight', 'bold');
    } else {
        $('#currentStock').val(stock + ' units available');
        $('#currentStock').css('color', 'green').css('font-weight', 'normal');
    }

    $('#quantity').attr('max', stock);
    $('#quantity').attr('min', 1);
}

function loadTodayStats() {
    $.ajax({
        url: '/Stock/GetTotalStockOut',
        type: 'GET',
        success: function (total) {
            $('#totalQuantityOut').text(total || 0);
        },
        error: function () {
            $('#totalQuantityOut').text('0');
        }
    });
}

function removeStock() {
    var productId = $('#productId').val();
    var quantity = $('#quantity').val();
    var reason = $('#reason').val();
    var remarks = $('#remarks').val();
    var selectedOption = $('#productId option:selected');
    var currentStock = parseInt(selectedOption.data('stock')) || 0;

    console.log("Remove Stock - Product:", productId, "Qty:", quantity, "Reason:", reason);

    // Validations
    if (!productId) {
        showToast('Please select a product', 'error');
        return;
    }

    if (!quantity || quantity <= 0) {
        showToast('Please enter valid quantity', 'error');
        return;
    }

    if (parseInt(quantity) > currentStock) {
        showToast('Cannot remove more than current stock (' + currentStock + ' units available)', 'error');
        return;
    }

    if (!reason) {
        showToast('Please select a reason', 'error');
        return;
    }

    // ============ FIX: Use FormData instead of JSON ============
    var formData = new FormData();
    formData.append('productId', parseInt(productId));
    formData.append('quantityRemoved', parseInt(quantity));
    formData.append('reason', reason);
    formData.append('remarks', remarks || '');

    console.log("Sending FormData:", {
        productId: parseInt(productId),
        quantityRemoved: parseInt(quantity),
        reason: reason,
        remarks: remarks || ''
    });

    var submitBtn = $('#submitBtn');
    var originalHtml = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Processing...');

    // ============ FIX: Send as FormData (matches backend expectations) ============
    $.ajax({
        url: '/Stock/RemoveStock',
        type: 'POST',
        data: formData,
        processData: false,  // Important for FormData
        contentType: false,   // Important for FormData
        headers: {
            'RequestVerificationToken': getToken()
        },
        success: function (response) {
            console.log("Success Response:", response);
            if (response.success) {
                showToast(response.message || 'Stock removed successfully!', 'success');
                // Reset form
                $('#stockOutForm')[0].reset();
                $('#currentStock').val('');
                // Reload data
                loadStockOutHistory();
                loadProducts();
                loadTodayStats();
            } else {
                showToast(response.message || 'Failed to remove stock', 'error');
            }
            submitBtn.prop('disabled', false).html(originalHtml);
        },
        error: function (xhr) {
            console.error("Error Response:", xhr);
            var msg = 'Error removing stock.';
            try {
                var response = JSON.parse(xhr.responseText);
                msg = response.message || msg;
            } catch (e) { }
            showToast(msg, 'error');
            submitBtn.prop('disabled', false).html(originalHtml);
        }
    });
}

function loadStockOutHistory() {
    console.log("Loading stock out history...");
    $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;">Loading...</td></tr>');

    $.ajax({
        url: '/Stock/GetStockOutHistory',
        type: 'GET',
        success: function (data) {
            console.log("History loaded:", data);
            displayStockOutHistory(data);
        },
        error: function (xhr) {
            console.error("Error loading history:", xhr);
            $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;color:red;">Error loading history</td></tr>');
            showToast('Error loading stock out history', 'error');
        }
    });
}

function displayStockOutHistory(data) {
    // Calculate totals for header
    var totalQty = 0;
    var todayTotal = 0;
    var today = new Date().toISOString().split('T')[0];

    if (!data || data.length === 0) {
        $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;">No records found</td></tr>');
        $('#showingCount').text('0');
        $('#todayStockOut').text('0');
        $('#totalQuantityOut').text('0');
        return;
    }

    var html = '';
    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var qty = item.quantityRemoved || 0;
        totalQty += qty;

        var itemDate = item.date ? new Date(item.date).toISOString().split('T')[0] : '';
        if (itemDate === today) {
            todayTotal += qty;
        }

        var reasonClass = (item.reason === 'Sale') ? 'badge-success' : 'badge-danger';

        html += '<tr>';
        html += '<td>' + (item.date ? new Date(item.date).toLocaleDateString() : '-') + '</td>';
        html += '<td>' + escapeHtml(item.productName || '-') + '</td>';
        html += '<td><strong>' + qty + '</strong></td>';
        html += '<td><span class="' + reasonClass + '">' + escapeHtml(item.reason || '-') + '</span></td>';
        html += '<td>' + escapeHtml(item.remarks || '-') + '</td>';
        html += '</tr>';
    }

    $('#stockOutTableBody').html(html);
    $('#showingCount').text(data.length);
    $('#todayStockOut').text(todayTotal);
    $('#totalQuantityOut').text(totalQty);
}

function searchStockOut() {
    var searchTerm = $('#searchInput').val().trim();

    if (!searchTerm) {
        loadStockOutHistory();
        return;
    }

    $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;">Searching...</td></tr>');

    $.ajax({
        url: '/Stock/GetStockOutHistory',
        type: 'GET',
        success: function (data) {
            var filtered = data.filter(function (item) {
                return (item.productName && item.productName.toLowerCase().includes(searchTerm.toLowerCase())) ||
                    (item.reason && item.reason.toLowerCase().includes(searchTerm.toLowerCase()));
            });
            displayStockOutHistory(filtered);
        },
        error: function () {
            showToast('Error searching', 'error');
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