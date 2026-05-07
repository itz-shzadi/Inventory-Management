// ==================== SUPPLIER MANAGEMENT AJAX MODULE ====================

var SupplierManagement = (function () {
    'use strict';

    // Private variables
    var isEditMode = false;
    var currentSearchTerm = '';

    // Improved showToast function
    function showToast(message, type) {
        // Remove existing toast
        $('#customToast').remove();

        // Create new toast
        var toast = $('<div id="customToast">' + message + '</div>');
        $('body').append(toast);

        // Style the toast
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
            'fontFamily': 'Arial, sans-serif',
            'boxShadow': '0 2px 5px rgba(0,0,0,0.2)',
            'animation': 'slideIn 0.3s ease-out'
        });

        // Add animation keyframes if not exists
        if (!$('#toastAnimations').length) {
            $('head').append(`
                <style id="toastAnimations">
                    @keyframes slideIn {
                        from { transform: translateX(100%); opacity: 0; }
                        to { transform: translateX(0); opacity: 1; }
                    }
                </style>
            `);
        }

        // Show and auto-hide
        toast.fadeIn(300);
        setTimeout(function () {
            toast.fadeOut(300, function () {
                $(this).remove();
            });
        }, 3000);
    }

    // Get CSRF token
    function getToken() {
        var token = $('input[name="__RequestVerificationToken"]').val();
        if (!token) {
            token = $('[name="__RequestVerificationToken"]').val();
        }
        if (!token) {
            console.warn('Anti-forgery token not found');
            return '';
        }
        return token;
    }

    // Escape HTML
    function escapeHtml(str) {
        if (!str) return '';
        return $('<div>').text(str).html();
    }

    // Load suppliers
    function loadSuppliers() {
        var url = currentSearchTerm ? '/Supplier/Search' : '/Supplier/GetAllSuppliers';
        var data = currentSearchTerm ? { searchTerm: currentSearchTerm } : {};

        $.ajax({
            url: url,
            type: 'GET',
            data: data,
            dataType: 'json',
            success: function (response) {
                displaySuppliers(response);
                updateTotalCount(response.length);
            },
            error: function (xhr, status, error) {
                console.error('Error loading suppliers:', error);
                $('#supplierTableBody').html('<tr><td colspan="5" style="text-align:center;color:red;">Error loading suppliers</td></tr>');
                showToast('Error loading suppliers', 'error');
            }
        });
    }

    // Display suppliers
    function displaySuppliers(suppliers) {
        if (!suppliers || suppliers.length === 0) {
            $('#supplierTableBody').html('<tr><td colspan="5" style="text-align:center;">No suppliers found</td></tr>');
            return;
        }

        var html = '';
        for (var i = 0; i < suppliers.length; i++) {
            var sup = suppliers[i];
            var address = sup.address ? (sup.address.length > 30 ? sup.address.substring(0, 30) + '...' : sup.address) : '—';
            var email = sup.email ? escapeHtml(sup.email) : '—';

            html += '<tr data-id="' + sup.supplierId + '">';
            html += '<td><strong>' + escapeHtml(sup.supplierName) + '</strong></td>';
            html += '<td><i class="fas fa-phone-alt"></i> ' + escapeHtml(sup.contactNo) + '</td>';
            html += '<td>' + email + '</td>';
            html += '<td>' + address + '</td>';
            html += '<td><div class="action-buttons">';
            html += '<button class="edit-btn" onclick="SupplierManagement.editSupplier(' + sup.supplierId + ')"><i class="fas fa-edit"></i> Edit</button>';
            html += '<button class="delete-btn" onclick="SupplierManagement.deleteSupplier(' + sup.supplierId + ')"><i class="fas fa-trash"></i> Delete</button>';
            html += '</div></td>';
            html += '</tr>';
        }
        $('#supplierTableBody').html(html);
    }

    function updateTotalCount(count) {
        $('#totalSuppliers').text(count);
        $('#displayCount').text(count);
    }

    // Create supplier
    function createSupplier(supplierData) {
        $.ajax({
            url: '/Supplier/Create',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(supplierData),
            headers: {
                'RequestVerificationToken': getToken()
            },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Saving...');
            },
            success: function (response) {
                if (response && response.success) {
                    showToast(response.message || 'Supplier created successfully!', 'success');
                    resetForm();
                    loadSuppliers();
                } else {
                    showToast(response?.message || 'Error creating supplier', 'error');
                }
            },
            error: function (xhr) {
                var errorMsg = 'Server error occurred';
                try {
                    var response = JSON.parse(xhr.responseText);
                    errorMsg = response.message || errorMsg;
                } catch (e) { }
                showToast(errorMsg, 'error');
                console.error('Create error:', xhr);
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Supplier');
            }
        });
    }

    // Update supplier
    function updateSupplier(supplierData) {
        $.ajax({
            url: '/Supplier/Edit',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(supplierData),
            headers: {
                'RequestVerificationToken': getToken()
            },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Updating...');
            },
            success: function (response) {
                if (response && response.success) {
                    showToast(response.message || 'Supplier updated successfully!', 'success');
                    resetForm();
                    loadSuppliers();
                } else {
                    showToast(response?.message || 'Error updating supplier', 'error');
                }
            },
            error: function (xhr) {
                var errorMsg = 'Server error occurred';
                try {
                    var response = JSON.parse(xhr.responseText);
                    errorMsg = response.message || errorMsg;
                } catch (e) { }
                showToast(errorMsg, 'error');
                console.error('Update error:', xhr);
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-pen"></i> Update Supplier');
            }
        });
    }

    // Delete supplier
    function deleteSupplier(id) {
        var row = $('tr[data-id="' + id + '"]');
        var supplierName = row.find('td:first strong').text() || 'this supplier';

        if (confirm('⚠️ Are you sure you want to delete "' + supplierName + '"?')) {
            $.ajax({
                url: '/Supplier/Delete/' + id,
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                success: function (response) {
                    if (response.success) {
                        showToast('Supplier deleted successfully!', 'success');
                        loadSuppliers();
                    } else {
                        showToast(response.message || 'Error deleting supplier', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting supplier', 'error');
                    console.error('Delete error:', xhr);
                }
            });
        }
    }

    // Edit supplier
    function editSupplier(id) {
        $.ajax({
            url: '/Supplier/Details/' + id,
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response && response.success) {
                    isEditMode = true;
                    $('#supplierId').val(response.supplierId);
                    $('#supplierName').val(response.supplierName);
                    $('#contactNo').val(response.contactNo);
                    $('#email').val(response.email || '');
                    $('#address').val(response.address || '');
                    $('#formTitle').text('Edit Supplier');
                    $('#submitBtn').html('<i class="fas fa-pen"></i> Update Supplier');
                    $('#cancelBtn').show();
                    $('html, body').animate({ scrollTop: 0 }, 300);
                } else {
                    showToast('Supplier not found', 'error');
                }
            },
            error: function (xhr) {
                showToast('Error loading supplier details', 'error');
                console.error('Edit load error:', xhr);
            }
        });
    }

    // Reset form
    function resetForm() {
        isEditMode = false;
        $('#supplierForm')[0].reset();
        $('#supplierId').val('0');
        $('#formTitle').text('Add New Supplier');
        $('#submitBtn').html('<i class="fas fa-save"></i> Save Supplier');
        $('#cancelBtn').hide();
        $('.error').html('');
        $('#email').val(''); // Clear email field
    }

    // Search functionality
    function searchSuppliers() {
        currentSearchTerm = $('#searchInput').val().trim();
        loadSuppliers();
    }

    function resetSearch() {
        $('#searchInput').val('');
        currentSearchTerm = '';
        loadSuppliers();
    }

    function deleteAllSuppliers() {
        if (confirm('⚠️ DANGER: This will delete ALL suppliers!\n\nAre you absolutely sure?')) {
            $.ajax({
                url: '/Supplier/DeleteAll',
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                beforeSend: function () {
                    $('#deleteAllBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Deleting...');
                },
                success: function (response) {
                    if (response.success) {
                        showToast('All suppliers deleted successfully!', 'success');
                        loadSuppliers();
                    } else {
                        showToast(response.message || 'Error deleting suppliers', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting suppliers', 'error');
                    console.error('Delete all error:', xhr);
                },
                complete: function () {
                    $('#deleteAllBtn').prop('disabled', false).html('<i class="fas fa-trash-alt"></i> Delete All');
                }
            });
        }
    }

    // Form validation and submit
    function validateAndSubmit(e) {
        e.preventDefault();

        $('.error').html('');

        var supplierName = $('#supplierName').val().trim();
        var contactNo = $('#contactNo').val().trim();
        var email = $('#email').val().trim();
        var address = $('#address').val().trim();

        // Validation
        if (!supplierName) {
            showToast('Supplier name is required', 'error');
            return false;
        }

        if (supplierName.length < 2) {
            showToast('Supplier name must be at least 2 characters', 'error');
            return false;
        }

        if (!contactNo) {
            showToast('Contact number is required', 'error');
            return false;
        }

        // Count digits only
        var digits = contactNo.replace(/[^0-9]/g, '');
        if (digits.length < 10) {
            showToast('Contact number must have at least 10 digits', 'error');
            return false;
        }

        // Email validation (only if provided)
        if (email && !isValidEmail(email)) {
            showToast('Invalid email format', 'error');
            return false;
        }

        var supplierData = {
            SupplierId: parseInt($('#supplierId').val()) || 0,
            SupplierName: supplierName,
            ContactNo: contactNo,
            Email: email || null,
            Address: address || null
        };

        if (isEditMode) {
            updateSupplier(supplierData);
        } else {
            createSupplier(supplierData);
        }

        return false;
    }

    function isValidEmail(email) {
        var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    }

    // Initialization
    function init() {
        loadSuppliers();

        $('#supplierForm').on('submit', validateAndSubmit);
        $('#cancelBtn').on('click', resetForm);
        $('#searchBtn').on('click', searchSuppliers);
        $('#resetSearchBtn').on('click', resetSearch);
        $('#deleteAllBtn').on('click', deleteAllSuppliers);

        $('#searchInput').on('keypress', function (e) {
            if (e.which === 13) {
                searchSuppliers();
            }
        });

        // Initialize cancel button hidden
        $('#cancelBtn').hide();
    }

    // Public API
    return {
        init: init,
        editSupplier: editSupplier,
        deleteSupplier: deleteSupplier,
        resetForm: resetForm
    };
})();

// Initialize when document is ready
$(document).ready(function () {
    SupplierManagement.init();
});