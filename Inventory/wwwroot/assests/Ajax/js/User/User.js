// ==================== USER MANAGEMENT AJAX MODULE ====================

var UserManagement = (function () {
    'use strict';

    var isEditMode = false;
    var currentFilterRole = '';
    var currentSearchTerm = '';

    // ==================== HELPER FUNCTIONS ====================

    function showToast(message, type) {
        var toast = $('#toast');
        toast.text(message).removeClass().addClass('toast ' + type).fadeIn(300);
        setTimeout(function () { toast.fadeOut(300); }, 3000);
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

    // ==================== LOAD USERS ====================

    function loadUsers() {
        var url = '/User/GetAllUsers';
        var params = {};

        if (currentSearchTerm) {
            url = '/User/Search';
            params = { searchTerm: currentSearchTerm };
        } else if (currentFilterRole) {
            url = '/User/GetByRole';
            params = { role: currentFilterRole };
        }

        $.ajax({
            url: url,
            type: 'GET',
            data: params,
            dataType: 'json',
            success: function (users) {
                displayUsers(users);
                updateTotalCount(users.length);
                $('#displayCount').text(users.length);
            },
            error: function (xhr, status, error) {
                console.error('Error loading users:', error);
                $('#userTableBody').html('<tr><td colspan="4" style="text-align:center;color:red;">Error loading users</td></tr>');
                showToast('Error loading users', 'error');
            }
        });
    }

    // ==================== DISPLAY USERS ====================

    function displayUsers(users) {
        if (!users || users.length === 0) {
            $('#userTableBody').html('<tr><td colspan="4" style="text-align:center;">No users found</td></tr>');
            return;
        }

        var html = '';
        for (var i = 0; i < users.length; i++) {
            var user = users[i];
            html += '<tr data-id="' + user.id + '">';
            html += '<td><strong>' + escapeHtml(user.userName) + '</strong></td>';
            html += '<td>' + escapeHtml(user.email) + '</td>';
            html += '<td><span class="role-badge role-' + user.role + '">' + escapeHtml(user.role) + '</span></td>';
            html += '<td><div class="action-buttons">';
            html += '<button class="edit-btn" onclick="UserManagement.editUser(' + user.id + ')"><i class="fas fa-edit"></i> Edit</button>';
            html += '<button class="delete-btn" onclick="UserManagement.deleteUser(' + user.id + ')"><i class="fas fa-trash"></i> Delete</button>';
            html += '</div></td></tr>';
        }
        $('#userTableBody').html(html);
    }

    function updateTotalCount(count) {
        $('#totalUsers').text(count);
    }

    // ==================== CREATE USER ====================

    function createUser(userData) {
        $.ajax({
            url: '/User/Create',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(userData),
            headers: {
                'RequestVerificationToken': getToken(),
                'Accept': 'application/json'
            },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Saving...');
            },
            success: function (response) {
                if (response.success) {
                    showToast('User created successfully!', 'success');
                    resetForm();
                    loadUsers();
                    loadUserCount();
                } else {
                    showToast(response.message || 'Error creating user', 'error');
                }
            },
            error: function (xhr) {
                var errorMsg = xhr.responseJSON?.message || 'Server error occurred';
                showToast(errorMsg, 'error');
                console.error('Create error:', xhr);
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save User');
            }
        });
    }

    // ==================== UPDATE USER ====================

    function updateUser(userData) {
        $.ajax({
            url: '/User/Edit',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(userData),
            headers: {
                'RequestVerificationToken': getToken(),
                'Accept': 'application/json'
            },
            beforeSend: function () {
                $('#submitBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Updating...');
            },
            success: function (response) {
                if (response.success) {
                    showToast('User updated successfully!', 'success');
                    resetForm();
                    loadUsers();
                    loadUserCount();
                } else {
                    showToast(response.message || 'Error updating user', 'error');
                }
            },
            error: function (xhr) {
                var errorMsg = xhr.responseJSON?.message || 'Server error occurred';
                showToast(errorMsg, 'error');
                console.error('Update error:', xhr);
            },
            complete: function () {
                $('#submitBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save User');
            }
        });
    }

    // ==================== DELETE USER (SOFT DELETE) ====================

    function deleteUser(id) {
        if (confirm('⚠️ Are you sure you want to delete this user?')) {
            $.ajax({
                url: '/User/Delete',
                type: 'POST',
                data: {
                    id: id,
                    __RequestVerificationToken: getToken()
                },
                headers: { 'Accept': 'application/json' },
                success: function (response) {
                    if (response.success) {
                        showToast(response.message, 'success');
                        loadUsers();
                        loadUserCount();
                    } else {
                        showToast(response.message || 'Error deleting user', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting user', 'error');
                    console.error('Delete error:', xhr);
                }
            });
        }
    }

    // ==================== DELETE ALL USERS ====================

    function deleteAllUsers() {
        if (confirm('⚠️ DANGER: This will delete ALL users permanently!\n\nAre you absolutely sure?')) {
            $.ajax({
                url: '/User/DeleteAll',
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                headers: { 'Accept': 'application/json' },
                beforeSend: function () {
                    $('#deleteAllBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-pulse"></i> Deleting...');
                },
                success: function (response) {
                    if (response.success) {
                        showToast('All users deleted successfully!', 'success');
                        loadUsers();
                        loadUserCount();
                    } else {
                        showToast(response.message || 'Error deleting users', 'error');
                    }
                },
                error: function (xhr) {
                    showToast('Error deleting users', 'error');
                    console.error('Delete all error:', xhr);
                },
                complete: function () {
                    $('#deleteAllBtn').prop('disabled', false).html('<i class="fas fa-trash-alt"></i> Delete All');
                }
            });
        }
    }

    // ==================== EDIT USER ====================

    function editUser(id) {
        $.ajax({
            url: '/User/Details/' + id,
            type: 'GET',
            dataType: 'json',
            success: function (user) {
                isEditMode = true;
                $('#userId').val(user.id);
                $('#userName').val(user.userName);
                $('#email').val(user.email);
                $('#role').val(user.role);
                $('#password').val('').prop('required', false);
                $('#pwdRequired').html('(optional)');
                $('#formTitle').text('Edit User');
                $('#submitBtn').html('<i class="fas fa-pen"></i> Update User');
                $('#cancelBtn').show();
                showToast('Edit mode: Update user details', 'info');
            },
            error: function (xhr) {
                showToast('Error loading user details', 'error');
                console.error('Edit load error:', xhr);
            }
        });
    }

    // ==================== LOAD USER COUNT ====================

    function loadUserCount() {
        $.ajax({
            url: '/User/GetCount',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                $('#totalUsers').text(response.count);
            },
            error: function (xhr) {
                console.error('Count error:', xhr);
                $('#totalUsers').text('0');
            }
        });
    }

    // ==================== RESET FORM ====================

    function resetForm() {
        isEditMode = false;
        $('#userForm')[0].reset();
        $('#userId').val('0');
        $('#password').prop('required', true);
        $('#pwdRequired').html('*');
        $('#formTitle').text('Add New User');
        $('#submitBtn').html('<i class="fas fa-save"></i> Save User');
        $('#cancelBtn').hide();
        $('.error').html('');
    }

    // ==================== SEARCH FUNCTIONALITY ====================

    function searchUsers() {
        currentSearchTerm = $('#searchInput').val().trim();
        currentFilterRole = '';
        $('#roleFilter').val('');
        loadUsers();
    }

    function resetSearch() {
        $('#searchInput').val('');
        currentSearchTerm = '';
        currentFilterRole = '';
        $('#roleFilter').val('');
        loadUsers();
    }

    function filterByRole() {
        currentFilterRole = $('#roleFilter').val();
        currentSearchTerm = '';
        $('#searchInput').val('');
        loadUsers();
    }

    // ==================== FORM VALIDATION ====================

    function validateAndSubmit(e) {
        e.preventDefault();

        $('.error').html('');

        var userId = $('#userId').val();
        var userName = $('#userName').val().trim();
        var email = $('#email').val().trim();
        var password = $('#password').val();
        var role = $('#role').val();

        var isValid = true;

        if (!userName) {
            $('#userNameError').html('Username is required');
            isValid = false;
        } else if (userName.length < 3) {
            $('#userNameError').html('Username must be at least 3 characters');
            isValid = false;
        }

        if (!email) {
            $('#emailError').html('Email is required');
            isValid = false;
        } else if (email.indexOf('@') === -1 || email.indexOf('.') === -1) {
            $('#emailError').html('Invalid email format');
            isValid = false;
        }

        if (!isEditMode && !password) {
            $('#passwordError').html('Password is required for new users');
            isValid = false;
        } else if (password && password.length < 4) {
            $('#passwordError').html('Password must be at least 4 characters');
            isValid = false;
        }

        if (!role) {
            $('#roleError').html('Role is required');
            isValid = false;
        }

        if (!isValid) {
            showToast('Please fix the validation errors', 'error');
            return false;
        }

        var userData = {
            Id: parseInt(userId),
            UserName: userName,
            Email: email,
            Password: password,
            Role: role
        };

        if (isEditMode) {
            updateUser(userData);
        } else {
            createUser(userData);
        }

        return false;
    }

    // ==================== INITIALIZATION ====================

    function init() {
        loadUsers();
        loadUserCount();

        $('#userForm').on('submit', validateAndSubmit);
        $('#cancelBtn').on('click', resetForm);
        $('#searchBtn').on('click', searchUsers);
        $('#resetSearchBtn').on('click', resetSearch);
        $('#filterBtn').on('click', filterByRole);
        $('#deleteAllBtn').on('click', deleteAllUsers);
    }

    // ==================== PUBLIC API ====================
    return {
        init: init,
        editUser: editUser,
        deleteUser: deleteUser,
        resetForm: resetForm
    };
})();

$(document).ready(function () {
    UserManagement.init();
});