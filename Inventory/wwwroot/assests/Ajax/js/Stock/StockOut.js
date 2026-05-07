// StockOut.js - FULLY FIXED VERSION

$(document).ready(function () {
    console.log("StockOut.js loaded");

    loadProducts();
    loadStockOutHistory();

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

    var postData = {
        ProductId: parseInt(productId),
        QuantityRemoved: parseInt(quantity),
        Reason: reason,
        Remarks: remarks || ''
    };

    console.log("Sending data:", postData);

    var submitBtn = $('#submitBtn');
    var originalHtml = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Processing...');

    // TRY THIS ENDPOINT FIRST - Match your controller
    $.ajax({
        url: '/Stock/RemoveStock',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(postData),
        headers: {
            'RequestVerificationToken': getToken()
        },
        success: function (response) {
            console.log("Success Response:", response);
            if (response.success) {
                showToast(response.message || 'Stock removed successfully!', 'success');
                $('#stockOutForm')[0].reset();
                $('#currentStock').val('');
                loadStockOutHistory();
                loadProducts();
            } else {
                showToast(response.message || 'Failed to remove stock', 'error');
            }
            submitBtn.prop('disabled', false).html(originalHtml);
        },
        error: function (xhr) {
            console.error("Error Response:", xhr);

            // Try alternative endpoint if first fails
            $.ajax({
                url: '/api/Stock/RemoveStock',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(postData),
                headers: {
                    'RequestVerificationToken': getToken()
                },
                success: function (response) {
                    console.log("Alternative endpoint success:", response);
                    if (response.success) {
                        showToast(response.message || 'Stock removed successfully!', 'success');
                        $('#stockOutForm')[0].reset();
                        $('#currentStock').val('');
                        loadStockOutHistory();
                        loadProducts();
                    } else {
                        showToast(response.message || 'Failed to remove stock', 'error');
                    }
                    submitBtn.prop('disabled', false).html(originalHtml);
                },
                error: function (xhr2) {
                    console.error("Both endpoints failed");
                    var msg = 'Error removing stock. Please check console.';
                    try {
                        var response = JSON.parse(xhr.responseText);
                        msg = response.message || msg;
                    } catch (e) { }
                    showToast(msg, 'error');
                    submitBtn.prop('disabled', false).html(originalHtml);
                }
            });
        }
    });
}

function loadStockOutHistory() {
    console.log("Loading stock out history...");
    $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;">Loading...<\/td><\/tr>');

    $.ajax({
        url: '/Stock/GetStockOutHistory',
        type: 'GET',
        success: function (data) {
            console.log("History loaded:", data);
            displayStockOutHistory(data);
            updateTotals(data);
        },
        error: function (xhr) {
            console.error("Error loading history:", xhr);
            $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;color:red;">Error loading history<\/td><\/tr>');
            showToast('Error loading stock out history', 'error');
        }
    });
}

function updateTotals(data) {
    var totalQty = 0;
    var todayTotal = 0;
    var today = new Date().toISOString().split('T')[0];

    if (data && data.length) {
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            var qty = item.quantityRemoved || 0;
            totalQty += qty;

            if (item.date) {
                var itemDate = new Date(item.date).toISOString().split('T')[0];
                if (itemDate === today) {
                    todayTotal += qty;
                }
            }
        }
    }

    $('#todayStockOut').text(todayTotal);
    if ($('#totalQuantityOut').length) {
        $('#totalQuantityOut').text(totalQty);
    }

    console.log("Totals - Total Qty:", totalQty, "Today:", todayTotal);
}

function displayStockOutHistory(data) {
    if (!data || data.length === 0) {
        $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;">No records found<\/td><\/tr>');
        $('#showingCount').text('0');
        return;
    }

    var html = '';
    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var reasonClass = (item.reason === 'Sale') ? 'badge-success' : 'badge-danger';

        html += '<tr>';
        html += '<td>' + (item.date ? new Date(item.date).toLocaleDateString() : '-') + '<\/td>';
        html += '<td>' + escapeHtml(item.productName || '-') + '<\/td>';
        html += '<td><strong>' + (item.quantityRemoved || 0) + '<\/strong><\/td>';
        html += '<td><span class="' + reasonClass + '">' + escapeHtml(item.reason || '-') + '<\/span><\/td>';
        html += '<td>' + escapeHtml(item.remarks || '-') + '<\/td>';
        html += '<\/tr>';
    }

    $('#stockOutTableBody').html(html);
    $('#showingCount').text(data.length);
}

function searchStockOut() {
    var searchTerm = $('#searchInput').val().trim();

    if (!searchTerm) {
        loadStockOutHistory();
        return;
    }

    $('#stockOutTableBody').html('<tr><td colspan="5" style="text-align:center;">Searching...<\/td><\/tr>');

    $.ajax({
        url: '/Stock/GetStockOutHistory',
        type: 'GET',
        success: function (data) {
            var filtered = data.filter(function (item) {
                return (item.productName && item.productName.toLowerCase().includes(searchTerm.toLowerCase())) ||
                    (item.reason && item.reason.toLowerCase().includes(searchTerm.toLowerCase()));
            });
            displayStockOutHistory(filtered);
            updateTotals(filtered);
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