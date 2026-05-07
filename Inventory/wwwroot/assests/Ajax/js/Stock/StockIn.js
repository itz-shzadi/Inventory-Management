// StockIn.js - FIXED VERSION with Total Updates

$(document).ready(function () {
    console.log("StockIn.js loaded - FIXED VERSION");

    // Set default date
    var today = new Date().toISOString().split('T')[0];
    $('#stockDate').val(today);
    $('#filterDate').val('');

    // Load data
    loadProducts();
    loadSuppliers();
    loadStockInHistory();

    // ============ FILE UPLOAD - PURE JAVASCRIPT ============
    var uploadContainer = document.getElementById('fileUploadContainer');
    var fileInput = document.getElementById('documentFile');

    if (uploadContainer && fileInput) {
        var newContainer = uploadContainer.cloneNode(true);
        uploadContainer.parentNode.replaceChild(newContainer, uploadContainer);
        uploadContainer = newContainer;

        var newFileInput = fileInput.cloneNode(true);
        fileInput.parentNode.replaceChild(newFileInput, fileInput);
        fileInput = newFileInput;

        uploadContainer.addEventListener('click', function (e) {
            e.stopPropagation();
            console.log("Container clicked - opening file dialog");
            fileInput.click();
        });

        fileInput.addEventListener('change', function (e) {
            console.log("File input changed");
            if (this.files && this.files.length > 0) {
                handleFileSelect(this.files[0]);
            }
        });

        uploadContainer.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.stopPropagation();
            this.classList.add('dragover');
        });

        uploadContainer.addEventListener('dragleave', function (e) {
            e.preventDefault();
            e.stopPropagation();
            this.classList.remove('dragover');
        });

        uploadContainer.addEventListener('drop', function (e) {
            e.preventDefault();
            e.stopPropagation();
            this.classList.remove('dragover');
            var files = e.dataTransfer.files;
            if (files && files.length > 0) {
                handleFileSelect(files[0]);
                fileInput.files = files;
            }
        });
    } else {
        console.error("File upload elements not found!");
    }

    // ============ FORM SUBMIT ============
    $('#stockInForm').off('submit').on('submit', function (e) {
        e.preventDefault();
        addStockIn();
        return false;
    });

    // ============ BUTTONS ============
    $('#searchBtn').off('click').on('click', function (e) {
        e.preventDefault();
        loadStockInHistory();
        return false;
    });

    $('#resetBtn').off('click').on('click', function (e) {
        e.preventDefault();
        $('#searchInput').val('');
        $('#filterDate').val('');
        loadStockInHistory();
        return false;
    });

    $('#resetFormBtn').off('click').on('click', function (e) {
        e.preventDefault();
        resetForm();
        return false;
    });

    $('#closeDocModal').off('click').on('click', function (e) {
        e.preventDefault();
        $('#documentModal').hide();
        return false;
    });

    $(window).off('click').on('click', function (e) {
        if ($(e.target).is('#documentModal')) {
            $('#documentModal').hide();
        }
    });
});

// Global variable
var selectedFile = null;

// ============ FILE HANDLING ============
function handleFileSelect(file) {
    if (!file) return;

    console.log("Processing file:", file.name);

    if (file.size > 5 * 1024 * 1024) {
        showToast('File size must be less than 5MB', 'error');
        return;
    }

    var allowedExt = ['jpg', 'jpeg', 'png', 'pdf', 'doc', 'docx'];
    var fileExt = file.name.split('.').pop().toLowerCase();

    if (!allowedExt.includes(fileExt)) {
        showToast('Unsupported file format. Please upload PDF, JPG, PNG, or DOC files.', 'error');
        return;
    }

    selectedFile = file;
    var fileSizeKB = (file.size / 1024).toFixed(2);

    var fileHtml = '<div class="selected-file">' +
        '<i class="fas fa-file"></i> ' + escapeHtml(file.name) + ' (' + fileSizeKB + ' KB) ' +
        '<i class="fas fa-times-circle remove-file" style="cursor:pointer; color:#dc3545; margin-left:8px;"></i>' +
        '</div>';

    $('#fileInfo').html(fileHtml);

    $('.remove-file').off('click').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        selectedFile = null;
        $('#fileInfo').empty();
        $('#documentFile').val('');
        console.log("File removed");
        return false;
    });

    showToast('File selected: ' + file.name, 'success');
}

// ============ FORM FUNCTIONS ============
function resetForm() {
    var today = new Date().toISOString().split('T')[0];
    $('#stockDate').val(today);
    $('#productId').val('');
    $('#supplierId').val('');
    $('#quantity').val('');
    $('#unitPrice').val('');
    $('#remarks').val('');
    selectedFile = null;
    $('#fileInfo').empty();
    $('#documentFile').val('');
    $('.error').text('');
}

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

// ============ LOAD DATA ============
function loadProducts() {
    $.ajax({
        url: '/Stock/GetAllProducts',
        type: 'GET',
        success: function (products) {
            var options = '<option value="">Select Product</option>';
            if (products && products.length) {
                for (var i = 0; i < products.length; i++) {
                    options += '<option value="' + products[i].productId + '">' +
                        escapeHtml(products[i].productName) +
                        ' (Stock: ' + (products[i].quantity || 0) + ')</option>';
                }
            }
            $('#productId').html(options);
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
            $('#supplierId').html(options);
        },
        error: function (xhr) {
            console.error('Error loading suppliers:', xhr);
            showToast('Error loading suppliers', 'error');
        }
    });
}

// ============ ADD STOCK ============
function addStockIn() {
    var productId = $('#productId').val();
    var supplierId = $('#supplierId').val();
    var quantity = $('#quantity').val();
    var stockDate = $('#stockDate').val();
    var unitPrice = $('#unitPrice').val();
    var remarks = $('#remarks').val();

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
    if (!stockDate) {
        showToast('Please select date', 'error');
        return;
    }

    var formData = new FormData();
    formData.append('ProductId', productId);
    formData.append('SupplierId', supplierId);
    formData.append('QuantityAdded', parseInt(quantity));
    formData.append('Date', stockDate);
    formData.append('UnitPrice', (unitPrice && unitPrice > 0) ? parseFloat(unitPrice) : 0);
    formData.append('Remarks', remarks || '');

    if (selectedFile) {
        formData.append('Document', selectedFile);
    }

    var submitBtn = $('#submitBtn');
    var originalHtml = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Adding...');

    $.ajax({
        url: '/Stock/AddStockIn',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            'RequestVerificationToken': getToken()
        },
        success: function (response) {
            if (response.success) {
                showToast(response.message || 'Stock added successfully!', 'success');
                resetForm();
                loadStockInHistory();
                loadProducts();
            } else {
                showToast(response.message || 'Failed to add stock', 'error');
            }
        },
        error: function (xhr) {
            console.error('Error:', xhr);
            var msg = 'Error adding stock';
            try {
                var response = JSON.parse(xhr.responseText);
                msg = response.message || response.error || msg;
            } catch (e) {
                msg = xhr.status + ': ' + (xhr.statusText || 'Server Error');
            }
            showToast(msg, 'error');
        },
        complete: function () {
            submitBtn.prop('disabled', false).html(originalHtml);
        }
    });
}

// ============ LOAD STOCK HISTORY ============
function loadStockInHistory() {
    var filterDate = $('#filterDate').val();
    var searchTerm = $('#searchInput').val();

    $('#stockInTableBody').html('<tr><td colspan="8" style="text-align:center;">Loading...<\/td><\/tr>');

    var params = {};
    if (filterDate) {
        params.fromDate = filterDate;
        params.toDate = filterDate;
    }
    if (searchTerm && searchTerm.trim()) {
        params.searchTerm = searchTerm.trim();
    }

    $.ajax({
        url: '/Stock/GetStockInHistory',
        type: 'GET',
        data: params,
        success: function (data) {
            displayStockInHistory(data);
            updateTotals(data);
        },
        error: function (xhr) {
            console.error('Error loading history:', xhr);
            $('#stockInTableBody').html('<tr><td colspan="8" style="text-align:center;color:red;">Error loading history<\/td><\/tr>');
            showToast('Error loading stock history', 'error');
        }
    });
}

// ============ UPDATE TOTALS - FIXED ============
function updateTotals(data) {
    console.log("Updating totals with data:", data);

    var totalQty = 0;
    var totalVal = 0;
    var todayTotal = 0;
    var today = new Date().toISOString().split('T')[0];

    if (data && data.length) {
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            var qty = item.quantityAdded || 0;
            var price = item.unitPrice || 0;

            totalQty += qty;
            totalVal += qty * price;

            // Calculate today's total
            if (item.date) {
                var itemDate = new Date(item.date).toISOString().split('T')[0];
                if (itemDate === today) {
                    todayTotal += qty;
                }
            }
        }
    }

    // Update Header Stats (using correct IDs from HTML)
    $('#todayStockIn').text(todayTotal);
    $('#totalQuantityHeader').text(totalQty);
    $('#totalValueHeader').text('$' + totalVal.toFixed(2));

    // Also update footer totals if they exist
    if ($('#totalQuantity').length) {
        $('#totalQuantity').text(totalQty);
    }
    if ($('#totalValue').length) {
        $('#totalValue').text('$' + totalVal.toFixed(2));
    }

    console.log("Totals updated - Quantity:", totalQty, "Value:", totalVal, "Today:", todayTotal);
}

// ============ DISPLAY HISTORY ============
function displayStockInHistory(data) {
    if (!data || data.length === 0) {
        $('#stockInTableBody').html('<tr><td colspan="8" style="text-align:center;">No records found<\/td><\/tr>');
        $('#showingCount').text('0');
        return;
    }

    var html = '';
    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var total = ((item.quantityAdded || 0) * (item.unitPrice || 0)).toFixed(2);

        var docHtml = '-';
        if (item.documentPath) {
            docHtml = '<a href="javascript:void(0)" onclick="viewDocument(\'' + escapeHtml(item.documentPath) + '\')" style="cursor:pointer; color:#007bff;">' +
                '<i class="fas fa-file-pdf"></i> View</a>';
        }

        html += '<tr>';
        html += '<td>' + (item.date ? new Date(item.date).toLocaleDateString() : '-') + '<\/td>';
        html += '<td>' + escapeHtml(item.productName || '-') + '<\/td>';
        html += '<td>' + escapeHtml(item.supplierName || '-') + '<\/td>';
        html += '<td>' + (item.quantityAdded || 0) + '<\/td>';
        html += '<td>$' + (item.unitPrice || 0).toFixed(2) + '<\/td>';
        html += '<td>$' + total + '<\/td>';
        html += '<td>' + docHtml + '<\/td>';
        html += '<td>' + escapeHtml(item.remarks || '-') + '<\/td>';
        html += '<\/tr>';
    }

    $('#stockInTableBody').html(html);
    $('#showingCount').text(data.length);
}

function viewDocument(path) {
    if (path) {
        $('#docFrame').attr('src', path);
        $('#documentModal').show();
    }
}

function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str).replace(/[&<>]/g, function (m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}