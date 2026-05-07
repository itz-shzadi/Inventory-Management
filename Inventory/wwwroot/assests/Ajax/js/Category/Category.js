// ==================== CATEGORY MANAGEMENT AJAX MODULE ====================
// Yeh poori file AJAX calls ke liye hai - UserManagement ke pattern par

var CategoryManagement = (function () {
    'use strict';

    // Private variables
    var isEditMode = false;
    var currentFilterStatus = '';
    var currentSearchTerm = '';

    // ==================== HELPER FUNCTIONS ====================

    function showToast(message, type) {
        console.log("showToast called:", message, type);

        // Check if toast element exists
        var toast = $('#toast');
        if (toast.length === 0) {
            console.error("Toast element not found in DOM!");
            alert(message); // Fallback
            return;
        }

        // Remove all existing classes
        toast.removeClass();

        // Add toast class and type class
        toast.addClass('toast ' + type);

        // Set the message
        toast.html('<i class="fas ' + (type === 'success' ? 'fa-check-circle' : (type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle')) + '"></i> ' + message);

        // Force CSS styles directly (override any conflicts)
        toast.css({
            'display': 'block',
            'position': 'fixed',
            'bottom': '30px',
            'right': '30px',
            'z-index': '9999',
            'padding': '14px 24px',
            'border-radius': '12px',
            'color': 'white',
            'font-weight': '500',
            'box-shadow': '0 8px 20px rgba(0,0,0,0.15)',
            'animation': 'slideIn 0.3s ease-out'
        });

        // Set background color based on type
        if (type === 'success') {
            toast.css('background', '#10b981');
        } else if (type === 'error') {
            toast.css('background', '#ef4444');
        } else {
            toast.css('background', '#3b82f6');
        }

        // Auto hide after 3 seconds
        setTimeout(function () {
            toast.fadeOut(300, function () {
                toast.css('display', 'none');
            });
        }, 3000);
    }

    function getToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/[&<>]/g, function (m) {
            if (m === '&') return '&amp;';
            if (m === '<') return '&lt;';
            if (m === '>') return '&gt;';
            return m;
        });
    }

    // Description ko truncate karne ka function
    function truncateText(text, maxLength) {
        if (!text) return '';
        if (text.length <= maxLength) return escapeHtml(text);
        return escapeHtml(text.substring(0, maxLength)) + '...';
    }

    // ==================== LOAD CATEGORIES (AJAX CALL) ====================

    function loadCategories() {
        var url = '/Category/GetAllCategories';
        var params = {};

        if (currentSearchTerm) {
            url = '/Category/Search';
            params = { searchTerm: currentSearchTerm };
        } else if (currentFilterStatus) {
            url = '/Category/GetByStatus';
            params = { status: currentFilterStatus };
        }

        $.ajax({
            url: url,
            type: 'GET',
            data: params,
            dataType: 'json',
            success: function (categories) {
                displayCategories(categories);
                updateTotalCount(categories.length);
                $('#displayCount').text(categories.length);
            },
            error: function (xhr, status, error) {
                console.error('Error loading categories:', error);
                $('#categoryTableBody').html('<tr><td colspan="4" style="text-align:center;color:red;">Error loading categories</td></tr>');
                showToast('Error loading categories', 'error');
            }
        });
    }

    // ==================== DISPLAY CATEGORIES ====================

    function displayCategories(categories) {
        if (!categories || categories.length === 0) {
            $('#categoryTableBody').html('<tr><td colspan="4" style="text-align:center;">No categories found</td></tr>');
            return;
        }

        var html = '';
        for (var i = 0; i < categories.length; i++) {
            var cat = categories[i];
            var description = truncateText(cat.description, 35);
            var statusClass = cat.status === 'Active' ? 'badge-success' : 'badge-danger';

            html += '<tr data-id="' + cat.id + '">';
            html += '<td><strong>' + escapeHtml(cat.name) + '</strong></td>';
            html += '<td>' + (description || '<span style="color:#aaa;">—</span>') + '</td>';
            html += '<td><span class="badge ' + statusClass + '"><i class="fas fa-circle"></i> ' + escapeHtml(cat.status) + '</span></td>';
            html += '<td><div class="action-buttons">';
            html += '<button class="edit-btn" onclick="CategoryManagement.editCategory(' + cat.id + ')"><i class="fas fa-edit"></i> Edit</button>';
            html += '<button class="delete-btn" onclick="CategoryManagement.deleteCategory(' + cat.id + ')"><i class="fas fa-trash"></i> Delete</button>';
            html += '</div></td></tr>';
        }
        $('#categoryTableBody').html(html);
    }

    function updateTotalCount(count) {
        $('#totalCategories').text(count);
    }

    // ==================== CREATE CATEGORY (AJAX POST CALL) ====================

    function createCategory(categoryData) {
        console.log("📤 Sending create request:", categoryData);

        $.ajax({
            url: '/Category/Create',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(categoryData),
            headers: {
                'RequestVerificationToken': getToken(),
                'Accept': 'application/json'
            },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Saving...');
            },
            success: function (response) {
                console.log("✅ Create response:", response);

                if (response && response.success) {
                    showToast(response.message || 'Category created successfully!', 'success');
                    resetForm();
                    loadCategories();
                    loadCategoryCount();
                } else {
                    var errorMsg = response?.message || 'Unknown error occurred';
                    console.error("❌ Create failed:", errorMsg);
                    showToast(errorMsg, 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error("❌ AJAX Error:", { xhr, status, error });

                // Try to get error message from response
                var errorMsg = "Server error occurred";
                try {
                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMsg = xhr.responseJSON.message;
                    } else if (xhr.responseText) {
                        var parsed = JSON.parse(xhr.responseText);
                        if (parsed.message) errorMsg = parsed.message;
                    }
                } catch (e) {
                    console.error("Parse error:", e);
                }

                showToast(errorMsg, 'error');
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Category');
            }
        });
    }

    // ==================== UPDATE CATEGORY (AJAX POST CALL) ====================

    function updateCategory(categoryData) {
        console.log("📤 Sending update request:", categoryData);

        $.ajax({
            url: '/Category/Edit',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(categoryData),
            headers: {
                'RequestVerificationToken': getToken(),
                'Accept': 'application/json'
            },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Updating...');
            },
            success: function (response) {
                console.log("✅ Update response:", response);

                if (response && response.success) {
                    showToast(response.message || 'Category updated successfully!', 'success');
                    resetForm();
                    loadCategories();
                    loadCategoryCount();
                } else {
                    var errorMsg = response?.message || 'Update failed';
                    showToast(errorMsg, 'error');
                }
            },
            error: function (xhr) {
                console.error("❌ Update AJAX Error:", xhr);
                var errorMsg = xhr.responseJSON?.message || xhr.statusText || 'Server error';
                showToast(errorMsg, 'error');
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-pen"></i> Update Category');
            }
        });
    }

    // ==================== DELETE CATEGORY (SOFT DELETE - AJAX POST CALL) ====================

    function deleteCategory(id) {
        // Get category name from table row
        var row = $('tr[data-id="' + id + '"]');
        var categoryName = row.find('td:first strong').text() || 'this category';

        if (confirm('⚠️ Are you sure you want to delete "' + categoryName + '"?\nThis will soft delete the category.')) {
            $.ajax({
                url: '/Category/Delete/' + id,
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                headers: { 'Accept': 'application/json' },
                success: function (response) {
                    if (response.success) {
                        showToast('Category soft deleted successfully!', 'success');
                        loadCategories();
                        loadCategoryCount();
                    } else {
                        showToast(response.message || 'Error deleting category', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting category', 'error');
                    console.error('Delete error:', xhr);
                }
            });
        }
    }

    // ==================== DELETE ALL CATEGORIES (SOFT DELETE - AJAX POST CALL) ====================

    function deleteAllCategories() {
        if (confirm('⚠️ DANGER: This will soft delete ALL categories!\n\nAre you absolutely sure?')) {
            $.ajax({
                url: '/Category/DeleteAll',
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                headers: { 'Accept': 'application/json' },
                beforeSend: function () {
                    $('#deleteAllBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Deleting...');
                },
                success: function (response) {
                    if (response.success) {
                        showToast('All categories soft deleted successfully!', 'success');
                        loadCategories();
                        loadCategoryCount();
                    } else {
                        showToast(response.message || 'Error deleting categories', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting categories', 'error');
                    console.error('Delete all error:', xhr);
                },
                complete: function () {
                    $('#deleteAllBtn').prop('disabled', false).html('<i class="fas fa-trash-alt"></i> Delete All');
                }
            });
        }
    }

    // ==================== RESTORE CATEGORY (AJAX POST CALL) ====================

    function restoreCategory(id) {
        if (confirm('Restore this category?')) {
            $.ajax({
                url: '/Category/Restore/' + id,
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                headers: { 'Accept': 'application/json' },
                success: function (response) {
                    if (response.success) {
                        showToast('Category restored successfully!', 'success');
                        loadCategories();
                        loadCategoryCount();
                    } else {
                        showToast(response.message || 'Error restoring category', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error restoring category', 'error');
                    console.error('Restore error:', xhr);
                }
            });
        }
    }

    // ==================== PERMANENT DELETE CATEGORY (AJAX POST CALL) ====================

    function permanentDeleteCategory(id) {
        if (confirm('⚠️ PERMANENT DELETE: This action cannot be undone!\n\nAre you sure?')) {
            $.ajax({
                url: '/Category/PermanentDelete/' + id,
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                headers: { 'Accept': 'application/json' },
                success: function (response) {
                    if (response.success) {
                        showToast('Category permanently deleted!', 'success');
                        loadCategories();
                        loadCategoryCount();
                    } else {
                        showToast(response.message || 'Error deleting category', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting category', 'error');
                    console.error('Permanent delete error:', xhr);
                }
            });
        }
    }

    // ==================== EDIT CATEGORY (AJAX GET CALL) ====================

    function editCategory(id) {
        $.ajax({
            url: '/Category/Details/' + id,
            type: 'GET',
            dataType: 'json',
            success: function (category) {
                if (category) {
                    isEditMode = true;
                    $('#categoryId').val(category.id);
                    $('#categoryName').val(category.name);
                    $('#description').val(category.description);
                    $('#status').val(category.status);
                    $('#formTitle').text('Edit Category');
                    $('#submitBtn').html('<i class="fas fa-pen"></i> Update Category');
                    $('#cancelBtn').show();

                    // Scroll to top smoothly
                    $('html, body').animate({ scrollTop: 0 }, 300);

                    showToast('Edit mode: Update category details', 'info');
                } else {
                    showToast('Category not found', 'error');
                }
            },
            error: function (xhr) {
                showToast('Error loading category details', 'error');
                console.error('Edit load error:', xhr);
            }
        });
    }

    // ==================== LOAD CATEGORY COUNT (AJAX GET CALL) ====================

    function loadCategoryCount() {
        $.ajax({
            url: '/Category/GetCount',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                $('#totalCategories').text(response.count);
            },
            error: function (xhr) {
                console.error('Count error:', xhr);
                $('#totalCategories').text('0');
            }
        });
    }

    // ==================== RESET FORM ====================

    function resetForm() {
        isEditMode = false;
        $('#categoryForm')[0].reset();
        $('#categoryId').val('0');
        $('#status').val('Active');
        $('#formTitle').text('Add New Category');
        $('#submitBtn').html('<i class="fas fa-save"></i> Save Category');
        $('#cancelBtn').hide();
        $('.error').html('');
    }

    // ==================== SEARCH FUNCTIONALITY ====================

    function searchCategories() {
        currentSearchTerm = $('#searchInput').val().trim();
        currentFilterStatus = '';
        $('#statusFilter').val('');
        loadCategories();
    }

    function resetSearch() {
        $('#searchInput').val('');
        currentSearchTerm = '';
        currentFilterStatus = '';
        $('#statusFilter').val('');
        loadCategories();
    }

    function filterByStatus() {
        currentFilterStatus = $('#statusFilter').val();
        currentSearchTerm = '';
        $('#searchInput').val('');
        loadCategories();
    }

    // ==================== FORM VALIDATION AND SUBMIT ====================

    function validateAndSubmit(e) {
        e.preventDefault();

        // Clear previous errors
        $('.error').html('');

        var categoryId = $('#categoryId').val();
        var categoryName = $('#categoryName').val().trim();
        var description = $('#description').val().trim();
        var status = $('#status').val();

        var isValid = true;

        // Category Name validation
        if (!categoryName) {
            $('#nameError').html('Category name is required');
            isValid = false;
        } else if (categoryName.length < 2) {
            $('#nameError').html('Category name must be at least 2 characters');
            isValid = false;
        } else if (categoryName.length > 100) {
            $('#nameError').html('Category name cannot exceed 100 characters');
            isValid = false;
        }

        // Description validation (optional but max length check)
        if (description && description.length > 500) {
            $('#descError').html('Description cannot exceed 500 characters');
            isValid = false;
        }

        // Status validation
        if (!status) {
            $('#statusError').html('Status is required');
            isValid = false;
        }

        if (!isValid) {
            showToast('Please fix the validation errors', 'error');
            return false;
        }

        var categoryData = {
            Id: parseInt(categoryId) || 0,
            Name: categoryName,
            Description: description || null,
            Status: status
        };

        if (isEditMode) {
            updateCategory(categoryData);
        } else {
            createCategory(categoryData);
        }

        return false;
    }

    // ==================== LOAD DELETED CATEGORIES (FOR TRASH VIEW - OPTIONAL) ====================

    function loadDeletedCategories() {
        $.ajax({
            url: '/Category/GetDeletedCategories',
            type: 'GET',
            dataType: 'json',
            success: function (categories) {
                displayDeletedCategories(categories);
            },
            error: function (xhr) {
                console.error('Error loading deleted categories:', xhr);
            }
        });
    }

    function displayDeletedCategories(categories) {
        if (!categories || categories.length === 0) {
            $('#categoryTableBody').html('<tr><td colspan="4" style="text-align:center;">No deleted categories found</td></tr>');
            return;
        }

        var html = '';
        for (var i = 0; i < categories.length; i++) {
            var cat = categories[i];
            var description = truncateText(cat.description, 35);

            html += '<tr data-id="' + cat.id + '" style="opacity:0.7;">';
            html += '<td><strong>' + escapeHtml(cat.name) + '</strong> <span class="deleted-badge">Deleted</span></td>';
            html += '<td>' + (description || '<span style="color:#aaa;">—</span>') + '</td>';
            html += '<td><span class="badge badge-danger"><i class="fas fa-circle"></i> ' + escapeHtml(cat.status) + '</span></td>';
            html += '<td><div class="action-buttons">';
            html += '<button class="restore-btn" onclick="CategoryManagement.restoreCategory(' + cat.id + ')"><i class="fas fa-undo"></i> Restore</button>';
            html += '<button class="delete-permanent-btn" onclick="CategoryManagement.permanentDeleteCategory(' + cat.id + ')"><i class="fas fa-trash"></i> Delete Permanently</button>';
            html += '</div></td></tr>';
        }
        $('#categoryTableBody').html(html);
    }

    // ==================== INITIALIZATION ====================

    function init() {
        // Load initial data
        loadCategories();
        loadCategoryCount();

        // Bind events
        $('#categoryForm').on('submit', validateAndSubmit);
        $('#cancelBtn').on('click', resetForm);
        $('#searchBtn').on('click', searchCategories);
        $('#resetSearchBtn').on('click', resetSearch);
        $('#filterBtn').on('click', filterByStatus);
        $('#deleteAllBtn').on('click', deleteAllCategories);

        // Enter key search support
        $('#searchInput').on('keypress', function (e) {
            if (e.which === 13) {
                searchCategories();
            }
        });
    }

    // ==================== PUBLIC API ====================
    return {
        init: init,
        editCategory: editCategory,
        deleteCategory: deleteCategory,
        restoreCategory: restoreCategory,
        permanentDeleteCategory: permanentDeleteCategory,
        resetForm: resetForm,
        loadDeletedCategories: loadDeletedCategories
    };
})();

// Initialize when document is ready
$(document).ready(function () {
    CategoryManagement.init();
});